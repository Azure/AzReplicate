using AzReplicate.Contracts.Configuration;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace AzReplicate.Core.Telemetry
{
    public class TelemetryInitializer : ITelemetryInitializer
    {
        private readonly ICorrelationConfiguration _correlationConfiguration;

        public TelemetryInitializer(ICorrelationConfiguration correlationConfiguration)
        {
            _correlationConfiguration = correlationConfiguration;
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (!telemetry.Context.GlobalProperties.ContainsKey("_CorrelationId"))
            {
                telemetry.Context.GlobalProperties.Add("_CorrelationId", _correlationConfiguration.CorrelationId);
            }
        }
    }
}
