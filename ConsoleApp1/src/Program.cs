using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Embeddings;
using DotNetEnv;
using Microsoft.SemanticKernel.Agents;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgentsSample;

#pragma warning disable CS0618, SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0110

public class Program
{

    #region Main Function
    public static async Task Main()
    {
        Env.Load();
        string AZURE_SQLCONNECTIONSTRING = Environment.GetEnvironmentVariable("AZURE_SQLCONNECTIONSTRING") ?? string.Empty;
        string AZURE_OPENAI_ENDPOINT = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? string.Empty;
        string AZURE_OPENAI_API_KEY = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? string.Empty;
        string AZURE_OPENAI_DEPLOYMENTNAME = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENTNAME") ?? string.Empty;
        string AZURE_OPENAI_MODEL = Environment.GetEnvironmentVariable("AZURE_OPENAI_MODEL") ?? string.Empty;
        string AZURE_OPENAI_EMBEDDING_ENDPOINT = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_ENDPOINT") ?? string.Empty;
        string AZURE_OPENAI_EMBEDDING_API_KEY = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_API_KEY") ?? string.Empty;
        string AZURE_OPENAI_EMBEDDING_DEPLOYMENTNAME = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENTNAME") ?? string.Empty;
        string AZURE_OPENAI_EMBEDDING_MODEL = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_MODEL") ?? string.Empty;

        Console.WriteLine("Creating kernel...");

        IKernelBuilder builder = Kernel.CreateBuilder().AddInMemoryVectorStore();

        builder.AddAzureOpenAIChatCompletion(
            deploymentName: AZURE_OPENAI_DEPLOYMENTNAME,
            endpoint: AZURE_OPENAI_ENDPOINT,
            apiKey: AZURE_OPENAI_API_KEY,
            modelId: AZURE_OPENAI_MODEL);

        builder.AddAzureOpenAITextEmbeddingGeneration(
            deploymentName: AZURE_OPENAI_EMBEDDING_DEPLOYMENTNAME,
            endpoint: AZURE_OPENAI_EMBEDDING_ENDPOINT,
            apiKey: AZURE_OPENAI_EMBEDDING_API_KEY,
            modelId: AZURE_OPENAI_EMBEDDING_MODEL);
        
        //builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));

        Kernel kernel = builder.Build();
        Kernel toolKernel = kernel.Clone();

        Console.WriteLine("Creating in-memory vector store...");

        var vectorStoreService = kernel.GetRequiredService<InMemoryVectorStore>();
        var textEmbeddingGenerationService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();

        var FAQcollectionName = "FAQ";
        var chatcollectionName = "chatstore";

        var vectorStorePlugin = new VectorStorePlugin<string>(vectorStoreService, textEmbeddingGenerationService);
        await vectorStorePlugin.FAQIngestDataToVectorStore(vectorStoreService, FAQcollectionName, () => Guid.NewGuid().ToString());

        toolKernel.Plugins.AddFromType<ClipboardAccess>();
        toolKernel.Plugins.AddFromObject(new SearchDatabasePluginNoLog(kernel, AZURE_SQLCONNECTIONSTRING));
        kernel.Plugins.AddFromObject(new VectorStorePlugin<string>(vectorStoreService, textEmbeddingGenerationService));
        //var logger = kernel.GetRequiredService<ILogger<Program>>();
        Console.WriteLine("Defining agents...");

        
        const string generatorName = "SQLGeneratorAgent";
        const string executorName = "SQLExecutorAgent";
        const string summarizorName = "SQLSummarizerAgent";
        const string reviewerName = "SQLChatReviewerAgent";

        //int OutputTokenCount = 0;
        //int InputTokenCount = 0;
        //int TotalTokenCount = 0;
        double InputCost = 0.0050;
        double OutputCost = 0.0150;

        ChatCompletionAgent agentReviewer = GroupAgentFactory.CreateReviewerAgent(reviewerName, kernel);
        ChatCompletionAgent sqlGeneratorAgent = GroupAgentFactory.CreateSQLGeneratorAgent(generatorName, toolKernel);
        ChatCompletionAgent sqlExecutorAgent = GroupAgentFactory.CreateSQLExecutorAgent(executorName, toolKernel);
        ChatCompletionAgent sqlSummarizerAgent = GroupAgentFactory.CreateSQLSummarizerAgent(summarizorName, kernel);

        KernelFunction selectionFunction = SelectionFunctionFactory.CreateSelectionFunction(generatorName, executorName, summarizorName, reviewerName);
        const string terminationToken = "yes";
        KernelFunction terminationFunction = SelectionFunctionFactory.CreateTerminationFunction(reviewerName, terminationToken);

        ChatHistoryTruncationReducer historyReducer = new(1);

        AgentGroupChat chat = AgentGroupChatFactory.CreateAgentGroupChat(
            sqlGeneratorAgent,
            sqlExecutorAgent,
            sqlSummarizerAgent,
            agentReviewer,
            kernel,
            selectionFunction,
            terminationFunction,
            historyReducer,
            terminationToken
        );

        Console.WriteLine("Ready!");

        ChatProcessor processor = new ChatProcessor(kernel, vectorStorePlugin, chat, textEmbeddingGenerationService, chatcollectionName, summarizorName, InputCost, OutputCost);
        await processor.ProcessChatLoop();

    }
    #endregion
    private sealed class ClipboardAccess
    {
        [KernelFunction]
        [Description("Copies the provided content to the clipboard.")]
        public static void SetClipboard(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            using Process clipProcess = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "clip",
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                });

