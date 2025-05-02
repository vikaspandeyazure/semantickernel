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
using Microsoft.Azure.Cosmos;
using Azure.Identity;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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
        string AZURE_COSMOSDBCONNECTIONSTRING = Environment.GetEnvironmentVariable("AZURE_COSMOSDBCONNECTIONSTRING") ?? string.Empty;
        string AZURE_COSMOSDBLOGDATABSEID = Environment.GetEnvironmentVariable("AZURE_COSMOSDBLOGDATABSEID") ?? string.Empty;
        string AZURE_COSMOSDBLOGCONTAINERID = Environment.GetEnvironmentVariable("AZURE_COSMOSDBLOGCONTAINERID") ?? string.Empty;
        string AZURE_COSMOSDBENDPOINT = Environment.GetEnvironmentVariable("AZURE_COSMOSDBENDPOINT") ?? string.Empty;
        string AZURE_OPENAI_CURRENCY = Environment.GetEnvironmentVariable("AZURE_OPENAI_CURRENCY") ?? string.Empty;
        string AZURE_OPENAI_INPUTTOKENCOST = Environment.GetEnvironmentVariable("AZURE_OPENAI_INPUTTOKENCOST") ?? string.Empty;
        string AZURE_OPENAI_OUTPUTTOKENCOST = Environment.GetEnvironmentVariable("AZURE_OPENAI_OUTPUTTOKENCOST") ?? string.Empty;

        double OpenAI_InputCost;
        double OpenAI_OutputCost;
        OpenAI_InputCost = Double.TryParse(AZURE_OPENAI_INPUTTOKENCOST, out OpenAI_InputCost) ? OpenAI_InputCost : 0.0;
        OpenAI_OutputCost = Double.TryParse(AZURE_OPENAI_OUTPUTTOKENCOST, out OpenAI_OutputCost) ? OpenAI_OutputCost : 0.0;
        
        // Console.WriteLine($"OpenAI_InputCost = {OpenAI_InputCost}, OpenAI_OutputCost = {OpenAI_OutputCost}");
  
        

       string sqlScriptPath = "sql\\schema_setup.sql";

        try
        {
            await DatabaseCreate.RunSqlScriptIfSchemasNotExistAsync(AZURE_SQLCONNECTIONSTRING, sqlScriptPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while creating Source Database: {ex.Message}");
        }

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
        
        //Enable Logging
        //builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));

       builder.Services.AddSingleton<Database>(
    sp =>
    {
        var cosmosClient = new CosmosClient(AZURE_COSMOSDBENDPOINT,new DefaultAzureCredential(), new CosmosClientOptions()
        {
            // When initializing CosmosClient manually, setting this property is required 
            // due to limitations in default serializer. 
            UseSystemTextJsonSerializerWithOptions = JsonSerializerOptions.Default,
        });

        return cosmosClient.GetDatabase(AZURE_COSMOSDBLOGDATABSEID);
    });
        
        builder.Services.AddSingleton<ChatLogger>(sp =>
        {
            var database = sp.GetRequiredService<Database>();
            return new ChatLogger(database, AZURE_COSMOSDBLOGCONTAINERID);
        });


        Kernel kernel = builder.Build();
        Kernel toolKernel = kernel.Clone();
        //var chatLogger = kernel.Services.GetService<ChatLogger>();
        Console.WriteLine("Creating in-memory vector store...");

        var vectorStoreService = kernel.GetRequiredService<InMemoryVectorStore>();
        var textEmbeddingGenerationService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        //var logcosmosdbservice = kernel.GetRequiredService<Database>();
        ChatLogger chatLogger = kernel.Services.GetRequiredService<ChatLogger>();

        var FAQcollectionName = "FAQ";
        var chatcollectionName = "chatstore";

        var vectorStorePlugin = new VectorStorePlugin<string>(vectorStoreService, textEmbeddingGenerationService);
        await vectorStorePlugin.FAQIngestDataToVectorStore(vectorStoreService, FAQcollectionName, () => Guid.NewGuid().ToString());

        // Add plugins to Kernel
        toolKernel.Plugins.AddFromType<ClipboardAccess>();
        toolKernel.Plugins.AddFromObject(new SearchDatabasePluginNoLog(kernel, AZURE_SQLCONNECTIONSTRING));
        kernel.Plugins.AddFromObject(new VectorStorePlugin<string>(vectorStoreService, textEmbeddingGenerationService));
        //kernel.Plugins.AddFromObject(new ChatLogger<string>(vectorStoreService, textEmbeddingGenerationService));
        //Enable Logging
        //var logger = kernel.GetRequiredService<ILogger<Program>>();
        Console.WriteLine("Defining agents...");

        //Chatlogger chatLogger = new ChatLogger(logcosmosdbservice, chatcollectionName);
        
        const string generatorName = "GenAgent";
        const string executorName = "ExecAgent";
        const string summarizorName = "SummAgent";
        const string reviewerName = "ReviewAgent";

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

        ChatProcessor processor = new ChatProcessor(kernel, vectorStorePlugin, chat, textEmbeddingGenerationService, chatcollectionName, summarizorName,reviewerName, OpenAI_InputCost, OpenAI_OutputCost,chatLogger);
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


    
}
