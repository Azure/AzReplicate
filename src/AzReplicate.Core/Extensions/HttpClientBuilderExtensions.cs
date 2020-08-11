using AzReplicate.Core.Rest;
using Microsoft.Extensions.DependencyInjection;

namespace AzReplicate.Core.Extensions
{
    public static class HttpClientBuilderExtensions
    {
        public static IHttpClientBuilder WithExponentialRetry(this IHttpClientBuilder httpClientBuilder)
        {
            httpClientBuilder.AddPolicyHandler(RetryPolicies.ExponentialRetry());
            return httpClientBuilder;
        }
    }
}
