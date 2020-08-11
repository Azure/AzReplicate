using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AzReplicate.Contracts.Storage
{
    public interface IDealWithBlobs
    {
        Task<bool> ExistsWithCopySuccessAsync(string url, long? sizeInBytes = null, CancellationToken cancellationToken = default);

        Task<long?> GetContentLengthAsync(string url, CancellationToken cancellationToken = default);

        Task EnsureContainerExistsAsync(string path, CancellationToken cancellationToken = default);

        Task CopyBlobFromUrlAsync(string source, string destination, CancellationToken cancellationToken = default);

        Task PutBlockFromUrlAsync(string source, string destination, string blockId, long startByte, long? endByte, CancellationToken cancellationToken = default);

        Task PutBlockListAsync(string destination, IEnumerable<string> blockIds, CancellationToken cancellationToken = default);

        string GenerateBlockId(long offset);
    }
}
