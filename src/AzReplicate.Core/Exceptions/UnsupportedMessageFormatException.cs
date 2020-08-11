using System;

namespace AzReplicate.Core.Exceptions
{
    public class UnsupportedMessageFormatException : Exception
    {
        public UnsupportedMessageFormatException() : base() { }

        public UnsupportedMessageFormatException(Exception e) : base("Message format is unsupported.", e) { }
    }
}
