using AzReplicate.Contracts.Messaging;
using AzReplicate.Contracts.Replication;
using System.Threading;
using System.Threading.Tasks;

namespace AzReplicate.Core.Messaging
{
    public class MessageHandler : IHandleMessages
    {
        private readonly IReplicateBlobs _blobReplicator;

        public MessageHandler(IReplicateBlobs blobReplicator)
        {
            _blobReplicator = blobReplicator;
        }

        public async Task HandleAsync(Replicatable replicatable, CancellationToken cancellationToken = default)
        {
            await _blobReplicator.ReplicateAsync(replicatable, cancellationToken);
        }
    }
}
