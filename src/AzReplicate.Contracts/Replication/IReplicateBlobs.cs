using AzReplicate.Contracts.Messaging;
using System.Threading;
using System.Threading.Tasks;

namespace AzReplicate.Contracts.Replication
{
    public interface IReplicateBlobs
    {
        Task ReplicateAsync(Replicatable replicatable, CancellationToken cancellationToken = default);
    }
}
