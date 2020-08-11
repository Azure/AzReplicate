using AzReplicate.Contracts.Configuration;
using AzReplicate.Contracts.Logging;
using AzReplicate.Contracts.Messaging;
using AzReplicate.Contracts.Replication;
using AzReplicate.Contracts.Storage;
using AzReplicate.Contracts.Telemetry;
using AzReplicate.Core.Extensions;
using AzReplicate.Core.Logging;
using AzReplicate.Core.Messaging;
using AzReplicate.Core.Replication;
using AzReplicate.Core.Rest;
using AzReplicate.Core.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AzReplicate.Core
{
    public static class Bootstrapper
    {
        public static IServiceCollection AddCore(this IServiceCollection services, dynamic configuration)
        {
            AddTransientIfImplements<IQueueConfiguration>(services, configuration);
            AddTransientIfImplements<ITableConfiguration>(services, configuration);
            AddTransientIfImplements<IReplicationConfiguration>(services, configuration);
            AddTransientIfImplements<ICorrelationConfiguration>(services, configuration);
            AddTransientIfImplements<IQueueListeningConfiguration>(services, configuration);
            AddTransientIfImplements<ITelemetryConfiguration>(services, configuration);

            services.AddTransient<IListenForMessages, StorageQueueListener>();
            services.AddTransient<ISendMessages, StorageQueueSender>();
            services.AddTransient<IHandleMessages, MessageHandler>();
            services.AddTransient<IReplicateBlobs, BlobReplicator>();
            services.AddTransient<ILogReplicationStatus, TableLogger>();
            services.AddTransient<ILogReplicationStatus, QueueLogger>();
            services.AddTransient<ILogReplicationStatus, ApplicationInsightsLogger>();
            services.AddTransient<IPurgeReplicationStatusLogs, TableLogPurger>();
            services.AddTransient<IReadReplicationStatusLogs, TableLogReader>();
            services.AddSingleton<IQueueTelemetry, TelemetryQueue>();

            services
                .AddHttpClient<IDealWithBlobs, BlobRestClient>()
                .WithExponentialRetry();

            return services;
        }

        public static IServiceCollection WithQueueConfiguration(this IServiceCollection services, IQueueConfiguration queueConfiguration)
        {
            services.RemoveAll<IQueueConfiguration>();
            services.AddTransient(s => queueConfiguration);

            return services;
        }

        public static IServiceCollection WithTableConfiguration(this IServiceCollection services, ITableConfiguration tableConfiguration)
        {
            services.RemoveAll<ITableConfiguration>();
            services.AddTransient(s => tableConfiguration);

            return services;
        }

        public static IServiceCollection WithReplicationConfiguration(this IServiceCollection services, IReplicationConfiguration replicationConfiguration)
        {
            services.RemoveAll<IReplicationConfiguration>();
            services.AddTransient(s => replicationConfiguration);

            return services;
        }

        public static IServiceCollection WithCorrelationConfiguration(this IServiceCollection services, ICorrelationConfiguration correlationConfiguration)
        {
            services.RemoveAll<ICorrelationConfiguration>();
            services.AddTransient(s => correlationConfiguration);

            return services;
        }

        public static IServiceCollection WithMessageListener<TMessageListener>(this IServiceCollection services) where TMessageListener : class, IListenForMessages
        {
            services.RemoveAll<IListenForMessages>();
            services.AddTransient<IListenForMessages, TMessageListener>();

            return services;
        }

        public static IServiceCollection WithMessageSender<TMessageSender>(this IServiceCollection services) where TMessageSender : class, ISendMessages
        {
            services.RemoveAll<ISendMessages>();
            services.AddTransient<ISendMessages, TMessageSender>();

            return services;
        }

        public static IServiceCollection WithMessageHandler<TMessageHandler>(this IServiceCollection services) where TMessageHandler : class, IHandleMessages
        {
            services.RemoveAll<IHandleMessages>();
            services.AddTransient<IHandleMessages, TMessageHandler>();

            return services;
        }

        public static IServiceCollection WithoutBlobReplicator(this IServiceCollection services)
        {
            services.RemoveAll<IReplicateBlobs>();
            return services;
        }

        public static IServiceCollection WithoutReplicationStatusLoggers(this IServiceCollection services)
        {
            services.RemoveAll<ILogReplicationStatus>();
            return services;
        }

        public static IServiceCollection WithReplicationStatusLogger<TReplicationStatusLogger>(this IServiceCollection services) where TReplicationStatusLogger : class, ILogReplicationStatus
        {
            services.AddTransient<ILogReplicationStatus, TReplicationStatusLogger>();
            return services;
        }

        public static IServiceCollection WithTelemetryFilterConfiguration(this IServiceCollection services, ITelemetryFilterConfiguration telemetryFilterConfiguration)
        {
            services.AddTransient(s => telemetryFilterConfiguration);
            return services;
        }

        private static void AddTransientIfImplements<TContract>(IServiceCollection services, dynamic implementation) where TContract : class
        {
            if (implementation is TContract)
            {
                services.AddTransient<TContract>(s => implementation);
            } 
            else
            {
                services.AddTransient<TContract>(s => null);
            }
        }
    }
}
