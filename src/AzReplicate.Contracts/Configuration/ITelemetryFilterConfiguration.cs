using System.Collections.Generic;

namespace AzReplicate.Contracts.Configuration
{
    public interface ITelemetryFilterConfiguration
    {
        bool EnableAdaptiveSampling { get; set; }

        IEnumerable<string> FilteredDependencyTypes { get; set; }
    }
}
