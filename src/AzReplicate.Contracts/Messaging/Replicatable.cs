using System.Collections.Generic;

namespace AzReplicate.Contracts.Messaging
{
    public class Replicatable
    {
        /// <summary>
        /// The current location of the blob to copy
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// The location the blob should be copied to
        /// </summary>
        public string Destination { get; set; }

        /// <summary>
        /// The size of the blob in bytes (optional)
        /// </summary>
        public long? SizeInBytes { get; set; }

        /// <summary>
        /// Is this blob small enough to be copied using the Copy Blob from URL API (optional)
        /// <see cref="https://docs.microsoft.com/en-us/rest/api/storageservices/copy-blob-from-url"/>
        ///    This field will be ignored if SizeInBytes is specified
        /// </summary>
        public bool? BiggerThan256MiB { get; set; }

        /// <summary>
        /// Diagnostic Information for troubleshooting
        /// </summary>
        public Dictionary<string, string> DiagnosticInfo { get; set; }
    }
}
