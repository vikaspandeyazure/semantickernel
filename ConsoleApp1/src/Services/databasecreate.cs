
using Microsoft.Data.SqlClient;

public class DatabaseCreate
{
 /// <summary>
    /// Asynchronously checks if the 'production' and 'sales' schemas exist in the database. If they
    /// do not exist, the SQL script at the provided path is executed.
    /// </summary>
    /// <param name="connectionString">The connection string to the Azure SQL database.</param>
    /// <param name="sqlScriptPath">The path to the SQL script file.</param>
    public static async Task RunSqlScriptIfSchemasNotExistAsync(string connectionString, string sqlScriptPath)
    {
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();

            // Check for the existence of the schemas
            bool productionSchemaExists = await SchemaExistsAsync(connection, "production");
            bool salesSchemaExists = await SchemaExistsAsync(connection, "sales");

            if (!productionSchemaExists || !salesSchemaExists)
            {
                Console.WriteLine("One or more required schemas ('production', 'sales') do not exist.  Running script...");
                await RunSqlScriptAsync(connection, sqlScriptPath);
                Console.WriteLine("Schema creation script executed.");
            }
            else
            {
                Console.WriteLine("Required schemas ('production', 'sales') already exist.  No action taken.");
            }
        }
    }

    /// <summary>
    /// Asynchronously checks if a schema exists in the database.
    /// </summary>
    /// <param name="connection">The SqlConnection to use.</param>
    /// <param name="schemaName">The name of the schema to check.</param>
    /// <returns>True if the schema exists, false otherwise.</returns>
    private static async Task<bool> SchemaExistsAsync(SqlConnection connection, string schemaName)
    {
        string sql = "SELECT COUNT(*) FROM sys.schemas WHERE name = @schemaName";
        using (SqlCommand command = new SqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@schemaName", schemaName);
            // Use ExecuteScalarAsync
            int count = (int)await command.ExecuteScalarAsync();
            return count > 0;
        }
    }

    /// <summary>
    /// Asynchronously executes the SQL script against the database.
    /// </summary>
    /// <param name="connection">The SqlConnection to use.</param>
    /// <param name="sqlScriptPath">The path to the SQL script file.</param>
    private static async Task RunSqlScriptAsync(SqlConnection connection, string sqlScriptPath)
    {
        if (!File.Exists(sqlScriptPath))
        {
            throw new FileNotFoundException("SQL script file not found.", sqlScriptPath);
        }

        string sqlScript = File.ReadAllText(sqlScriptPath).Replace(System.Environment.NewLine, "");
        
        using (SqlCommand command = new SqlCommand(sqlScript, connection))
        {
            // Use ExecuteNonQueryAsync
            await command.ExecuteNonQueryAsync();
        }
    }
}