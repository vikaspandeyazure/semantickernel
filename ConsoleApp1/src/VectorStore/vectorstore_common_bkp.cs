// // Copyright (c) Microsoft. All rights reserved.

// using Microsoft.Extensions.VectorData;
// using Microsoft.SemanticKernel.Embeddings;

// #pragma warning disable CS0618,SKEXP0001, SKEXP0010, SKEXP0020,SKEXP0110

// namespace Memory;

// /// <summary>
// /// This class is part of an example that shows how to ingest data into a vector store and then use vector search to find related records to a given string.
// /// The example shows how to write code that can be used with multiple database types.
// /// This class contains the common code.
// ///
// /// </summary>
// /// <param name="vectorStore">The vector store to ingest data into.</param>
// /// <param name="textEmbeddingGenerationService">The service to use for generating embeddings.</param>
// /// <param name="output">A helper to write output to the xunit test output stream.</param>
// public class VectorStore_Common(IVectorStore vectorStore, ITextEmbeddingGenerationService textEmbeddingGenerationService, ITestOutputHelper output)
// {
//     /// <summary>
//     /// Ingest data into a collection with the given name, and search over that data.
//     /// </summary>
//     /// <typeparam name="TKey">The type of key to use for database records.</typeparam>
//     /// <param name="collectionName">The name of the collection to ingest the data into.</param>
//     /// <param name="uniqueKeyGenerator">A function to generate unique keys for each record to upsert.</param>
//     /// <returns>An async task.</returns>
//     public async Task IngestDataAndSearchAsync<TKey>(string collectionName, Func<TKey> uniqueKeyGenerator)
//         where TKey : notnull
//     {
//         // Get and create collection if it doesn't exist.
//         var collection = vectorStore.GetCollection<TKey, FAQ<TKey>>(collectionName);
//         await collection.CreateCollectionIfNotExistsAsync();

 

//         // Create FAQ entries and generate embeddings for them.
//         var FAQEntries = CreateFAQEntries(uniqueKeyGenerator).ToList();
        
//         var tasks = FAQEntries.Select(entry => Task.Run(async () =>
//         {
//             entry.QuestionEmbedding = await textEmbeddingGenerationService.GenerateEmbeddingAsync(entry.Question);
//         }));
//         await Task.WhenAll(tasks);

//         // Upsert the FAQ entries into the collection and return their keys.
//         var upsertedKeysTasks = FAQEntries.Select(x => collection.UpsertAsync(x));
//         var upsertedKeys = await Task.WhenAll(upsertedKeysTasks);

//         // Search the collection using a vector search.
//         var searchString = "What is an Application Programming Interface";
//         var searchVector = await textEmbeddingGenerationService.GenerateEmbeddingAsync(searchString);
//         var searchResult = await collection.VectorizedSearchAsync(searchVector, new() { Top = 1 });
//         var resultRecords = await searchResult.Results.ToListAsync();

//         output.WriteLine("Search string: " + searchString);
//         output.WriteLine("Result: " + resultRecords.First().Record.Answer);
//         output.WriteLine("Result 1 Score: " + resultRecords[0].Score);
//         output.WriteLine();

     
//         // // Search the collection using a vector search with pre-filtering.
//         // searchString = "What is Retrieval Augmented Generation";
//         // searchVector = await textEmbeddingGenerationService.GenerateEmbeddingAsync(searchString);
//         // searchResult = await collection.VectorizedSearchAsync(searchVector, new() { Top = 3, Filter = g => g.Category == "External Definitions" });
//         // resultRecords = await searchResult.Results.ToListAsync();

     
//     }

    
   
 
// private static  IEnumerable<FAQ<TKey>> CreateFAQEntries<TKey>(Func<TKey> uniqueKeyGenerator)
// {
//     yield return new FAQ<TKey>
//     {
//         QuestionId = uniqueKeyGenerator(),
//         Question = "What is the total number of stores in the database?",
//         Answer = "The total number of stores in the database is 3."
//     };

//     yield return new FAQ<TKey>
//     {
//         QuestionId = uniqueKeyGenerator(),
//         Question = "How many customers have placed orders in the last month?",
//         Answer = "A total of 1,200 customers have placed orders in the last month."
//     };

//     yield return new FAQ<TKey>
//     {
//          QuestionId = uniqueKeyGenerator(),
//         Question = "What is the average order value?",
//         Answer = "The average order value is $75.50."
//     };

//     yield return new FAQ<TKey>
//     {
//          QuestionId = uniqueKeyGenerator(),
//         Question = "Which store has the highest sales revenue?",
//         Answer = "Store ID 45 has the highest sales revenue with $500,000."
//     };

//     yield return new FAQ<TKey>
//     {
//         QuestionId = uniqueKeyGenerator(),
//         Question = "How many orders were placed online versus in-store?",
//         Answer = "There were 3,000 online orders and 2,500 in-store orders."
//     };

//     yield return new FAQ<TKey>
//     {
//         QuestionId = uniqueKeyGenerator(),
//         Question = "What is the most popular product category?",
//         Answer = "The most popular product category is Electronics."
//     };

//     yield return new FAQ<TKey>
//     {
//         QuestionId = uniqueKeyGenerator(),
//         Question = "How many new customers were added this quarter?",
//         Answer = "A total of 500 new customers were added this quarter."
//     };

//     yield return new FAQ<TKey>
//     {
//         QuestionId = uniqueKeyGenerator(),
//         Question = "What is the total sales revenue for the current year?",
//         Answer = "The total sales revenue for the current year is $2,000,000."
//     };

//     yield return new FAQ<TKey>
//     {
//         QuestionId = uniqueKeyGenerator(),
//         Question = "Which customer has placed the highest number of orders?",
//         Answer = "Customer ID 123 has placed the highest number of orders with 50 orders."
//     };

//     yield return new FAQ<TKey>
//     {
//         QuestionId = uniqueKeyGenerator(),
//         Question = "What is the average delivery time for orders?",
//         Answer = "The average delivery time for orders is 3 days."
//     };
// }


    


//     public sealed class FAQ<TKey>
//     {
//     [VectorStoreRecordKey]
//     public TKey QuestionId { get; set; }

//     [VectorStoreRecordData(IsFullTextSearchable = true)]
//     public required string Question { get; set; }

//     [VectorStoreRecordData]
//     public required string Answer { get; set; }

//     [VectorStoreRecordVector(4, DistanceFunction.CosineDistance, IndexKind.Hnsw)]
//     public ReadOnlyMemory<float> QuestionEmbedding { get; set; }
//     }


// }