using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

#pragma warning disable CS0618,SKEXP0001, SKEXP0010, SKEXP0020,SKEXP0110

public static class GroupAgentFactory
{



public static ChatCompletionAgent CreateReviewerAgent(string reviewerName, Kernel kernel)
    {

    ChatCompletionAgent sqlrevieweragent =
    new()
    {
        Name = reviewerName,
        Instructions ="""
            You are a text summarization reviewer agent.
            RULES:
            - Be humble and polite in your responses.
            - Suggest how ouptut summarization can be more readable
            - Do not generate, execute or summarize any SQL queries.
            - Do not include any additional information.
            - Do not include any explanation or context.
            """,
                 
        Kernel = kernel,
        Arguments =
             new KernelArguments(
                new AzureOpenAIPromptExecutionSettings() 
                { 
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() 
                })
    };
    return sqlrevieweragent;
    }

public static ChatCompletionAgent CreateSQLGeneratorAgent(string generatorName, Kernel kernel)
    {

    ChatCompletionAgent sqlgeneratoragent =
    new()
    {
        Name = generatorName,
        Instructions ="""
            You are a Azure SQL Query Generating agent.Ensure the queries generated are optimized, secure, and follow best practices.
            Only generate SELECT queries from the database.Do not run this query in database by functions or by any methods.

            RULES:
            - Make sure you select the relavant tables only by analysing database schema, do not generate query for any table which is not a part of database
            - After generating the final select apply top 10 in generated query.
            - Do not include any comments in the query.
            - Do not include any additional text in the query.
            - Do not include any wrappers or markdown.
            - Do not include any explanation or context.
            - Do not include any additional information.
            - Do not call kernel functions to execute the query only generate SQL and return. 
            """,
        
         //"""Generate SQL queries based on the user's input.Ensure the queries are optimized, secure, and follow best practices. The queries should be able to handle various types of user requests, including data retrieval, updates, deletions, and insertions. Additionally, validate the input to prevent SQL injection attacks and provide meaningful error messages if the input is invalid.""",
        Kernel = kernel,
        Arguments =
             new KernelArguments(
                new AzureOpenAIPromptExecutionSettings() 
                { 
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() 
                })
    };
    return sqlgeneratoragent;
    }

 


    public static ChatCompletionAgent CreateSQLExecutorAgent(string executorName, Kernel Kernel)
    {
         ChatCompletionAgent sqlexecutoragent =
    new()
    {
        Name = executorName,
        Instructions = """
            You are a Azure SQL Executing agent.Ensure the queries are optimized, secure, and follow best practices.
            Additionally, validate the input to prevent SQL injection attacks and provide meaningful error messages if the input is invalid.
            Also you can ask for more information if required.
            
            RULES:
            - Only run valid SELECT queries
            - Do not run any Update.Insert,delete and drop queries
            - Return only the result of the executed queries
            - Do not return the input SQL query
            - Do not include any explanation or context or additional information
            """,
            Kernel = Kernel,
            Arguments =
             new KernelArguments(
                new AzureOpenAIPromptExecutionSettings() 
                { 
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() 
                })
    };
    return sqlexecutoragent;
    }
        
      

     public static ChatCompletionAgent CreateSQLSummarizerAgent(string summarizorName, Kernel kernel)
    {
        ChatCompletionAgent sqlsummarizeragent =
        new()
        {
            Name = summarizorName,
            Instructions = """
                You are a Azure SQL Query output Analyser

                RULES:
                - Analyze the Azure SQL Query output, identify key insights, trends, and anomalies, and generate a comprehensive and professional summary report.
                - Ensure the report is clear, concise, and highlights the most important findings.
                - Do not answer anything which is not related to the SQL Query output analysis.
                """,
            //"Analyze the Azure SQL Query output, identify key insights, trends, and anomalies, and generate a comprehensive and professional summary report. Ensure the report is clear, concise, and highlights the most important findings.",
            Kernel = kernel,
            Arguments =
                new KernelArguments(
                    new AzureOpenAIPromptExecutionSettings() 
                    { 
                        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() 
                    })
        };
        return sqlsummarizeragent;  
    }
}