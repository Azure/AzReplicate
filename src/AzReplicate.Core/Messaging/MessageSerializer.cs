using AzReplicate.Contracts.Messaging;
using AzReplicate.Core.Exceptions;
using Newtonsoft.Json;
using System;

namespace AzReplicate.Core.Messaging
{
    public static class MessageSerializer
    {
        public static string Serialize(Replicatable replicatable)
        {
            return JsonConvert.SerializeObject(replicatable);
        }

        public static Replicatable Deserialize(string serialized)
        {
            try
            {
                return (Replicatable)JsonConvert.DeserializeObject(serialized, typeof(Replicatable));
            }
            catch (Exception e)
            {
                throw new UnsupportedMessageFormatException(e);
            }
        }
    }
}
