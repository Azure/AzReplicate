using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzReplicate.Sample.AzBlobSourcerer
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly BlobServiceConnections _blobServiceConnections;
        private readonly QueueClient _queueClient;


        public Worker(ILogger<Worker> logger,
            IHostApplicationLifetime hostApplicationLifetime,
            BlobServiceConnections blobServiceConnections,
            QueueClient queueClient)
        {
            _logger = logger;
            _hostApplicationLifetime = hostApplicationLifetime;
            _blobServiceConnections = blobServiceConnections;
            _queueClient = queueClient;
        }

        #region Background Service Overrides

        /// <summary>
        /// Override the Start method on the background service to log start time
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Sample AzBlobSourcerer Worker Starting at: {DateTimeOffset.UtcNow}");
            await base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// Override the execute method on the background service to wire in logging and gracefull shutdown
        /// NOTE: this calls our Run method where the work of the sourcerer happens
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.Register(() =>
            {
                _logger.LogInformation($"Sample AzBlobSourcerer Worker Cancelling at: {DateTimeOffset.UtcNow}");
            });

            try
            {
                _logger.LogInformation($"Sample AzBlobSourcerer Worker Running at: {DateTimeOffset.UtcNow}");

                await RunAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation($"Sample AzBlobSourcerer Worker Canceled at: {DateTimeOffset.UtcNow}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Sample AzBlobSourcerer Worker Unhandled Exception at: {DateTimeOffset.UtcNow}");

            }
            finally
            {
                _hostApplicationLifetime.StopApplication();
            }
        }

        /// <summary>
        /// Override the stop method of the background service to wire in logging and gracefull shutdown
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Sample AzBlobSourcerer Worker Ending at: {DateTimeOffset.UtcNow}");

            await base.StopAsync(cancellationToken);
        }
        #endregion

        /// <summary>
        /// This is where the work of the sourcerer happens 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task RunAsync(CancellationToken cancellationToken)
        {
            int messageCount = 0;

            // Get a reference to our source blob client and SAS for AzReplicate to use to access it
            var sourceClient = _blobServiceConnections.SourceClient;
            var sourceSas = _blobServiceConnections.SourceSas;

            // Get a reference to our destination blob client(s) and SAS(s) for AzReplicate to use to access it
            var destinationClients = _blobServiceConnections.DestinationClients;
            var destinationSas = _blobServiceConnections.DestinationSas;

            //Ensure the queue exists
            _queueClient.CreateIfNotExists(cancellationToken: cancellationToken);

            //iterate over each of the containers in the source account
            await foreach (var sourceContainer in sourceClient.GetBlobContainersAsync(BlobContainerTraits.Metadata, cancellationToken: cancellationToken))
            {
                // get a container client to talk to the container
                var sourceContainerClient = sourceClient.GetBlobContainerClient(sourceContainer.Name);

                // iterate over each of the blobs in the source container
                await foreach (var sourceBlob in sourceContainerClient.GetBlobsAsync(BlobTraits.Metadata, cancellationToken: cancellationToken))
                {
                    // URI pointing to the source file with SAS
                    var sourceUri = new BlobUriBuilder(sourceClient.Uri);
                    sourceUri.BlobContainerName = sourceContainer.Name;
                    sourceUri.BlobName = sourceBlob.Name;
                    sourceUri.Query = sourceSas.ToString();

                    // URI pointing to the dest file with SAS
                    // If configured with more than one dest account, files will be round robin distributed accross all distination accounts
                    var destinationClientKey = destinationClients.ElementAt(messageCount % destinationClients.Count()).Key;
                    var destUri = new BlobUriBuilder(destinationClients[destinationClientKey].Uri);
                    destUri.BlobContainerName = sourceContainer.Name;
                    destUri.BlobName = sourceBlob.Name;
                    destUri.Query = destinationSas[destinationClientKey].ToString();

                    //create the message to put in the queue
                    //we use the replicatable class to ensure that the message is in the format that
                    //AzReplicate is expecting
                    var message = new Replicatable
                    {
                        //the file we want to copy, including required SAS signature
                        Source = sourceUri.ToString(),

                        //the place we want AzReplicate to put the file, including required SAS signature
                        Destination = destUri.ToString(),

                        //Anything you pass along in the Diag Info will be available on the completer
                        DiagnosticInfo = null,
                        
                        //Here we are passing along any metadata from the source
                        Metadata = sourceBlob.Metadata.ToDictionary(x => x.Key, x => x.Value)
                    };

                    //convert the message to Json and put it in the queue
                    var serializedMessage = JsonConvert.SerializeObject(message);
                    await _queueClient.SendMessageAsync(serializedMessage, timeToLive: TimeSpan.FromSeconds(-1), cancellationToken: cancellationToken);
                    messageCount++;

                    //show some progress in the logs
                    if ((messageCount % 100) == 0)
                    {
                        _logger.LogInformation($"Sample AzBlobSourcerer Worker {messageCount} items added to the queue at {DateTimeOffset.UtcNow}");
                    }

                }

                //log that we are all done
                _logger.LogInformation($"Sample AzBlobSourcerer Worker Done adding items to the queue. Total message count {messageCount} at {DateTimeOffset.UtcNow}");
            }
        }


    }
}
