using AzReplicate.Contracts.Configuration;
using AzReplicate.Contracts.Messaging;
using Azure.Storage.Queues;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AzReplicate.Core.Messaging
{
    public class StorageQueueSender : ISendMessages
    {
        private readonly IQueueConfiguration _configuration;

        private readonly Dictionary<string, QueueClient> _queueClients = new Dictionary<string, QueueClient>();

        public StorageQueueSender(IQueueConfiguration configuration)
        {
            _configuration = configuration;

            GetOrCreateQueueClientIfNotExists();
        }

        public async Task SendAsync(IEnumerable<Replicatable> replicatables, string queueName = null, CancellationToken cancellationToken = default)
        {
            await Task.WhenAll(replicatables.Select(r => SendAsync(r, queueName, cancellationToken)));
        }

        public async Task SendAsync(Replicatable replicatable, string queueName, CancellationToken cancellationToken = default)
        {
            var queueClient = GetOrCreateQueueClientIfNotExists(queueName, cancellationToken);

            var serializedMessage = JsonConvert.SerializeObject(replicatable);
            await queueClient.SendMessageAsync(serializedMessage, timeToLive: TimeSpan.FromSeconds(-1), cancellationToken: cancellationToken);
        }

        private QueueClient GetOrCreateQueueClientIfNotExists(string queueName = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(queueName))
            {
                queueName = _configuration.QueueName ?? ReplicationConfigurationDefaults.ReplicationQueueName;
            }

            if (!_queueClients.ContainsKey(queueName))
            {
                _queueClients[queueName] = new QueueClient(_configuration.QueueConnectionString, queueName);
                _queueClients[queueName].CreateIfNotExists(cancellationToken: cancellationToken);
            }

            return _queueClients[queueName];
        }
    }
}
