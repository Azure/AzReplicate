using System;
using System.Threading;
using System.Threading.Tasks;

namespace AzReplicate.Contracts.Messaging
{
    public interface IListenForMessages
    {
        Task ReceiveAndHandleAsync(CancellationToken cancellationToken = default);
    }
}
