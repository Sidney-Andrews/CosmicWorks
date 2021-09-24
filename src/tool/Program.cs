using Azure.Cosmos;
using CommandLine;
using CosmicWorks.Models;
using Flurl.Http;
using Humanizer;
using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CosmicWorks.Tool
{
    internal class Program
    {
        private static string _endpointUrl;
        private static string _authorizationKey;

        internal static async Task Main(string[] args) =>
            await Parser.Default.ParseArguments<Options>(args)
                .WithParsedAsync(RunAsync);

        private static async Task RunAsync(Options options)
        {
            if (options.Emulator)
            {
                // TODO: Add this to a configuration file
                _endpointUrl = "https://localhost:8081";
                _authorizationKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
            }
            else
            {
                _endpointUrl = String.IsNullOrWhiteSpace(options.Endpoint) ?
                    Prompt.GetString("Enter your Endpoint Uri:\t") :
                    options.Endpoint;

                _authorizationKey = String.IsNullOrWhiteSpace(options.Key) ?
                    Prompt.GetString("Enter your Authorization Key:\t") :
                    options.Key;
            }

            Console.WriteLine($"Endpoint:\t{_endpointUrl}");
            Console.WriteLine($"Auth Key:\t{_authorizationKey}{Environment.NewLine}");
            Console.WriteLine($"Revision:\t{options.Revision}");
            Console.WriteLine($"Datasets:{Environment.NewLine}\t{String.Join($"{Environment.NewLine}\t", options.Datasets)}{Environment.NewLine}");

            CosmosClient client = new CosmosClient(_endpointUrl, _authorizationKey);

            string databaseName =  options.Name ?? $"cosmicworks";
            
            CosmosDatabase clientDatabase = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            Console.WriteLine($"Database:\t[cosmicworks]\tStatus:\tCreated");

            foreach (var database in options.Datasets)
            {
                // TODO: This should be updated to read the local files instead of downloading from GitHub
                var container = (options.Revision, database) switch
                {
                    (Revision.v4, Dataset.product) =>
                        await BulkUpsertContent<Product>(clientDatabase, database, "https://raw.githubusercontent.com/azurecosmosdb/cosmicworks/master/data/cosmic-works-v4/product", "/categoryId", e => e.categoryId),
                    (Revision.v4, Dataset.customer) =>
                        await BulkUpsertContent<CustomerV4>(clientDatabase, database, "https://raw.githubusercontent.com/azurecosmosdb/cosmicworks/master/data/cosmic-works-v4/customer", "/customerId", e => e.customerId),
                    _ => throw new ArgumentOutOfRangeException("Revision and names do not map to known values")
                };
                Console.WriteLine($"{Environment.NewLine}Container:\t[{container}]\tStatus:\tPopulated");
            }
        }

        private static async Task<Dataset> BulkUpsertContent<T>(CosmosDatabase database, Dataset dataset, string uri, string partitionKey, Func<T, string> partitionKeyValue) where T : IEntity
        {
            // TODO: Make this configurable
            int parallelism = 200;

            string containerName = dataset.ToString().ToLower().Pluralize();

            CosmosContainer container = await database.CreateContainerIfNotExistsAsync($"{containerName}", partitionKeyPath: partitionKey, throughput: 4000);
            Console.WriteLine($"Container:\t[{containerName}]\tStatus:\tReady{Environment.NewLine}");
            IEnumerable<T> entities = await uri.GetJsonAsync<List<T>>();

            int batchIndex = 1;
            int batchCounter = 0;
            var concurrentOperations = new List<Task>(parallelism);
            foreach (T entity in entities)
            {
                concurrentOperations.Add(
                    container
                        .UpsertItemAsync<T>(entity, new PartitionKey(partitionKeyValue(entity)))
                        .ContinueWith(response => {
                            if (response.IsFaulted)
                            {
                                Console.WriteLine($"Crash:\t[{entity.id}]\tContainer:{containerName}\tReason:{response.Status}");
                            }
                            else
                            {
                                Console.WriteLine($"Entity:\t[{response.Result.Value.id}]\tContainer:{containerName}\tStatus:\t{response.Status}");
                            }
                        })
                );
                batchCounter++;
                if (batchCounter >= parallelism)
                {
                    await Task.WhenAll(concurrentOperations);
                    // TODO: Do we need to print the batch count here?
                    Console.WriteLine($"Batch:\t[{Guid.Parse(batchIndex.ToString("D32"))}]\tContainer:{containerName}\tStatus:\tComplete");
                    concurrentOperations.Clear();
                    batchIndex++;
                    batchCounter = 0;
                }
            }

            return dataset;
        }
    }
}
