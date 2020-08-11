using AzReplicate.Contracts.Telemetry;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AzReplicate.Core.Telemetry
{
    public class TelemetryQueue : IQueueTelemetry
    {
        private const int Threshold = 32;

        private readonly TelemetryClient _telemetryClient;
        private readonly Timer _timer;
        private readonly ConcurrentQueue<QueuedTelemetry> _telemetry = new ConcurrentQueue<QueuedTelemetry>();

        public TelemetryQueue(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
            _timer = new Timer(FlushQueue, Threshold, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));
        }

        public void Flush()
        {
            _timer.Dispose();
            FlushQueue(_telemetry.Count);
        }

        public void Queue(QueuedTelemetry telemetry)
        {
            _telemetry.Enqueue(telemetry);
        }

        private void FlushQueue(dynamic amountOfItemsToFlush = null)
        {
            var name = "";
            var duration = TimeSpan.FromSeconds(0);
            var metricValues = new Dictionary<string, double>();

            for (var counter = 0; counter < amountOfItemsToFlush; counter++)
            {
                if (_telemetry.TryDequeue(out QueuedTelemetry telemetry))
                {
                    name = telemetry.Name;
                    duration = duration.Add(telemetry.Duration);

                    foreach (var metric in telemetry.Metrics)
                    {
                        if (!metricValues.ContainsKey($"_{metric.Key}"))
                        {
                            metricValues.Add($"_{metric.Key}", 0);
                        }

                        metricValues[$"_{metric.Key}"] += metric.Value;
                    }
                }
            }

            foreach (var metricValue in metricValues)
            {
                _telemetryClient.TrackMetric(metricValue.Key, metricValue.Value);
            }

            if (metricValues.Any())
            {
                _telemetryClient.TrackDependency(CreateDependencyTelemetry(name, duration, metricValues));
            }
            
            if (_telemetry.Count > 0)
            {
                FlushQueue(_telemetry.Count);
            }
        }

        private DependencyTelemetry CreateDependencyTelemetry(string name, TimeSpan duration, IDictionary<string, double> metrics)
        {
            var dependencyTelemetry = new DependencyTelemetry
            {
                Name = name,
                Duration = duration,
                Success = true
            };

            foreach (var metric in metrics)
            {
                dependencyTelemetry.Metrics.Add(metric.Key, metric.Value);
            }

            return dependencyTelemetry;
        }
    }
}
