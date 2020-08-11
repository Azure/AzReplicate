using AzReplicate.Core;
using AzReplicate.Core.Telemetry;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace AzReplicate
{
    public static class Bootstrapper
    {
        public static void Bootstrap(this IServiceCollection services, HostBuilderContext hostBuilderContext)
        {
            var appSettings = ConfigureAppSettings(hostBuilderContext);
            services.AddTransient(s => appSettings);

            var applicationInsightsSettings = ConfigureApplicationInsightsSettings(hostBuilderContext);
            services.AddTransient(s => applicationInsightsSettings);

            services
                .AddHostedService<Worker>();

            services
                .AddApplicationInsightsTelemetryWorkerService(applicationInsightsSettings)
                .AddApplicationInsightsTelemetryProcessor<TelemetryProcessorFilter>()
                .AddSingleton<ITelemetryInitializer, TelemetryInitializer>();

            services
                .AddCore(appSettings)
                    .WithTelemetryFilterConfiguration(applicationInsightsSettings);
        }

        private static AppSettings ConfigureAppSettings(HostBuilderContext hostBuilderContext)
        {
            var appSettings = new AppSettings { CorrelationId = Guid.NewGuid().ToString() };

            hostBuilderContext
                .Configuration
                .GetSection("AppSettings")
                .Bind(appSettings);

            return appSettings;
        }

        private static ApplicationInsightsSettings ConfigureApplicationInsightsSettings(HostBuilderContext hostBuilderContext) 
        {
            var applicationInsightsSettings = new ApplicationInsightsSettings();

            hostBuilderContext
                .Configuration
                .GetSection("Logging")
                .GetSection("ApplicationInsights")
                .Bind(applicationInsightsSettings);

            return applicationInsightsSettings;
        }
    }
}
