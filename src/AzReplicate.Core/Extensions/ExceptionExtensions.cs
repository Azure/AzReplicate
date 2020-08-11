using AzReplicate.Core.Exceptions;
using System;

namespace AzReplicate.Core.Extensions
{
    public static class ExceptionExtensions
    {
        public static string RequestId(this Exception exception)
        {
            if (exception is ReplicationFailedException)
            {
                return ((ReplicationFailedException)exception).RequestId;
            }

            return null;
        }
    }
}
