using AzReplicate.Contracts.Messaging;
using System.Threading;
using System.Threading.Tasks;

namespace AzReplicate.Contracts.Logging
{
    public interface IReadReplicationStatusLogs
    {
        Task<ILogEntry> GetFailedAsync(Replicatable replicatable, CancellationToken cancellationToken);
    }
}
