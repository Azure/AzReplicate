using AzReplicate.Contracts.Messaging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AzReplicate.Contracts.Logging
{
    public interface ILogReplicationStatus
    {
        Task SuccessAsync(Replicatable replicatable, TimeSpan replicatedIn = default, CancellationToken cancellationToken = default);

        Task FailureAsync(Replicatable replicatable, Exception e, CancellationToken cancellationToken = default);

        Task UnprocessableAsync(string messageId, string popReceipt, string content, Exception e, CancellationToken cancellationToken = default);
    }
}
