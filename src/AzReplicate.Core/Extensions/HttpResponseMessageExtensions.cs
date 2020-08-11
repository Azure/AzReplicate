using AzReplicate.Core.Exceptions;
using AzReplicate.Core.Rest;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;

namespace AzReplicate.Core.Extensions
{
    public static class HttpResponseMessageExtensions
    {
        public static void EnsureSuccess(this HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode || !response.IsCopySuccess())
            {
                throw new ReplicationFailedException(BuildExceptionMessage(response), TryGetHeaderValue(response, "x-ms-request-id"));
            }
        }

        public static bool IsCopySuccess(this HttpResponseMessage response)
        {
            var copyStatus = TryGetHeaderValue(response, "x-ms-copy-status");
            return string.IsNullOrEmpty(copyStatus) 
                || (!string.IsNullOrEmpty(copyStatus) && ConvertToCopyStatus(copyStatus) == CopyStatus.Success);
        }

        public static string TryGetHeaderValue(this HttpResponseMessage response, string header)
        {
            if ((bool) response.Headers?.Contains(header))
            {
                return response.Headers?.GetValues(header)?.FirstOrDefault();
            }

            return null;
        }

        private static CopyStatus ConvertToCopyStatus(string copyStatusValue)
        {
            var capitalizedCopyStatusValue = copyStatusValue[0].ToString().ToUpper() + copyStatusValue.Substring(1);
            return Enum.Parse<CopyStatus>(capitalizedCopyStatusValue);
        }

        private static string BuildExceptionMessage(HttpResponseMessage response)
        {
            var requestId = TryGetHeaderValue(response, "x-ms-request-id");
            var copyStatus = TryGetHeaderValue(response, "x-ms-copy-status");

            var exception = "";

            if (!response.IsSuccessStatusCode)
            {
                if (!string.IsNullOrEmpty(requestId))
                {
                    exception = string.Format(CultureInfo.InvariantCulture, $"Response status code does not indicate success: {response.StatusCode} ({HttpStatusDescription.Get(response.StatusCode)}) [{requestId}].");
                }
                else
                {
                    exception = string.Format(CultureInfo.InvariantCulture, $"Response status code does not indicate success: {response.StatusCode} ({HttpStatusDescription.Get(response.StatusCode)}).");
                }
            } 
            else if (!response.IsCopySuccess())
            {
                if (!string.IsNullOrEmpty(requestId))
                {
                    exception = string.Format(CultureInfo.InvariantCulture, $"Response status code does indicate success but background copy operation failed: {response.StatusCode} ({HttpStatusDescription.Get(response.StatusCode)} | Copy {copyStatus}) [{requestId}].");
                }
                else
                {
                    exception = string.Format(CultureInfo.InvariantCulture, $"Response status code does indicate success but background copy operation failed: {response.StatusCode} ({HttpStatusDescription.Get(response.StatusCode)} | Copy {copyStatus}).");
                }
            }

            return exception;
        }
    }
}
