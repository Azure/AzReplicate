using System;
using System.Collections.Generic;

namespace AzReplicate.Contracts.Telemetry
{
    public class QueuedTelemetry
    {
        public string Name { get; set; }

        public bool? Success { get; set; }

        public TimeSpan Duration { get; set; }

        public IDictionary<string, double> Metrics { get; }

        public IDictionary<string, string> Properties { get; }

        public QueuedTelemetry()
        {
            Metrics = new Dictionary<string, double>();
            Properties = new Dictionary<string, string>();
        }
    }
}
