// // Copyright (c) Microsoft. All rights reserved.

// using System;
// using System.ComponentModel;
// using System.Diagnostics;
// using System.IO;
// using System.Text.Json;
// using System.Threading.Tasks;
// using Azure.Identity;
// using Microsoft.SemanticKernel;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
// using Microsoft.SemanticKernel.Agents;
// using Microsoft.SemanticKernel.Agents.Chat;
// using Microsoft.SemanticKernel.ChatCompletion;
// using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
// using Microsoft.SemanticKernel.Memory;
// using Microsoft.SemanticKernel.Connectors.InMemory;
// using Microsoft.SemanticKernel.Embeddings;
// using DotNetEnv;
// using Xunit.Abstractions;
// using System.Runtime.CompilerServices;
// namespace AgentsSample;

// #pragma warning disable CS0618,SKEXP0001, SKEXP0010, SKEXP0020,SKEXP0110



// public class Program 
// {
     
//     public static async Task Main()
//     {
       

//         Env.Load();
//         string AZURE_SQLCONNECTIONSTRING=Environment.GetEnvironmentVariable("AZURE_SQLCONNECTIONSTRING") ?? string.Empty;

//         string AZURE_OPENAI_ENDPOINT = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? string.Empty;
//         string AZURE_OPENAI_API_KEY = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? string.Empty;
//         string AZURE_OPENAI_DEPLOYMENTNAME = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENTNAME") ?? string.Empty;
//         string AZURE_OPENAI_MODEL = Environment.GetEnvironmentVariable("AZURE_OPENAI_MODEL") ?? string.Empty;

//         string AZURE_OPENAI_EMBEDDING_ENDPOINT = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_ENDPOINT") ?? string.Empty;
//         string AZURE_OPENAI_EMBEDDING_API_KEY = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_API_KEY") ?? string.Empty;
//         string AZURE_OPENAI_EMBEDDING_DEPLOYMENTNAME = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_DEPLOYMENTNAME") ?? string.Empty;
//         string AZURE_OPENAI_EMBEDDING_MODEL = Environment.GetEnvironmentVariable("AZURE_OPENAI_EMBEDDING_MODEL") ?? string.Empty;
      
//         // Console.WriteLine(
//         //                     $"AZURE_OPENAI_API_KEY={AZURE_OPENAI_API_KEY} | \n" +
//         //                     $"AZURE_OPENAI_ENDPOINT={AZURE_OPENAI_ENDPOINT} \n" +
//         //                     $"AZURE_OPENAI_DEPLOYMENTNAME={AZURE_OPENAI_DEPLOYMENTNAME} \n" +
//         //                     $"AZURE_OPENAI_MODEL={AZURE_OPENAI_MODEL} \n" +
//         //                     $"AZURE_OPENAI_EMBEDDING_ENDPOINT={AZURE_OPENAI_EMBEDDING_ENDPOINT} \n" +
//         //                     $"AZURE_OPENAI_EMBEDDING_API_KEY={AZURE_OPENAI_EMBEDDING_API_KEY} \n" +
//         //                     $"AZURE_OPENAI_EMBEDDING_DEPLOYMENTNAME={AZURE_OPENAI_EMBEDDING_DEPLOYMENTNAME} \n" +
//         //                     $"AZURE_OPENAI_EMBEDDING_MODEL={AZURE_OPENAI_EMBEDDING_MODEL} \n" +
//         //                     $"AZURE_SQLCONNECTIONSTRING={AZURE_SQLCONNECTIONSTRING} " 
//         //                 );
//         // Load configuration from environment variables or user secrets.
//         //Settings settings = new();

        
        
//         //FetchAndCacheSchema(sqlConnectionString, vectorStore);

//         Console.WriteLine("Creating kernel...");

//         IKernelBuilder builder = Kernel.CreateBuilder().AddInMemoryVectorStore();
        
        

//         // Add enterprise components                            
//         builder.AddAzureOpenAIChatCompletion(
//                                 deploymentName:AZURE_OPENAI_DEPLOYMENTNAME,
//                                 endpoint:AZURE_OPENAI_ENDPOINT,
//                                 apiKey:AZURE_OPENAI_API_KEY,
//                                 modelId:AZURE_OPENAI_MODEL);
        
//         builder.AddAzureOpenAITextEmbeddingGeneration(
//                                 deploymentName: AZURE_OPENAI_EMBEDDING_DEPLOYMENTNAME,
//                                 endpoint: AZURE_OPENAI_EMBEDDING_ENDPOINT,
//                                 apiKey: AZURE_OPENAI_EMBEDDING_API_KEY,
//                                 modelId: AZURE_OPENAI_EMBEDDING_MODEL); 
                                
//         //builder.Services.AddSingleton<ITestOutputHelper>(this.Output);
//            //builder.Services.AddSingleton<ITestOutputHelper>();
//        // builder.Services.AddInMemoryVectorStore(vectorStore); 
       
