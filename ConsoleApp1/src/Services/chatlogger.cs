using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using  AgentsSample;
public class ChatLogger
{
    private readonly Container _container;

    public ChatLogger(Database database, string containerId)
    {
        if (database == null) throw new ArgumentNullException(nameof(database));
        if (string.IsNullOrEmpty(containerId)) throw new ArgumentNullException(nameof(containerId));

        _container = database.GetContainer(containerId);
        //var testDocument = new { id = Guid.NewGuid().ToString(), Name = "Test Document" };
        // _container.CreateItemAsync(testDocument, new PartitionKey(testDocument.id));


    }

    public async Task<ChatLogEntry> CreateChatLogEntry(ChatLogEntry chatLogEntry)
    {
        if (chatLogEntry == null) throw new ArgumentNullException(nameof(chatLogEntry));

        var response = await _container.CreateItemAsync(chatLogEntry);
        return response.Resource;
    }

    public async Task<ChatLogEntry> GetChatLogEntry(string id, PartitionKey partitionKey)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

        try
        {
            var response = await _container.ReadItemAsync<ChatLogEntry>(id, partitionKey);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<List<ChatLogEntry>> GetChatLogEntriesByUserInput(string userInput)
    {
        if (string.IsNullOrEmpty(userInput)) throw new ArgumentNullException(nameof(userInput));

        var queryable = _container.GetItemLinqQueryable<ChatLogEntry>();
        var iterator = queryable.Where(c => c.UserInput == userInput).ToFeedIterator();

        var results = new List<ChatLogEntry>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response.ToList());
        }
        return results;
    }

    // public async Task<ChatLogEntry> UpdateChatLogEntry(ChatLogEntry chatLogEntry)
    // {
    //     if (chatLogEntry == null) throw new ArgumentNullException(nameof(chatLogEntry));

    //     // Corrected line:
    //     var response = await _container.ReplaceItemAsync(chatLogEntry.Id, chatLogEntry);

    //     return response.Resource;
    // }

    public async Task DeleteChatLogEntry(string id, PartitionKey partitionKey)
    {
        if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

        await _container.DeleteItemAsync<ChatLogEntry>(id, partitionKey);
    }

    
}

    public class ChatLogEntry
    {
        public string id { get; set; } = Guid.NewGuid().ToString();
        public string UserInput { get; set; }
        public List<AgentResponse> AgentResponses { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class AgentResponse
    {
        public string AgentName { get; set; }
        public string Response { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public LogTokens Tokens { get; set; }
    }

    public class LogTokens
    {
        public int Input { get; set; }
        public int Output { get; set; }
        public double InputCost { get; set; }
        public double OutputCost { get; set; }
        public int Total { get; set; }
    }