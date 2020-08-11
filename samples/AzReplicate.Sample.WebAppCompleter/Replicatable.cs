using System;
using System.Collections.Generic;
using System.Text;

namespace AzReplicate.Sample.WebAppCompleter
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
        /// Diagnostic Information for troubleshooting
        /// </summary>
        public Dictionary<string, string> DiagnosticInfo { get; set; }

        /// <summary>
        /// Object Metadata
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; }
    }
}
