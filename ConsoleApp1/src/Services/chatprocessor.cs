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
using AgentsSample;

#pragma warning disable CS0618, SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0110

    public class ChatProcessor
    {
        private bool isComplete = false;
        private readonly Kernel _kernel;
        private readonly ChatLogger _chatLogger; 
        private readonly VectorStorePlugin<string> _vectorStorePlugin;
        private readonly AgentGroupChat _chat;
        private readonly ITextEmbeddingGenerationService _embeddingService;
        private readonly string _chatCollectionName;
        private readonly string _summarizorName;
        private readonly string _reviewerName;
        private readonly double _inputCost;
        private readonly double _outputCost;
        public int OutputTokenCount = 0;
        public int InputTokenCount = 0;
        public int TotalTokenCount = 0;
    

        public ChatProcessor(Kernel kernel, VectorStorePlugin<string> vectorStorePlugin, AgentGroupChat chat, ITextEmbeddingGenerationService embeddingService, string chatCollectionName, string summarizorName,string reviewerName, double inputCost, double outputCost, ChatLogger chatLogger)
        {
            _kernel = kernel;
            _vectorStorePlugin = vectorStorePlugin;
            _chat = chat;
            _embeddingService = embeddingService;
            _chatCollectionName = chatCollectionName;
            _summarizorName = summarizorName;
            _reviewerName = reviewerName;
            _inputCost = inputCost;
            _outputCost = outputCost;
            _chatLogger = chatLogger;
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
             ChatLogEntry chatLogEntry = new ChatLogEntry
                {
                    id=Guid.NewGuid().ToString(),
                    UserInput = input,
                    Timestamp = DateTime.UtcNow,
                    AgentResponses = new List<AgentResponse>()
                };
            await InvokeAgentChat(chatLogEntry,input);
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


    
            private async Task InvokeAgentChat(ChatLogEntry chatLogEntry, string input)
            {
                
                try
                {
                    await foreach (ChatMessageContent response in _chat.InvokeAsync())
                    {
                                       
                        Tokens? tokenCounts = JsonSerializer.Deserialize<Tokens>(JsonSerializer.Serialize(response.Metadata?["Usage"]) ?? string.Empty);
                    
                        if (tokenCounts != null)
                        {
                        
                            OutputTokenCount = tokenCounts.OutputTokenCount;
                            InputTokenCount = tokenCounts.InputTokenCount;
                            TotalTokenCount = tokenCounts.TotalTokenCount;
                        }
                        chatLogEntry.AgentResponses.Add(new AgentResponse
                                {
                                    AgentName = response?.AuthorName?.ToString()?? string.Empty,
                                    Response = response.Content,
                                    Timestamp = DateTime.UtcNow,
                                    Tokens = new LogTokens
                                    {
                                        Input = InputTokenCount,
                                        Output = OutputTokenCount,
                                        InputCost=Math.Round((double)InputTokenCount/ 1000 * _inputCost,8),
                                        OutputCost=Math.Round((double)OutputTokenCount/ 1000 * _outputCost,8),
                                        Total = TotalTokenCount    
                                    }
                                });
                
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

                    await _vectorStorePlugin.IngestDataToChatStore(_chatCollectionName, newCacheEntry);
                }
                 else if(response.AuthorName==_reviewerName)
                 {
                    Console.Write("\n>>>>>>>>>>>Intiatiating Log entry ");
                   var result=   await _chatLogger.CreateChatLogEntry(chatLogEntry);
                      Console.WriteLine($"Created chat log entry with ID: {result.id}");
                 }
               
    
                        PrintTokenCounts();
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
    
       

        private void PrintTokenCounts()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            double input=Math.Round((double)InputTokenCount / 1000 * _inputCost,8);
            double output=Math.Round((double)OutputTokenCount / 1000 * _outputCost,8);
            double total=Math.Round(input+output,8);
            Console.WriteLine($"\nInputToken: {InputTokenCount} | ${input} | OutputToken: {OutputTokenCount} | ${output} | TotalTokens: {TotalTokenCount} | ${total}");
                       
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