//          //builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));

//         // Build the kernel.
//         Kernel kernel = builder.Build();
//         Kernel toolKernel = kernel.Clone();

//         // Create a vector store.
//         Console.WriteLine("Creating in-memory vector store...");
//         //var vectorStore = new InMemoryVectorStore();

        
//         var vectorStoreService = kernel.GetRequiredService<InMemoryVectorStore>();
//         var textEmbeddingGenerationService = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
       
//         //var logger = kernel.GetRequiredService<ILogger<Program>>();
//         var FAQcollectionName="FAQ";
//         var chatcollectionName="chatstore";

//         //var collection = new InMemoryVectorStoreRecordCollection<string, FAQ<string>>(collectionName);
//         var vectorStorePlugin = new VectorStorePlugin<string>(vectorStoreService, textEmbeddingGenerationService);
//         //VectorStoreManager<string> vectormanager = new VectorStoreManager<string>(textEmbeddingGenerationService);
//         await vectorStorePlugin.FAQIngestDataToVectorStore(vectorStoreService, FAQcollectionName, () => Guid.NewGuid().ToString());
        

//     // Add plugins.
//         toolKernel.Plugins.AddFromType<ClipboardAccess>();
//         toolKernel.Plugins.AddFromObject(new SearchDatabasePluginNoLog(kernel, AZURE_SQLCONNECTIONSTRING));
//         kernel.Plugins.AddFromObject(new VectorStorePlugin<string>(vectorStoreService, textEmbeddingGenerationService));
//         //toolKernel.Plugins.AddFromObject(new SearchDatabasePlugin(toolKernel, logger, sqlConnectionString));
        
        
        
        
//         Console.WriteLine("Defining agents...");

//         const string reviewerName = "SQLChatReviewerAgent";
//         const string generatorName = "SQLGeneratorAgent";
//         const string executorName = "SQLExecutorAgent";
//         const string summarizorName = "SQLSummarizerAgent";

//         int OutputTokenCount = 0;
//         int InputTokenCount = 0;
//         int TotalTokenCount = 0;
//         //double TotalCost = 0.0;
//         double InputCost = 0.0050;
//         double OutputCost = 0.0150;

//         ChatCompletionAgent agentReviewer = GroupAgentFactory.CreateReviewerAgent(reviewerName, kernel);
//         ChatCompletionAgent sqlGeneratorAgent = GroupAgentFactory.CreateSQLGeneratorAgent(generatorName, toolKernel);
//         ChatCompletionAgent sqlExecutorAgent = GroupAgentFactory.CreateSQLExecutorAgent(executorName, toolKernel);
//         ChatCompletionAgent sqlSummarizerAgent = GroupAgentFactory.CreateSQLSummarizerAgent(summarizorName,kernel); 

//         // Define the selection and termination functions.
//         KernelFunction selectionFunction = SelectionFunctionFactory.CreateSelectionFunction(generatorName, executorName, summarizorName,reviewerName );
//         const string terminationToken = "yes";
//         KernelFunction terminationFunction = SelectionFunctionFactory.CreateTerminationFunction(reviewerName,terminationToken);

//     // Define the history reducer.
//         ChatHistoryTruncationReducer historyReducer = new(1);
    
//     // Create the chat.
//         AgentGroupChat chat = AgentGroupChatFactory.CreateAgentGroupChat(
//             sqlGeneratorAgent,
//             sqlExecutorAgent,
//             sqlSummarizerAgent,
//             agentReviewer,
//             kernel,
//             selectionFunction,
//             terminationFunction,
//             historyReducer,
//             terminationToken
//         );

//         Console.WriteLine("Ready!");
        
        
//           bool isComplete = false;
//         do
//         {
//             Console.WriteLine();
//             Console.Write("User (To quit type [EXIT] or to reset type [RESET])> ");
//             string input = Console.ReadLine() ?? string.Empty;
//             if (string.IsNullOrWhiteSpace(input))
//             {
//                 continue;
//             }
//             input = input.Trim();
//             if (input.Equals("EXIT", StringComparison.OrdinalIgnoreCase))
//             {
//                 isComplete = true;
//                 break;
//             }

//             if (input.Equals("RESET", StringComparison.OrdinalIgnoreCase))
//             {
//                 InputTokenCount=0;
//                 OutputTokenCount=0;
//                 TotalTokenCount=0;

//                 await chat.ResetAsync();
//                 Console.WriteLine("[Converation has been reset]");
//                 continue;
//             }

//             if (input.StartsWith("#", StringComparison.Ordinal) && input.Length > 1)
//             {
//                 string filePath = input.Substring(1);
//                 try
//                 {
//                     if (!File.Exists(filePath))
//                     {
//                         Console.WriteLine($"Unable to access file: {filePath}");
//                         continue;
//                     }
                    
//                     input = File.ReadAllText(filePath);
//                     Console.WriteLine($"\n Input Query from file : {input}");
//                 }
//                 catch (Exception)
//                 {
//                     Console.WriteLine($"Unable to access file: {filePath}");
//                     continue;
//                 }
//             }

