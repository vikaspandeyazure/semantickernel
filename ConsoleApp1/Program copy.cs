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

// namespace AgentsSample;

// #pragma warning disable CS0618,SKEXP0001, SKEXP0010, SKEXP0020,SKEXP0110
// public class Program
// {
//     public static async Task Main()
//     {
//         // Load configuration from environment variables or user secrets.
//         Settings settings = new();

//         Console.WriteLine("Creating kernel...");
//         IKernelBuilder builder = Kernel.CreateBuilder();
//         string sqlConnectionString="Server=tcp:chatserverpoc.database.windows.net,1433;Initial Catalog=chattest;Authentication=Active Directory Default;";
//         builder.AddAzureOpenAIChatCompletion(
//             "gpt-4o",
//             "https://ai-vikaspandey5698ai656217437089.cognitiveservices.azure.com/",
//             "7ltE4YKQ4oDCpRm5uCuxZ6BbtHi8U5ONfE1vQKXmyivGF5szHh0TJQQJ99BBACHYHv6XJ3w3AAAAACOGx1DM");
//     // Add enterprise components
//         //builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));

//         Kernel kernel = builder.Build();
//        // var logger = kernel.GetRequiredService<ILogger<Program>>();
//         Kernel toolKernel = kernel.Clone();
//         toolKernel.Plugins.AddFromType<ClipboardAccess>();
//         //toolKernel.Plugins.AddFromObject(new SearchDatabasePlugin(kernel, logger,sqlConnectionString));
//         toolKernel.Plugins.AddFromObject(new SearchDatabasePlugin(kernel, sqlConnectionString));

//         Console.WriteLine("Defining agents...");

//         const string ReviewerName = "SQLChatReviewerAgent";
//         const string GeneratorName = "SQLGeneratorAgent";
//         const string ExecutorName = "SQLExecutorAgent";
//         const string SummarizorName = "SQLSummarizerAgent";

// ChatCompletionAgent agentReviewer =
//             new()
//             {
//                 Name = ReviewerName,
//                 Instructions =
//                     """
//                     You are a Chat reviewer agent,your responsiblity is understand user's query and take help from other agents to generate SQL queries based on the user's input.
                    
//                     RULES:
//                     - Only identify suggestions that are specific and actionable.
//                     - reply in bullet points.
//                     - Verify previous suggestions have been addressed.
//                     - Never repeat previous suggestions.
//                     """,
//                 Kernel = kernel,
//                 Arguments = new KernelArguments(new AzureOpenAIPromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() })
//             };


// ChatCompletionAgent sqlgeneratoragent =
//     new()
//     {
//         Name = GeneratorName,
//         Instructions ="""
//             You are a Chatting and Azure SQL Query Generating agent,follow only Azure SQL Syntax.Ensure the queries generated are optimized, secure, and follow best practices.
//             Additionally, validate the input to prevent SQL injection attacks and provide meaningful error messages if the input is invalid.
//             Only generate SELECT queries.Do not run this query by functions

//             RULES:
//             - Only generate SELECT queries which might not return more than 30 rows.
//             - Do not include any comments in the query.
//             - Do not include any additional text in the query.
//             - Do not include any wrappers or markdown.
//             - Do not include any explanation or context.
//             - Do not include any additional information.
//             """,
        
//          //"""Generate SQL queries based on the user's input.Ensure the queries are optimized, secure, and follow best practices. The queries should be able to handle various types of user requests, including data retrieval, updates, deletions, and insertions. Additionally, validate the input to prevent SQL injection attacks and provide meaningful error messages if the input is invalid.""",
//         Kernel = toolKernel,
//         Arguments =
//              new KernelArguments(
//                 new AzureOpenAIPromptExecutionSettings() 
//                 { 
//                     FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() 
//                 })
//     };

        
//        ChatCompletionAgent sqlexecutoragent =
//     new()
//     {
//         Name = ExecutorName,
//         Instructions = """
//             You are a Azure SQL Executing agent.Ensure the queries are optimized, secure, and follow best practices.
//             Additionally, validate the input to prevent SQL injection attacks and provide meaningful error messages if the input is invalid.
//             Also you can ask for more information if required.
            
//             RULES:
//             - Only run SELECT queries which might not return more than 50 rows.
//             - Return only the result of the executed queries.
//             - Do not include any explanation or context or additional information.
//             """,
//             Kernel = toolKernel,
//             Arguments =
//              new KernelArguments(
//                 new AzureOpenAIPromptExecutionSettings() 
//                 { 
//                     FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() 
//                 })
//     };

