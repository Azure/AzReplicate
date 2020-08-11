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
    public class TableLogReader : IReadReplicationStatusLogs
    {
        private readonly CloudStorageAccount _storagAccount;
        private readonly CloudTableClient _tableClient;
        private readonly ConcurrentDictionary<string, CloudTable> _tables = new ConcurrentDictionary<string, CloudTable>();

        public TableLogReader(ITableConfiguration configuration)
        {
            _storagAccount = CloudStorageAccount.Parse(configuration.TableConnectionString);
            _tableClient = _storagAccount.CreateCloudTableClient();
        }

        public async Task<ILogEntry> GetFailedAsync(Replicatable replicatable, CancellationToken cancellationToken)
        {
            var table = await CreateOrGetCloudTableAsync("Failed", cancellationToken);
            var operation = TableOperation.Retrieve<LogEntry>(replicatable.PartitionKey(), replicatable.RowKey());
            return (await table.ExecuteAsync(operation, cancellationToken)).Result as ILogEntry;
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
