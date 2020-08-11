using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AzReplicate.Contracts.Messaging
{
    public interface ISendMessages
    {
        Task SendAsync(Replicatable message, string queueName = null, CancellationToken cancellationToken = default);

        Task SendAsync(IEnumerable<Replicatable> messages, string queueName = null, CancellationToken cancellationToken = default);
    }
}
