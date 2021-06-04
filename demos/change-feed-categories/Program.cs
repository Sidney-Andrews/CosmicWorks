using CosmicWorks.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CosmicWorks.Demos.ChangeFeedCategories
{
    internal class Program
    {
        //=================================================================
        //Load secrets
        private static IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(@"appSettings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<Secrets>();
        private static Secrets config = builder.Build().Get<Secrets>();

        private static readonly CosmosClient client = new CosmosClient(config.uri, config.key);
        private static readonly Database database = client.GetDatabase("database-v3");
        private static readonly Container productCategoryContainer = database.GetContainer("productCategory");
        private static readonly Container productContainer = database.GetContainer("product");

        static async Task Main(string[] args)
        {
            
            ContainerProperties leaseContainerProperties = new ContainerProperties("consoleLeases", "/id");
            Container leaseContainer = await database.CreateContainerIfNotExistsAsync(leaseContainerProperties, throughput: 400);

            var builder = productCategoryContainer.GetChangeFeedProcessorBuilder("ProductCategoryProcessor",
                async (IReadOnlyCollection<ProductCategory> input, CancellationToken cancellationToken) => 
                {
                    Console.WriteLine(input.Count + " Change(s) Received");

                    List<Task> tasks = new List<Task>();

                    //Fetch each change to productCategory container
                    foreach (ProductCategory item in input)
                    {
                        string categoryId = item.id;
                        string categoryName = item.name;

                        tasks.Add(UpdateProductCategoryName(categoryId, categoryName));
                    }

                    await Task.WhenAll(tasks);
                });

            var processor = builder
                .WithInstanceName("ChangeFeedProductCategories")
                .WithLeaseContainer(leaseContainer)
                .Build();

            await processor.StartAsync();
            Console.WriteLine("Started Change Feed Processor");
            Console.WriteLine("Press any key to stop the processor...");

            Console.ReadKey();

            Console.WriteLine("Stopping Change Feed Processor");

            await processor.StopAsync();
        }

        private static async Task UpdateProductCategoryName(string categoryId, string categoryName)
        {
            //query all products for the category
            string sql = $"SELECT * FROM c WHERE c.categoryId = '{categoryId}'";

            FeedIterator<Product> resultSet = productContainer.GetItemQueryIterator<Product>(
                new QueryDefinition(sql), 
                requestOptions: new QueryRequestOptions 
                { 
                    PartitionKey = new PartitionKey(categoryId) 
                });

            int productCount = 0;

            //Loop through all pages
            while (resultSet.HasMoreResults)
            {
                FeedResponse<Product> response = await resultSet.ReadNextAsync();

                //Loop through all products
                foreach (Product product in response)
                {
                    productCount++;
                    //update category name for product
                    product.categoryName = categoryName;
                    
                    //write the update back to product container
                    await productContainer.ReplaceItemAsync(
                        partitionKey: new PartitionKey(categoryId),
                        id: product.id,
                        item: product);
                }

                Console.WriteLine($"Updated {productCount} products with updated category name '{categoryName}'");
            }
        }
    }
}