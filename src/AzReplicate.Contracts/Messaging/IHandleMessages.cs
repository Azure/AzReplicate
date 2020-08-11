using System.Threading;
using System.Threading.Tasks;

namespace AzReplicate.Contracts.Messaging
{
    public interface IHandleMessages
    {
        Task HandleAsync(Replicatable message, CancellationToken cancellationToken = default);
    }
}
