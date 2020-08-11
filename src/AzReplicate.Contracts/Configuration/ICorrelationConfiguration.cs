using System;
using System.Collections.Generic;
using System.Text;

namespace AzReplicate.Contracts.Configuration
{
    public interface ICorrelationConfiguration
    {
        public string CorrelationId { get; }
    }
}
