using Polly;
using Polly.Extensions.Http;
using System;
using System.Net.Http;

namespace AzReplicate.Core.Rest
{
    public static class RetryPolicies
    {
        public static IAsyncPolicy<HttpResponseMessage> ExponentialRetry()
        {
            var jitterer = new Random();

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, 
                    retryAttempt => 
                        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + 
                        TimeSpan.FromMilliseconds(jitterer.Next(0, 100)));
        }
    }
}
