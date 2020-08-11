using System;
using System.Threading;
using System.Threading.Tasks;
using AzReplicate.Contracts.Configuration;
using AzReplicate.Contracts.Messaging;
using AzReplicate.Contracts.Telemetry;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzReplicate
{
    public class Worker : BackgroundService
    {
        private readonly ICorrelationConfiguration _correlationConfiguration;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ILogger<Worker> _logger;
        private readonly IListenForMessages _messageListener;
        private readonly TelemetryClient _telemetryClient;
        private readonly IQueueTelemetry _telemetryQueue;

        public Worker(IHostApplicationLifetime hostApplicationLifetime, ILogger<Worker> logger, IListenForMessages messageListener, ICorrelationConfiguration correlationConfiguration, TelemetryClient telemetryClient, IQueueTelemetry telemetryQueue)
        {
            _correlationConfiguration = correlationConfiguration;
            _hostApplicationLifetime = hostApplicationLifetime;
            _logger = logger;
            _messageListener = messageListener;
            _telemetryClient = telemetryClient;
            _telemetryQueue = telemetryQueue;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker Starting at: {time} for correlation id: {id}", DateTimeOffset.Now, _correlationConfiguration.CorrelationId);
            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.Register(() =>
            {
                _logger.LogInformation("Worker Cancelling");
            });

            try
            {
                _logger.LogInformation("Worker Running at: {time} for correlation id: {id}", DateTimeOffset.Now, _correlationConfiguration.CorrelationId);
                
                await _messageListener.ReceiveAndHandleAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Operation Canceled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled Exception");
            }
            finally
            {
                _telemetryQueue.Flush();
                _telemetryClient.Flush();

                Task.Delay(TimeSpan.FromSeconds(30)).Wait();
                
                _hostApplicationLifetime.StopApplication();
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker Ending at: {time} for correlation id: {id}", DateTimeOffset.Now, _correlationConfiguration.CorrelationId);

            _telemetryQueue.Flush();
            _telemetryClient.Flush();

            Task.Delay(TimeSpan.FromSeconds(30)).Wait();

            await base.StopAsync(cancellationToken);
        }
    }
}
