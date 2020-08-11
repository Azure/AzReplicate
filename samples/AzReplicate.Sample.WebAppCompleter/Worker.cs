using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzReplicate.Sample.WebAppCompleter
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
            _logger.LogInformation($"Sample WebAppCompleter Worker Starting at: {DateTimeOffset.UtcNow}");
            await base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// Override the execute method on the background service to wire in logging and gracefull shutdown
        /// NOTE: this calls our Run method where the work of the completer happens
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.Register(() =>
            {
                _logger.LogInformation($"Sample WebAppCompleter Worker Cancelling at: {DateTimeOffset.UtcNow}");
            });

            try
            {
                _logger.LogInformation($"Sample WebAppCompleter Worker Running at: {DateTimeOffset.UtcNow}");

                await RunAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation($"Sample WebAppCompleter Worker Canceled at: {DateTimeOffset.UtcNow}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Sample WebAppCompleter Worker Unhandled Exception at: {DateTimeOffset.UtcNow}");

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
            _logger.LogInformation($"Sample WebAppCompleter Worker Ending at: {DateTimeOffset.UtcNow}");

            await base.StopAsync(cancellationToken);
        }
        #endregion


        /// <summary>
        /// This is where the work of the completer happens 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task RunAsync(CancellationToken cancellationToken)
        {
            var messageCount = 0;
            var backoff = 1;

            while (_queueClient.Exists())
            {
                // Receive and process messages in batches
                var receivedMessages = (await _queueClient.ReceiveMessagesAsync(20, TimeSpan.FromMinutes(5))).Value;

                // there was something to read out of the queue
                if (receivedMessages.Any())
                {
                    // reset the backoff
                    backoff = 1;

                    // iterate over the messages we got from the queue
                    foreach (var receivedMessage in receivedMessages)
                    {
                        // Deseralize the message
                        var message = JsonConvert.DeserializeObject<Replicatable>(receivedMessage.MessageText);

                        // Convert the detination uri string to a uri
                        var newUri = new Uri(message.Destination);

                        // lookup the image record in the database
                        var image = _imageContext.Images
                            .FirstOrDefault(x => x.ImageId == int.Parse(message.DiagnosticInfo["ImageId"]));

                        if (image != null)
                        {
                            // update the image record with the new url (without the SAS)
                            image.Url = newUri.GetLeftPart(UriPartial.Path);

                            // Save th changes to sql server
                            await _imageContext.SaveChangesAsync(cancellationToken);

                            // incrament our counter
                            messageCount++;
                        }

                        // Delete the message
                        _queueClient.DeleteMessage(receivedMessage.MessageId, receivedMessage.PopReceipt);
                    }

                    _logger.LogInformation($"Sample WebAppCompleter Worker batch complete. Total message count {messageCount} at {DateTimeOffset.UtcNow}");

                }
                else
                {
                    //queue empty, exponential backoff (in minutes)
                    await Task.Delay(backoff * 60000, cancellationToken);
                    backoff = backoff * 2;
                }

            }

            _logger.LogInformation($"Sample WebAppCompleter Worker Done updating the DB. Total message count {messageCount} at {DateTimeOffset.UtcNow}");


        }
    }
}
