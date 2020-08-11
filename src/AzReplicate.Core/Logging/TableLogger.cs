using AzReplicate.Contracts.Configuration;
using AzReplicate.Contracts.Logging;
using AzReplicate.Contracts.Messaging;
using AzReplicate.Core.Extensions;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AzReplicate.Core.Logging
{
    public class TableLogger : ILogReplicationStatus
    {
        private readonly string _correlationId;
        private readonly CloudStorageAccount _storagAccount;
        private readonly CloudTableClient _tableClient;
        private readonly ConcurrentDictionary<string, CloudTable> _tables = new ConcurrentDictionary<string, CloudTable>();

        public TableLogger(ITableConfiguration configuration, ICorrelationConfiguration correlationConfiguration)
        {
            _correlationId = correlationConfiguration.CorrelationId;
            _storagAccount = CloudStorageAccount.Parse(configuration.TableConnectionString);
            _tableClient = _storagAccount.CreateCloudTableClient();
        }

        public async Task SuccessAsync(Replicatable replicatable, TimeSpan replicatedIn = default, CancellationToken cancellationToken = default)
        {
            var table = await CreateOrGetCloudTableAsync("Succeeded", cancellationToken);
            var operation = TableOperation.InsertOrMerge(CreateLogEntry(replicatable, replicatedIn.TotalSeconds));
            await table.ExecuteAsync(operation, cancellationToken);
        }

        public async Task FailureAsync(Replicatable replicatable, Exception e, CancellationToken cancellationToken = default)
        {
            var table = await CreateOrGetCloudTableAsync("Failed", cancellationToken);
            var operation = TableOperation.InsertOrMerge(CreateLogEntry(replicatable, e));
            await table.ExecuteAsync(operation, cancellationToken);
        }

        public async Task UnprocessableAsync(string id, string receipt, string content, Exception e, CancellationToken cancellationToken = default)
        {
            var table = await CreateOrGetCloudTableAsync("Unprocessable", cancellationToken);
            var operation = TableOperation.InsertOrMerge(CreateLogEntry(id, receipt, content, e));
            await table.ExecuteAsync(operation, cancellationToken);
        }

        private LogEntry CreateLogEntry(Replicatable replicatable, Exception e)
        {
            return CreateLogEntry(replicatable, null, e);
        }

        private LogEntry CreateLogEntry(Replicatable replicatable, double? replicatedInSeconds = null, Exception exception = null)
        {
            return new LogEntry
            {
                PartitionKey = replicatable.PartitionKey(),
                RowKey = replicatable.RowKey(),
                Source = replicatable.Source,
                Destination = replicatable.Destination,
                Content = JsonConvert.SerializeObject(replicatable),
                ReplicatedInSeconds = replicatedInSeconds,
                ReplicatedBytes = exception == null ? replicatable.SizeInBytes : null,
                ExceptionReason = exception?.Message,
                Exception = exception?.ToString(),
                CorrelationId = _correlationId,
                RequestId = exception?.RequestId()
            };
        }

        private LogEntry CreateLogEntry(string id, string receipt, string content, Exception exception)
        {
            return new LogEntry
            {
                PartitionKey = id,
                RowKey = receipt,
                Content = content,
                ExceptionReason = exception?.Message,
                Exception = exception?.ToString(),
                CorrelationId = _correlationId
            };
        }

        private async Task<CloudTable> CreateOrGetCloudTableAsync(string tableName, CancellationToken cancellationToken = default)
        {
            if (!_tables.ContainsKey(tableName))
            {
                var table = _tableClient.GetTableReference(tableName);
                await table.CreateIfNotExistsAsync(cancellationToken);
                _tables.TryAdd(tableName, table);
            }

            return _tables[tableName];
        }
    }
}
