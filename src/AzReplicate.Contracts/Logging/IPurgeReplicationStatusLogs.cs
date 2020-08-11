using AzReplicate.Contracts.Messaging;
using System.Threading;
using System.Threading.Tasks;

namespace AzReplicate.Contracts.Logging
{
    public interface IPurgeReplicationStatusLogs
    {
        Task PurgeFailureAsync(Replicatable replicatable, CancellationToken cancellationToken = default);

        Task TryPurgeFailureAsync(Replicatable replicatable, CancellationToken cancellationToken = default);
    }
}
