// using Microsoft.Extensions.VectorData;
// using Microsoft.SemanticKernel;
// using Microsoft.SemanticKernel.Embeddings;
// using System;
// using System.Collections.Generic;
// using System.ComponentModel;
// using System.Linq;
// using System.Threading.Tasks;
// using Xunit.Abstractions;

// #pragma warning disable CS0618,SKEXP0001, SKEXP0010, SKEXP0020,SKEXP0110


// public class VectorStorePlugin<TKey> where TKey : notnull
// {
//     private readonly IVectorStore vectorStore;
//     private readonly ITextEmbeddingGenerationService textEmbeddingGenerationService;

//     public VectorStorePlugin(IVectorStore vectorStore, ITextEmbeddingGenerationService textEmbeddingGenerationService)
//     {
//         this.vectorStore = vectorStore;
//         this.textEmbeddingGenerationService = textEmbeddingGenerationService;
//     }

//     [KernelFunction, Description("Searches the vector store for relevant information before calling any agent or plugin for FAQ")]
//     public async Task<string> SearchVectorStore(
//         [Description("The name of the collection to search.")] string collectionName,
//         [Description("The search input.")] string searchInput,
//         [Description("The number of top results to return.")] int topResults = 1)
//     {
//         try
//         {
//             var collection = vectorStore.GetCollection<TKey, FAQ<TKey>>(collectionName);
//             var searchVector = await textEmbeddingGenerationService.GenerateEmbeddingAsync(searchInput);
//             var searchResult = await collection.VectorizedSearchAsync(searchVector, new() { Top = topResults });
//             var resultRecords = await searchResult.Results.ToListAsync();

//             if (resultRecords.Count == 0)
//             {
//                 return "No results found.";
//             }

//             var results = resultRecords.Select(r => r.Record).ToList();
// //            var answers = results.Select(r => r.Answer).ToList();
//             var answersWithScores = resultRecords.Select(r =>
//             {
//                 double score = r.Score ?? 0.0; // Handle nullable double
//                 double percentage = Math.Round((1 - score) * 100, 2);
//                 return $"{r.Record.Answer} (Score: {score}, Matching Percentage: {percentage}%)";
//             }).ToList();
             
//              return string.Join("\n", answersWithScores);

//         }
//         catch (Exception ex)
//         {
//             return $"Error during search: {ex.Message}";
//         }
//     }
// }
// // public class VectorSearchPluginwhere<TKey>  where TKey : notnull
// // {
    
// // [KernelFunction("query vectorstore")]
// //    [Description(""" 
// //    Searches the vector store for relevant information for FAQ.
// // """)]
// //     public async Task<List<FAQ<TKey>>> SearchVectorStore(IVectorStore vectorStore, string collectionName, string searchInput, int topResults = 1)
// //     {
// //         try
// //         {
// //             var collection = vectorStore.GetCollection<TKey, FAQ<TKey>>(collectionName);
// //             var searchVector = await textEmbeddingGenerationService.GenerateEmbeddingAsync(searchInput);
// //             var searchResult = await collection.VectorizedSearchAsync(searchVector, new() { Top = topResults });
// //             var resultRecords = await searchResult.Results.ToListAsync();

// //             output.WriteLine($"Search string: {searchInput}");

// //             var results = resultRecords.Select(r => r.Record).ToList();

// //             foreach (var result in results)
// //             {
// //                 output.WriteLine($"Result: {result.Answer}");
// //             }
// //             if (resultRecords.Count > 0)
// //             {
// //                 output.WriteLine("Result 1 Score: " + resultRecords[0].Score);
// //             }
// //             output.WriteLine("");

// //             return results;
// //         }
// //         catch (Exception ex)
// //         {
// //             output.WriteLine($"Error in SearchVectorStore: {ex.Message}");
// //             return new List<FAQ<TKey>>();
// //         }
// //     }
// // }

// public class VectorStoreManager<TKey> where TKey : notnull 
// {
//     private readonly ITextEmbeddingGenerationService textEmbeddingGenerationService;
    

//     public VectorStoreManager(ITextEmbeddingGenerationService textEmbeddingGenerationService)
//     {
//         this.textEmbeddingGenerationService = textEmbeddingGenerationService;
        