            if (clipProcess != null)
            {
                clipProcess.StandardInput.Write(content);
                clipProcess.StandardInput.Close();
            }
        }
    }

    public class ChatProcessor
    {
        private bool isComplete = false;
        private readonly Kernel _kernel;
        private readonly VectorStorePlugin<string> _vectorStorePlugin;
        private readonly AgentGroupChat _chat;
        private readonly ITextEmbeddingGenerationService _embeddingService;
        private readonly string _chatCollectionName;
        private readonly string _summarizorName;
        private readonly double _inputCost;
        private readonly double _outputCost;
        public int OutputTokenCount = 0;
        public int InputTokenCount = 0;
        public int TotalTokenCount = 0;

        public ChatProcessor(Kernel kernel, VectorStorePlugin<string> vectorStorePlugin, AgentGroupChat chat, ITextEmbeddingGenerationService embeddingService, string chatCollectionName, string summarizorName, double inputCost, double outputCost)
        {
            _kernel = kernel;
            _vectorStorePlugin = vectorStorePlugin;
            _chat = chat;
            _embeddingService = embeddingService;
            _chatCollectionName = chatCollectionName;
            _summarizorName = summarizorName;
            _inputCost = inputCost;
            _outputCost = outputCost;
        }

        public async Task ProcessChatLoop()
        {
            do
            {
                string input = GetUserInput();
                if (string.IsNullOrEmpty(input)) continue;

                switch (input.ToUpperInvariant())
                {
                    case "EXIT":
                        isComplete = true;
                        break;
                    case "RESET":
                        await ResetConversation();
                        break;
                    default:
                        await ProcessUserQuery(input);
                        break;
                }
            } while (!isComplete);
        }

        private string GetUserInput()
        {
            Console.WriteLine();
            Console.Write("User (To quit type [EXIT] or to reset type [RESET])> ");
            return Console.ReadLine()?.Trim();
        }

        private async Task ResetConversation()
        {
            InputTokenCount = 0;
            OutputTokenCount = 0;
            TotalTokenCount = 0;
            await _chat.ResetAsync();
            Console.WriteLine("[Conversation has been reset]");
        }

       private async Task ProcessUserQuery(string input)
        {
            if (input.StartsWith("#", StringComparison.Ordinal))
            {
                input = await ReadFromFile(input.Substring(1));
                if (input == null) return;
            }

            string vectorSearchResult = await PerformVectorSearch(input);
            if (await HandleVectorSearchResult(vectorSearchResult)) return;

            _chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, input));
            await InvokeAgentChat(input);
            _chat.IsComplete = false;
        }

        private async Task<string> ReadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Unable to access file: {filePath}");
                    return null;
                }
                string fileContent = await File.ReadAllTextAsync(filePath);
                Console.WriteLine($"\nInput Query from file: {fileContent}");
                return fileContent;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to access file: {filePath}. Error: {ex.Message}");
                return null;
            }
        }

        private async Task<string> PerformVectorSearch(string input)
        {
            Console.WriteLine($">>>>>>>>>>> Searching Chat Cache for recent conversations <<<<<<<<<<<<<<\n");
            return await _vectorStorePlugin.SearchChatStore(_chatCollectionName, input);
        }

       private async Task<bool> HandleVectorSearchResult(string vectorSearchResult)
        {
    // Even if you don't use await, it is still required to have one for async methods,
    // so we will add a delay of 0 milliseconds.
    await Task.Delay(0);

    if (!string.IsNullOrWhiteSpace(vectorSearchResult) && vectorSearchResult != "No results found.")
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($">>>>>>>>>>>Chat cache search Result:\n{vectorSearchResult}");
        Console.Write("\nIs the answer correct? (Yes/No)> ");
        Console.ResetColor();
        string userResponse = Console.ReadLine()?.Trim();

        if (userResponse?.Equals("Yes", StringComparison.OrdinalIgnoreCase) == true)
        {
            isComplete = true;
            return true;
        }
    }
    return false;
        }

        private async Task InvokeAgentChat(string input)
        {
            try
            {
                await foreach (ChatMessageContent response in _chat.InvokeAsync())
                {
                    await ProcessAgentResponse(input, response);
                }
            }
            catch (HttpOperationException exception)
            {
                HandleHttpException(exception);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"An error occurred while invoking agent chat: {exception.Message}");
            }
        }

        private async Task ProcessAgentResponse(string input, ChatMessageContent response)
        {

            

            TokenCounts? tokenCounts = JsonSerializer.Deserialize<TokenCounts>(JsonSerializer.Serialize(response.Metadata?["Usage"]) ?? string.Empty);
            if (tokenCounts != null)
            {
                OutputTokenCount += tokenCounts.OutputTokenCount;
                InputTokenCount += tokenCounts.InputTokenCount;
                TotalTokenCount += tokenCounts.TotalTokenCount;
            }

            Console.WriteLine($"{response.AuthorName?.ToUpperInvariant() ?? "UNKNOWN"}:{Environment.NewLine}{response.Content}");

            if (response.AuthorName == _summarizorName)
            {
                Func<string> uniqueKeyGenerator = () => Guid.NewGuid().ToString();
                var newCacheEntry = new ChatStore<string>
                {
                    QuestionId = uniqueKeyGenerator(),
                    Question = input,
                    Answer = response.Content ?? string.Empty,
                    QuestionEmbedding = await _embeddingService.GenerateEmbeddingAsync(input)
                };
                await _vectorStorePlugin.IngestDataToChatStore(_chatCollectionName, newCacheEntry).ConfigureAwait(false);
            }
            // else if(response.AuthorName=="SQLChatReviewerAgent")
            // {
            //      Console.WriteLine("SQL Chat reviewer invoked");
            // }
           

            PrintTokenCounts();
        }

        private void PrintTokenCounts()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\nTotalInputToken: {InputTokenCount} | ${InputTokenCount / 1000 * _inputCost} | TotalOutputToken: {OutputTokenCount} | ${OutputTokenCount / 1000 * _outputCost} | TotalTokens: {TotalTokenCount} | ${InputTokenCount / 1000 * _inputCost + OutputTokenCount / 1000 * _outputCost}");
            Console.ResetColor();
        }

        private void HandleHttpException(HttpOperationException exception)
        {
            Console.WriteLine($"Http main exception {exception.Message}");
            Console.WriteLine(exception.StackTrace);
            if (exception.InnerException != null)
            {
                Console.WriteLine($"Http Exception : {exception.InnerException.Message}");
                Console.WriteLine(exception.StackTrace);
                if (exception.InnerException.Data.Count > 0)
                {
                    Console.WriteLine(JsonSerializer.Serialize(exception.InnerException.Data, new JsonSerializerOptions() { WriteIndented = true }));
                }
        }
    }
    
}
}