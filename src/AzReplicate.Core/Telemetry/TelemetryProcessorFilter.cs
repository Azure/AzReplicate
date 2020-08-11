using AzReplicate.Contracts.Configuration;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System.Linq;

namespace AzReplicate.Core.Telemetry
{
    public class TelemetryProcessorFilter : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor _telemetryProcessor;
        private readonly ITelemetryFilterConfiguration _telemetryFilterConfiguration;

        public TelemetryProcessorFilter(ITelemetryProcessor telemetryProcessor, ITelemetryFilterConfiguration telemetryFilterConfiguration = null)
        {
            _telemetryProcessor = telemetryProcessor;
            _telemetryFilterConfiguration = telemetryFilterConfiguration;
        }

        public void Process(ITelemetry item)
        {
            var dependencyTelemetry = item as DependencyTelemetry;

            if (dependencyTelemetry != null 
                && dependencyTelemetry.Type != null
                && _telemetryFilterConfiguration != null
                && _telemetryFilterConfiguration.FilteredDependencyTypes != null 
                && _telemetryFilterConfiguration.FilteredDependencyTypes.Any()
                && _telemetryFilterConfiguration.FilteredDependencyTypes.Contains(dependencyTelemetry.Type))
            {
                return;
            }

            _telemetryProcessor.Process(item);
        }
    }
}