//     }

//     public async Task IngestDataToVectorStore(IVectorStore vectorStore, string collectionName, Func<TKey> uniqueKeyGenerator)
//     {
//         try
//         {
//             var collection = vectorStore.GetCollection<TKey, FAQ<TKey>>(collectionName);
//             await collection.CreateCollectionIfNotExistsAsync();

//             var FAQEntries = CreateFAQEntries(uniqueKeyGenerator).ToList();

//             var tasks = FAQEntries.Select(entry => Task.Run(async () =>
//             {
//                 entry.QuestionEmbedding = await textEmbeddingGenerationService.GenerateEmbeddingAsync(entry.Question);
//             }));
//             await Task.WhenAll(tasks);

//             var upsertedKeysTasks = FAQEntries.Select(x => collection.UpsertAsync(x));
//             await Task.WhenAll(upsertedKeysTasks);

                   

//            Console.WriteLine($"IngestDataToVectorStore completed: {collectionName}");
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine($"Error in IngestDataToVectorStore: {ex.Message}");
//             }
//     }

//     // [KernelFunction, Description("Searches the vector store for relevant information.")]

//      public IEnumerable<FAQ<TKey>> CreateFAQEntries(Func<TKey> uniqueKeyGenerator)
//     {
//         yield return new FAQ<TKey>
//         {
//             QuestionId = uniqueKeyGenerator(),
//             Question = "What is the total number of stores in the database?",
//             Answer = "The total number of stores in the database is 3."
//         };

//         yield return new FAQ<TKey>
//         {
//             QuestionId = uniqueKeyGenerator(),
//             Question = "How many customers have placed orders in the last month?",
//             Answer = "A total of 1,200 customers have placed orders in the last month."
//         };

//         yield return new FAQ<TKey>
//         {
//             QuestionId = uniqueKeyGenerator(),
//             Question = "What is the average order value?",
//             Answer = "The average order value is $75.50."
//         };

//         yield return new FAQ<TKey>
//         {
//             QuestionId = uniqueKeyGenerator(),
//             Question = "Which store has the highest sales revenue?",
//             Answer = "Store ID 45 has the highest sales revenue with $500,000."
//         };

//         yield return new FAQ<TKey>
//         {
//             QuestionId = uniqueKeyGenerator(),
//             Question = "How many orders were placed online versus in-store?",
//             Answer = "There were 3,000 online orders and 2,500 in-store orders."
//         };

//         yield return new FAQ<TKey>
//         {
//             QuestionId = uniqueKeyGenerator(),
//             Question = "What is the most popular product category?",
//             Answer = "The most popular product category is Electronics."
//         };

//         yield return new FAQ<TKey>
//         {
//             QuestionId = uniqueKeyGenerator(),
//             Question = "How many new customers were added this quarter?",
//             Answer = "A total of 500 new customers were added this quarter."
//         };

//         yield return new FAQ<TKey>
//         {
//             QuestionId = uniqueKeyGenerator(),
//             Question = "What is the total sales revenue for the current year?",
//             Answer = "The total sales revenue for the current year is $2,000,000."
//         };

//         yield return new FAQ<TKey>
//         {
//             QuestionId = uniqueKeyGenerator(),
//             Question = "Which customer has placed the highest number of orders?",
//             Answer = "Customer ID 123 has placed the highest number of orders with 50 orders."
//         };

//         yield return new FAQ<TKey>
//         {
//             QuestionId = uniqueKeyGenerator(),
//             Question = "What is the average delivery time for orders?",
//             Answer = "The average delivery time for orders is 3 days."
//         };
//     }

   
    

// }

// public sealed class FAQ<TKey>
//     {
//         [VectorStoreRecordKey]
//         public TKey QuestionId { get; set; }

//         [VectorStoreRecordData(IsFullTextSearchable = true)]
//         public required string Question { get; set; }

//         [VectorStoreRecordData]
//         public required string Answer { get; set; }

//         [VectorStoreRecordVector(4, DistanceFunction.CosineDistance, IndexKind.Hnsw)]
//         public ReadOnlyMemory<float> QuestionEmbedding { get; set; }
//     }

