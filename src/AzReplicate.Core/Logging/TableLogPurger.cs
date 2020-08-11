using AzReplicate.Contracts.Configuration;
using AzReplicate.Contracts.Logging;
using AzReplicate.Contracts.Messaging;
using AzReplicate.Core.Extensions;
using Microsoft.Azure.Cosmos.Table;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace AzReplicate.Core.Logging
{
    public class TableLogPurger : IPurgeReplicationStatusLogs
    {
        private readonly CloudStorageAccount _storagAccount;
        private readonly CloudTableClient _tableClient;
        private readonly ConcurrentDictionary<string, CloudTable> _tables = new ConcurrentDictionary<string, CloudTable>();

        public TableLogPurger(ITableConfiguration configuration)
        {
            _storagAccount = CloudStorageAccount.Parse(configuration.TableConnectionString);
            _tableClient = _storagAccount.CreateCloudTableClient();
        }

        public async Task PurgeFailureAsync(Replicatable replicatable, CancellationToken cancellationToken = default)
        {
            var table = await CreateOrGetCloudTableAsync("Failed", cancellationToken);
            var operation = TableOperation.Delete(CreateLogEntry(replicatable));
            await table.ExecuteAsync(operation, cancellationToken);
        }

        public async Task TryPurgeFailureAsync(Replicatable replicatable, CancellationToken cancellationToken = default)
        {
            try
            {
                await PurgeFailureAsync(replicatable, cancellationToken);
            }
            catch { }
        }

        private LogEntry CreateLogEntry(Replicatable replicatable)
        {
            return new LogEntry
            {
                PartitionKey = replicatable.PartitionKey(),
                RowKey = replicatable.RowKey(),
                ETag = "*"
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
