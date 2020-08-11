using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AzReplicate.Sample.WebAppSourcerer
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
                    //configure the connection string for our DB context and add it to our DI Container
                    services.AddDbContext<ImageContext>(options =>
                        options.UseSqlServer(hostContext.Configuration.GetConnectionString("MyDbConnection")));

                    services.AddSingleton(x => 
                    {
                        return new QueueClient(
                            hostContext.Configuration.GetConnectionString("MyStorageConnection"), 
                            hostContext.Configuration["ReplicationQueueName"]);
                    });

                    services.AddHostedService<Worker>();

                });
    }
}
