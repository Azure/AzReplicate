using System;

namespace AzReplicate.Core.Exceptions
{
    public class ReplicationFailedException : Exception
    {
        public string RequestId { get; set; }

        public ReplicationFailedException(string message) : base(message) { }

        public ReplicationFailedException(string message, string requestId) : this(message) 
        {
            RequestId = requestId;
        }

        public ReplicationFailedException(string message, Exception exception) : base(message, exception) { }
    }
}
