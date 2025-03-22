using System.ComponentModel;
using Microsoft.SemanticKernel;
using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Data.Common;

public class SearchDatabasePluginNoLog(Kernel kernel, string connectionString)
//public class SearchDatabasePlugin(Kernel kernel, string connectionString)
{   
    //private readonly ILogger logger = logger;
    private readonly Kernel kernel = kernel;    
    private readonly string connectionString = connectionString;
/* Query the SALES database to return data for the given query, The high-level schema of the database as follows:
         TABLE production.products : columns [product_id],[product_name],[brand_id],[category_id],[model_year],[list_price]
         TABLE production.stocks : [stock_id],[product_id],[quantity],[store_id]       
         TABLE production.categories : [category_id],[category_name]
         TABLE production.brands : [brand_id],[brand_name]
         TABLE sales.customers : [customer_id],[first_name],[last_name],[phone],[email],[street],[city],[state],[zip_code]
         TABLE sales.stores : [store_id],[store_name],[phone],[email],[street],[city],[state],[zip_code]
         TABLE sales.staffs : [staff_id],[first_name],[last_name],[email],[phone],[active],[store_id],[manager_id]
         TABLE sales.orders : [order_id],[order_date],[status],[staff_id],[customer_id]
         TABLE sales.order_items : [order_id],[item_id],[product_id],[quantity],[list_price],[discount]*/

         //Query the SALES database to return data for the given query,run the procedure dbo.GetDatabaseSchema for the high-level schema of the database   
    [KernelFunction("query_database")]
   [Description("""
         Query the SALES database to return data for the given query, The high-level schema of the database as follows:
         TABLES production.products : columns [product_id],[product_name],[brand_id],[category_id],[model_year],[list_price]
         production.stocks : [stock_id],[product_id],[quantity],[store_id]       
         production.categories : [category_id],[category_name]
         production.brands : [brand_id],[brand_name]
         sales.customers : [customer_id],[first_name],[last_name],[phone],[email],[street],[city],[state],[zip_code]
         sales.stores : [store_id],[store_name],[phone],[email],[street],[city],[state],[zip_code]
         sales.staffs : [staff_id],[first_name],[last_name],[email],[phone],[active],[store_id],[manager_id]
         sales.orders : [order_id],[order_date],[status],[staff_id],[customer_id]
         sales.order_items : [order_id],[item_id],[product_id],[quantity],[list_price],[discount]
         
""")]
        
    public async Task<IEnumerable<dynamic>> QueryDatabase(string sqlquery)
    {        
        //logger.LogInformation($"------------------------Querying the database for '{query}'");

        //var ai = kernel.GetRequiredService<IChatCompletionService>();
        //var chat = new ChatHistory(@"You create T-SQL queries for Azure SQL Server based on the given user request and the provided schema. 
    //Just return T-SQL query to be executed. Do not return other text or explanation. Don't use markdown or any wrappers.
       // Ensure that the query is safe and does not contain any malicious code.
       // ");

        //chat.AddUserMessage(query);
        //var response = await ai.GetChatMessageContentAsync(chat);
        //if (response.Content == null)
        if(sqlquery==null)
        {
           // logger.LogWarning("------------------------AI was not able to generate a SQL query.");
            return [];
        }

        //string sqlQuery = response.Content.Replace("```sql", "").Replace("```", "");

        //logger.LogInformation($"-------------------------Executing the following query: {sqlquery}");
        
        await using var connection = new SqlConnection(connectionString);
        var result = await connection.QueryAsync(sqlquery);

        return result;
    }

}

public class SearchDatabasePlugin(Kernel kernel, ILogger logger, string connectionString)
//public class SearchDatabasePlugin(Kernel kernel, string connectionString)
{   
    private readonly ILogger logger = logger;
    private readonly Kernel kernel = kernel;    
    private readonly string connectionString = connectionString;
/*
 Query the SALES database to return data for the given query, The high-level schema of the database as follows:
         TABLE production.products : columns [product_id],[product_name],[brand_id],[category_id],[model_year],[list_price]
         TABLE production.stocks : [stock_id],[product_id],[quantity],[store_id]       
         TABLE production.categories : [category_id],[category_name]
         TABLE production.brands : [brand_id],[brand_name]
         TABLE sales.customers : [customer_id],[first_name],[last_name],[phone],[email],[street],[city],[state],[zip_code]
         TABLE sales.stores : [store_id],[store_name],[phone],[email],[street],[city],[state],[zip_code]
         TABLE sales.staffs : [staff_id],[first_name],[last_name],[email],[phone],[active],[store_id],[manager_id]
         TABLE sales.orders : [order_id],[order_date],[status],[staff_id],[customer_id]
         TABLE sales.order_items : [order_id],[item_id],[product_id],[quantity],[list_price],[discount]
*/
    [KernelFunction("query_azure_sql_database")]
    [Description("""
         Query the sql database to return data for the given query 
         
""")]
        
    public async Task<IEnumerable<dynamic>> QueryDatabase(string sqlquery)
    {        
        //logger.LogInformation($"------------------------Querying the database for '{query}'");

        //var ai = kernel.GetRequiredService<IChatCompletionService>();
        //var chat = new ChatHistory(@"You create T-SQL queries for Azure SQL Server based on the given user request and the provided schema. 
    //Just return T-SQL query to be executed. Do not return other text or explanation. Don't use markdown or any wrappers.
       // Ensure that the query is safe and does not contain any malicious code.
       // ");

        //chat.AddUserMessage(query);
        //var response = await ai.GetChatMessageContentAsync(chat);
        //if (response.Content == null)
        if(sqlquery==null)
        {
           // logger.LogWarning("------------------------AI was not able to generate a SQL query.");
            return [];
        }

        //string sqlQuery = response.Content.Replace("```sql", "").Replace("```", "");

        //logger.LogInformation($"-------------------------Executing the following query: {sqlquery}");
        
        await using var connection = new SqlConnection(connectionString);
        var result = await connection.QueryAsync(sqlquery);

        return result;
    }

    // [KernelFunction("find_sessions_similar_to_topic")]
    // [Description("Return a list of sessions at SQL Konferenz 2024 at that are similar to a specific topic or by a specific speaker name specified in the provided topic parameter. If no results are found, an empty list is returned. This function only return data from the SQL Konferenz 2024 conference.")]
    // public async Task<IEnumerable<Session>> GetSessionSimilarToTopic(string topic)
    // {        
    //     logger.LogInformation($"Searching for sessions related to '{topic}'");

    //     DefaultTypeMap.MatchNamesWithUnderscores = true;

    //     await using var connection = new SqlConnection(connectionString);
    //     var sessions = await connection.QueryAsync<Session>("web.find_similar_sessions", 
    //         new { 
    //             topic                
    //         }, 
    //         commandType: CommandType.StoredProcedure
    //     );
                     
    //     return sessions;    
    // }
}