//     ChatCompletionAgent sqlsummarizeragent =
//     new()
//     {
//         Name = SummarizorName,
//         Instructions = """
//             Analyze the Azure SQL Query output, identify key insights, trends, and anomalies, and generate a comprehensive and professional summary report. 
//             Ensure the report is clear, concise, and highlights the most important findings.
//             """,
//         //"Analyze the Azure SQL Query output, identify key insights, trends, and anomalies, and generate a comprehensive and professional summary report. Ensure the report is clear, concise, and highlights the most important findings.",
//         Kernel = kernel,
//         Arguments =
//             new KernelArguments(
//                 new AzureOpenAIPromptExecutionSettings() 
//                 { 
//                     FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() 
//                 })
//     };  

//         KernelFunction selectionFunction =
//             AgentGroupChat.CreatePromptFunctionForStrategy(
//                 $$$"""
//                 Examine the provided RESPONSE and choose the next participant.
//                 State only the name of the chosen participant without explanation.
//                 Never choose the participant named in the RESPONSE.

//                 Choose only from these participants:
               
//                 - {{{GeneratorName}}}
//                 - {{{ExecutorName}}}
//                 - {{{SummarizorName}}}
                
//                 Always follow these rules when choosing the next participant:
//                 - If RESPONSE is user input, it is {{{GeneratorName}}}'s turn.
//                 - If RESPONSE is by {{{GeneratorName}}}, it is {{{ExecutorName}}}'s turn.
//                 - If RESPONSE is by {{{ExecutorName}}}, it is {{{SummarizorName}}}'s turn.
             

//                 RESPONSE:
//                 {{$lastmessage}}
//                 """,
//                 safeParameterNames: "lastmessage");

                


//         const string TerminationToken = "yes";

//         KernelFunction terminationFunction =
//             AgentGroupChat.CreatePromptFunctionForStrategy(
//                 $$$"""
//                 Examine the RESPONSE and determine whether the content has been deemed satisfactory.
//                 If content is satisfactory, respond with a single word without explanation: {{{TerminationToken}}}.
//                 If specific suggestions are being provided, it is not satisfactory.
//                 If no correction is suggested, it is satisfactory.

//                 RESPONSE:
//                 {{$lastmessage}}
//                 """,
//                 safeParameterNames: "lastmessage");

//         ChatHistoryTruncationReducer historyReducer = new(1);

//         AgentGroupChat chat =
//             new(sqlgeneratoragent,sqlexecutoragent,sqlsummarizeragent)
//             {
//                 ExecutionSettings = new AgentGroupChatSettings
//                 {
//                     SelectionStrategy =
//                         new KernelFunctionSelectionStrategy(selectionFunction, kernel)
//                         {
//                             // Always start with the editor agent.
//                             InitialAgent = sqlgeneratoragent,
//                             // Save tokens by only including the final response
//                             HistoryReducer = historyReducer,
//                             // The prompt variable name for the history argument.
//                             HistoryVariableName = "lastmessage",
//                             // Returns the entire result value as a string.
//                             ResultParser = (result) => result.GetValue<string>() ?? sqlgeneratoragent.Name
//                         },
//                     TerminationStrategy =
//                         new KernelFunctionTerminationStrategy(terminationFunction, kernel)
//                         {
//                             // Only evaluate for editor's response
//                             Agents = [sqlsummarizeragent],
//                             // Save tokens by only including the final response
//                             HistoryReducer = historyReducer,
//                             // The prompt variable name for the history argument.
//                             HistoryVariableName = "lastmessage",
//                             // Limit total number of turns
//                             MaximumIterations = 12,
//                             // Customer result parser to determine if the response is "yes"
//                             ResultParser = (result) => result.GetValue<string>()?.Contains(TerminationToken, StringComparison.OrdinalIgnoreCase) ?? false
//                         }
//                 }
//             };

//         Console.WriteLine("Ready!");

//         bool isComplete = false;
//         do
//         {
//             Console.WriteLine();
//             Console.Write("> ");
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
//                 await chat.ResetAsync();
//                 Console.WriteLine("[Converation has been reset]");
//                 continue;
//             }

//             if (input.StartsWith("@", StringComparison.Ordinal) && input.Length > 1)
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
//                 }
//                 catch (Exception)
//                 {
//                     Console.WriteLine($"Unable to access file: {filePath}");
//                     continue;
//                 }
//             }

//             chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, input));

//             chat.IsComplete = false;

//             try
//             {
//                 await foreach (ChatMessageContent response in chat.InvokeAsync())
//                 {
//                     Console.WriteLine();
//                     Console.WriteLine($"{response.AuthorName?.ToUpperInvariant() ?? "UNKNOWN"}:{Environment.NewLine}{response.Content}");
//                 }
//             }
//             catch (HttpOperationException exception)
//             {
//                 Console.WriteLine(exception.Message);
//                 if (exception.InnerException != null)
//                 {
//                     Console.WriteLine(exception.InnerException.Message);
//                     if (exception.InnerException.Data.Count > 0)
//                     {
//                         Console.WriteLine(JsonSerializer.Serialize(exception.InnerException.Data, new JsonSerializerOptions() { WriteIndented = true }));
//                     }
//                 }
//             }
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