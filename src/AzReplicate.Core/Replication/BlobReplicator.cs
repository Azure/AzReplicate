using AzReplicate.Contracts.Configuration;
using AzReplicate.Contracts.Messaging;
using AzReplicate.Contracts.Replication;
using AzReplicate.Contracts.Storage;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AzReplicate.Core.Replication
{
    public class BlobReplicator : IReplicateBlobs
    {
        private readonly IReplicationConfiguration _replicationConfiguration;
        private readonly IDealWithBlobs _blobStorage;

        public BlobReplicator(IReplicationConfiguration replicationConfiguration, IDealWithBlobs blobStorage)
        {
            _replicationConfiguration = replicationConfiguration;
            _blobStorage = blobStorage;
        }

        public async Task ReplicateAsync(Replicatable replicatable, CancellationToken cancellationToken = default)
        {
            if (!replicatable.SizeInBytes.HasValue)
            {
                replicatable.SizeInBytes = await GetContentSizeInBytesAsync(replicatable.Source, cancellationToken);
            }

            if (!await ExistsWithCopySuccessAndSimilarSizeAsync(replicatable.Destination, replicatable.SizeInBytes))
            {
                await EnsureContainerExistsAsync(replicatable.Destination, cancellationToken);

                if (CanBeProcessedInASingleRequest(replicatable))
                {
                    await CopyBlobFromUrlAsync(replicatable, cancellationToken);
                }
                else
                {
                    await CopyBlocksFromUrlAsync(replicatable, cancellationToken);
                }
            }
        }

        private async Task CopyBlobFromUrlAsync(Replicatable replicatable, CancellationToken cancellationToken = default)
        {
            await _blobStorage.CopyBlobFromUrlAsync(replicatable.Source, replicatable.Destination, cancellationToken);
        }

        private async Task CopyBlocksFromUrlAsync(Replicatable replicatable, CancellationToken cancellationToken = default)
        {
            var blockSize = _replicationConfiguration.ReplicationBlockSizeInBytes ?? ReplicationConfigurationDefaults.ReplicationBlockSizeInBytes;
            var numberOfBlocks = Math.Ceiling((decimal) replicatable.SizeInBytes / blockSize);

            var blockIds = new List<string>();
            var tasks = new List<Task>();

            for (var blockCounter = 0; blockCounter < numberOfBlocks; blockCounter++)
            {
                var blockId = _blobStorage.GenerateBlockId(blockSize * blockCounter);
                var startByte = blockCounter * blockSize;
                var endByte = Math.Min(startByte + blockSize - 1, replicatable.SizeInBytes.Value - 1);

                tasks.Add(_blobStorage.PutBlockFromUrlAsync(replicatable.Source, replicatable.Destination, blockId, startByte, endByte, cancellationToken));

                blockIds.Add(blockId);
            }

            await Task.WhenAll(tasks);

            await _blobStorage.PutBlockListAsync(replicatable.Destination, blockIds, cancellationToken);
        } 

        private async Task EnsureContainerExistsAsync(string url, CancellationToken cancellationToken = default)
        {
            await _blobStorage.EnsureContainerExistsAsync(url, cancellationToken);
        }

        private async Task<long?> GetContentSizeInBytesAsync(string url, CancellationToken cancellationToken = default)
        {
            if (!_replicationConfiguration.DisableGetSourceContentSizeInBytes)
            {
                return await _blobStorage.GetContentLengthAsync(url, cancellationToken);
            }

            return null;
        }

        private bool CanBeProcessedInASingleRequest(Replicatable replicatable)
        {
            return (replicatable.SizeInBytes ?? 0) / 1024 / 1024 <= 255;
        }

        private async Task<bool> ExistsWithCopySuccessAndSimilarSizeAsync(string url, long? sizeInBytes, CancellationToken cancellationToken = default)
        {
            if (!_replicationConfiguration.DisableExistsDestinationWithCopySuccessAndSimilarSize)
            {
                return await _blobStorage.ExistsWithCopySuccessAsync(url, sizeInBytes, cancellationToken);
            }

            return false;
        }
    }
}
