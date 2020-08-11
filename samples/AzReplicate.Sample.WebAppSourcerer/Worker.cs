using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzReplicate.Sample.WebAppSourcerer
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly IConfiguration _configuration;
        private readonly ImageContext _imageContext;
        private readonly QueueClient _queueClient;

        public Worker(ILogger<Worker> logger,
            IHostApplicationLifetime hostApplicationLifetime,
            IConfiguration configuration,
            ImageContext imageContext,
            QueueClient queueClient)
        {
            _logger = logger;
            _hostApplicationLifetime = hostApplicationLifetime;
            _configuration = configuration;
            _imageContext = imageContext;
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
            _logger.LogInformation($"Sample WebAppSourcerer Worker Starting at: {DateTimeOffset.UtcNow}");
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
                _logger.LogInformation($"Sample WebAppSourcerer Worker Cancelling at: {DateTimeOffset.UtcNow}");
            });

            try
            {
                _logger.LogInformation($"Sample WebAppSourcerer Worker Running at: {DateTimeOffset.UtcNow}");

                await RunAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation($"Sample WebAppSourcerer Worker Canceled at: {DateTimeOffset.UtcNow}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Sample WebAppSourcerer Worker Unhandled Exception at: {DateTimeOffset.UtcNow}");

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
            _logger.LogInformation($"Sample WebAppSourcerer Worker Ending at: {DateTimeOffset.UtcNow}");

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

            //Ensure the queue exists
            _queueClient.CreateIfNotExists(cancellationToken: cancellationToken);

            //get config info
            string destinationPathRoot = _configuration["DestinationPathRoot"];
            string destinationSAS = _configuration["DestinationSAS"];

            //what images should we move
            var imagesToMove = _imageContext.Images
                .Where(x => x.Url.Contains(".amazonaws.com"));

            //iterate over all the images we want to copy, and write them to the queue
            foreach (var image in imagesToMove)
            {
                //Get the first half of the source url adding on the trailing slash
                var imageUriRoot = new Uri(image.Url).GetLeftPart(UriPartial.Authority) + "/";

                //create the message to put in the queue
                //we use the replicatable class to ensure that the message is in the format that
                //AzReplicate is expecting
                var message = new Replicatable
                {
                    //the file we want to copy, including any required SAS signature
                    Source = image.Url,

                    //the place we want the file to go, including any required SAS signature
                    //in this simple case we want to keep the same file structure in the destination account,
                    //so we are just replacing the front half of the URL and adding on the SAS signature for the destination
                    Destination = $"{image.Url.Replace(imageUriRoot, destinationPathRoot)}?{destinationSAS}",
                    
                    //Anything you pass along in the Diag Info will be available on the completer
                    //Here we are passing along the ID of the record in the database so we can update the
                    //URL in the completer. 
                    DiagnosticInfo = new Dictionary<string, string>()
                    {
                        { "ImageId", image.ImageId.ToString() }
                    }
                };

                //convert the message to Json and put it in the queue
                var serializedMessage = JsonConvert.SerializeObject(message);
                await _queueClient.SendMessageAsync(serializedMessage, timeToLive: TimeSpan.FromSeconds(-1), cancellationToken: cancellationToken);
                messageCount++;

                //show some progress in the logs
                if ((messageCount % 100) == 0)
                {
                    _logger.LogInformation($"Sample WebAppSourcerer Worker {messageCount} items added to the queue at {DateTimeOffset.UtcNow}");
                }

            }

            //log that we are all done
            _logger.LogInformation($"Sample WebAppSourcerer Worker Done adding items to the queue. Total message count {messageCount} at {DateTimeOffset.UtcNow}");


        }
    }
}
