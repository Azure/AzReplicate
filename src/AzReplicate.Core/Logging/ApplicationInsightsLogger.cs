using AzReplicate.Contracts.Configuration;
using AzReplicate.Contracts.Logging;
using AzReplicate.Contracts.Messaging;
using AzReplicate.Contracts.Telemetry;
using AzReplicate.Core.Extensions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AzReplicate.Core.Logging
{
    public class ApplicationInsightsLogger : ILogReplicationStatus
    {
        private readonly ITelemetryConfiguration _telemetryConfiguration;
        private readonly TelemetryClient _telemetryClient;
        private readonly IQueueTelemetry _telemetryQueue;

        public ApplicationInsightsLogger(ITelemetryConfiguration telemetryConfiguration,  TelemetryClient telemetryClient, IQueueTelemetry telemetryQueue)
        {
            _telemetryConfiguration = telemetryConfiguration;
            _telemetryClient = telemetryClient;
            _telemetryQueue = telemetryQueue;
        }

        public async Task FailureAsync(Replicatable replicatable, Exception exception, CancellationToken cancellationToken = default)
        {
            var exceptionTelemetry = new ExceptionTelemetry(exception);

            AddDiagnosticInfo(exceptionTelemetry.Properties, replicatable.DiagnosticInfo);
            AddRequestId(exceptionTelemetry.Properties, exception?.RequestId());

            _telemetryClient.TrackException(exceptionTelemetry);

            await Task.CompletedTask;
        }

        public async Task SuccessAsync(Replicatable replicatable, TimeSpan replicatedIn = default, CancellationToken cancellationToken = default)
        {
            var queuedTelemetry = new QueuedTelemetry
            {
                Name = _telemetryConfiguration.QueuedTelemetryName ?? TelemetryConfigurationDefaults.QueuedTelemetryName,
                Duration = replicatedIn
            };

            queuedTelemetry.Metrics[_telemetryConfiguration.QueuedTelemetryMetricAggregatedCount ?? TelemetryConfigurationDefaults.QueuedTelemetryMetricAggregatedCount] = 1;

            queuedTelemetry.Metrics[_telemetryConfiguration.QueuedTelemetryMetricAggregatedDuration ?? TelemetryConfigurationDefaults.QueuedTelemetryMetricAggregatedDuration] = replicatedIn.TotalSeconds;
            
            if (replicatable.SizeInBytes.HasValue)
            {
                queuedTelemetry.Metrics[_telemetryConfiguration.QueuedTelemetryMetricAggregatedSize ?? TelemetryConfigurationDefaults.QueuedTelemetryMetricAggregatedSize] = (double) replicatable.SizeInBytes;
            }

            AddDiagnosticInfo(queuedTelemetry.Properties, replicatable.DiagnosticInfo);

            _telemetryQueue.Queue(queuedTelemetry);

            await Task.CompletedTask;
        }

        public async Task UnprocessableAsync(string messageId, string popReceipt, string content, Exception exception, CancellationToken cancellationToken = default)
        {
            var exceptionTelemetry = new ExceptionTelemetry(exception);

            AddDiagnosticInfo(exceptionTelemetry.Properties, new Dictionary<string, string> { { "_MessageId", messageId }, { "_PopReceipt", popReceipt } });
            AddRequestId(exceptionTelemetry.Properties, exception?.RequestId());

            _telemetryClient.TrackException(exceptionTelemetry);

            await Task.CompletedTask;
        }

        private void AddDiagnosticInfo(IDictionary<string, string> properties, IDictionary<string, string> diagnosticInfo)
        {
            if (diagnosticInfo != null)
            {
                foreach (var entry in diagnosticInfo)
                {
                    var key = entry.Key.StartsWith("_") ? entry.Key : $"_{entry.Key}";

                    if (key == "_CorrelationId")
                    {
                        key = $"_ParentCorrelationId";
                    }

                    properties[key] = entry.Value;
                }
            }
        }

        private void AddRequestId(IDictionary<string, string> properties, string requestId)
        {
            if (!string.IsNullOrEmpty(requestId))
            {
                properties.Add($"_RequestId", requestId);
            }
        }
    }
}
