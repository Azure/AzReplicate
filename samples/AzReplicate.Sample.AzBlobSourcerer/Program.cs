using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AzReplicate.Sample.AzBlobSourcerer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Setup our Queue Client in our DI Container
                    services.AddSingleton(x =>
                    {
                        return new QueueClient(
                            hostContext.Configuration.GetConnectionString("QueueStorageConnection"),
                            hostContext.Configuration["ReplicationQueueName"]);
                    });

                    // Setup any blob service connections for both source and destination(s)
                    services.AddSingleton(x =>
                    {
                        return new BlobServiceConnections(
                            hostContext.Configuration.GetSection("ConnectionStrings").GetChildren());
                    });

                    // Add the worker service that is going to do the work
                    services.AddHostedService<Worker>();
                });
    }
}