//              // Perform vector search
           
           
//             chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, input));
//            // Console.WriteLine($">>>>>>>>>>>Searching Vector Store\n");
//            // string vectorSearchResult = await vectorStorePlugin.SearchChatStore(chatcollectionName, input);

//             // if (!string.IsNullOrWhiteSpace(vectorSearchResult) && vectorSearchResult != "No results found.")
//             // {
//             //     Console.ForegroundColor = ConsoleColor.Green;
//             //     Console.WriteLine($">>>>>>>>>>>Vector Store Result:\n{vectorSearchResult}");
//             //     Console.Write("\nIs the answer correct? (Yes/No)> ");
//             //     Console.ResetColor(); 
//             //     string userResponse = Console.ReadLine()?.Trim();

//             //     if (userResponse?.Equals("Yes", StringComparison.OrdinalIgnoreCase) == true)
//             //     {
//             //         isComplete = true;
//             //         break;
//             //     }
//             //     else
//             //     {
//             //         continue;
                      
//             //     }
//             // }

//             try
//                     {
//                       await foreach (ChatMessageContent response in chat.InvokeAsync())
//                       {
//                     //Console.WriteLine();
                                      
                    
//                     TokenCounts tokenCounts = JsonSerializer.Deserialize<TokenCounts>(JsonSerializer.Serialize(response.Metadata?["Usage"]) ?? string.Empty);
//                     if (tokenCounts != null)
//                     {
//                         OutputTokenCount += tokenCounts.OutputTokenCount;
//                         InputTokenCount += tokenCounts.InputTokenCount;
//                         TotalTokenCount += tokenCounts.TotalTokenCount;
//                     }
                    
//                     // Console.WriteLine($"InputTokenCount: {tokenCounts.InputTokenCount}");
//                     // Console.WriteLine($"TotalTokenCount: {tokenCounts.TotalTokenCount}");
//                     //Console.WriteLine($"{JsonSerializer.Serialize(response.Metadata?["Usage"])}");
//                     Console.WriteLine($"{response.AuthorName?.ToUpperInvariant() ?? "UNKNOWN"}:{Environment.NewLine}{response.Content}");

                    
//                     // if(response.AuthorName==summarizorName)
//                     // {
//                     //     Func<string> uniqueKeyGenerator = () => Guid.NewGuid().ToString();
//                     //  // Cache the result using ChatStore
//                     //     var newCacheEntry = new ChatStore<string>
//                     //     {
//                     //      QuestionId =  uniqueKeyGenerator(),
//                     //      Question = input,
//                     //      Answer = response.Content,
//                     //      QuestionEmbedding = await textEmbeddingGenerationService.GenerateEmbeddingAsync(input)
//                     //     };
//                     //     await vectorStorePlugin.IngestDataToChatStore(chatcollectionName, newCacheEntry);
//                     // }

//                     Console.ForegroundColor = ConsoleColor.Yellow;
//                     Console.WriteLine($"\nTotalInputToken: {InputTokenCount} | ${InputTokenCount/1000*InputCost}  | TotalOutputToken: {OutputTokenCount} | ${OutputTokenCount/1000*OutputCost}| TotalTokens: {TotalTokenCount} | ${InputTokenCount/1000*InputCost+OutputTokenCount/1000*OutputCost}" );
//                     Console.ResetColor();
//                     //await Task.Delay(TimeSpan.FromSeconds(1000));
//                         }
//                     }
//                      catch (HttpOperationException exception)
//                      {
//                              Console.WriteLine($"Http main exception {exception.Message}");
//                             Console.WriteLine(exception.StackTrace);
//                             if (exception.InnerException != null)
//                             {
//                                 Console.WriteLine($"Http Exception : {exception.InnerException.Message}");
//                                 Console.WriteLine(exception.StackTrace);
//                                 if (exception.InnerException.Data.Count > 0)
//                                 {
//                                     Console.WriteLine(JsonSerializer.Serialize(exception.InnerException.Data, new JsonSerializerOptions() { WriteIndented = true }));
//                                 }
//                             }
//                      }
//             chat.IsComplete = false;

           
//         } while (!isComplete);
//     }

//     private sealed class ClipboardAccess
//     {
//         [KernelFunction]
//         [Description("Copies the provided content to the clipboard.")]
//         public static void SetClipboard(string content)
//         {
//             if (string.IsNullOrWhiteSpace(content))
//             {
//                 return;
//             }

//             using Process clipProcess = Process.Start(
//                 new ProcessStartInfo
//                 {
//                     FileName = "clip",
//                     RedirectStandardInput = true,
//                     UseShellExecute = false,
//                 });

//             if (clipProcess != null)
//             {
//                 clipProcess.StandardInput.Write(content);
//                 clipProcess.StandardInput.Close();
//             }
//         }
//     }
// }