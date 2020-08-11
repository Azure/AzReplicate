using AzReplicate.Contracts.Storage;
using AzReplicate.Core.Exceptions;
using AzReplicate.Core.Extensions;
using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Serialization;

namespace AzReplicate.Core.Rest
{
    public class BlobRestClient : IDealWithBlobs
    {
        private readonly HttpClient _httpClient;
        private List<string> _knownContainers = new List<string>();

        public BlobRestClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task CopyBlobFromUrlAsync(string source, string destination, CancellationToken cancellationToken = default)
        {
            await PutAsync(
                destination, 
                new[] {
                    new RequestHeader("x-ms-requires-sync", "true"),
                    new RequestHeader("x-ms-copy-source", source) 
                },
                cancellationToken
            );
        }

        public async Task PutBlockFromUrlAsync(string source, string destination, string blockId, long startByte, long? endByte, CancellationToken cancellationToken = default)
        {
            await PutAsync(
                $"{destination}&comp=block&blockid={blockId}",
                new[] {
                    new RequestHeader("x-ms-copy-source", source),
                    new RequestHeader("x-ms-source-range", $"bytes={startByte}-{endByte}")
                }, 
                cancellationToken
            );
        }

        public async Task PutBlockListAsync(string destination, IEnumerable<string> blockIds, CancellationToken cancellationToken = default)
        {
            var blockList = new BlockList
            {
                Latest = blockIds.Select(b => HttpUtility.UrlDecode(b)).ToList()
            };

            var serializer = new XmlSerializer(typeof(BlockList));
            var xns = new XmlSerializerNamespaces();
            xns.Add(string.Empty, string.Empty);
            var stringWriter = new UTF8StringWriter();
            serializer.Serialize(stringWriter, blockList, xns);

            await PutAsync(
                $"{destination}&comp=blocklist", 
                stringWriter.ToString(), 
                cancellationToken);
        }

        public async Task<long?> GetContentLengthAsync(string url, CancellationToken cancellationToken = default)
        {
            var response = await HeadAsync(url, cancellationToken);
            return response.Content.Headers.ContentLength;
        }

        public async Task<bool> ExistsWithCopySuccessAsync(string url, long? sizeInBytes, CancellationToken cancellationToken = default)
        {
            var response = await TryHeadAsync(url, cancellationToken);
            
            return 
                response.IsSuccessStatusCode && 
                response.IsCopySuccess() && 
                (sizeInBytes.HasValue ? response.Content.Headers.ContentLength == sizeInBytes : true);
        }

        public async Task EnsureContainerExistsAsync(string path, CancellationToken cancellationToken = default)
        {
            var blobUri = new BlobUriBuilder(new Uri(path));

            if (!_knownContainers.Contains(blobUri.BlobContainerName))
            {
                try
                {
                    await PutAsync(
                        $"{blobUri.Scheme}://{blobUri.Host}/{blobUri.BlobContainerName}?restype=container&{blobUri.Sas}", 
                        cancellationToken);
                }
                catch { }

                _knownContainers.Add(blobUri.BlobContainerName);
            }
        }

        public string GenerateBlockId(long offset)
        {
            byte[] id = new byte[48];
            BitConverter.GetBytes(offset).CopyTo(id, 0);

            return HttpUtility.UrlEncode(Convert.ToBase64String(id));
        }

        private async Task<HttpResponseMessage> HeadAsync(string path, CancellationToken cancellationToken = default)
        {
            return await HeadAsync(path, null, cancellationToken);
        }

        private async Task<HttpResponseMessage> HeadAsync(string path, RequestHeader[] headers, CancellationToken cancellationToken = default)
        {
            return await SendAsync(path, HttpMethod.Head, headers, cancellationToken);
        }

        private async Task<HttpResponseMessage> TryHeadAsync(string path, CancellationToken cancellationToken = default)
        {
            return await TrySendAsync(path, HttpMethod.Head, null, null, cancellationToken);
        }

        private async Task<HttpResponseMessage> PutAsync(string path, CancellationToken cancellationToken = default)
        {
            return await PutAsync(path, headers: null, cancellationToken);
        }

        private async Task<HttpResponseMessage> PutAsync(string path, RequestHeader[] headers, CancellationToken cancellationToken = default)
        {
            return await SendAsync(path, HttpMethod.Put, headers, cancellationToken);
        }

        private async Task<HttpResponseMessage> PutAsync(string path, string body, RequestHeader[] headers, CancellationToken cancellationToken = default)
        {
            return await SendAsync(path, HttpMethod.Put, body, headers, cancellationToken);
        }

        private async Task<HttpResponseMessage> PutAsync(string path, string body, CancellationToken cancellationToken = default)
        {
            return await SendAsync(path, HttpMethod.Put, body, cancellationToken);
        }

        private async Task<HttpResponseMessage> SendAsync(string path, HttpMethod httpMethod, CancellationToken cancellationToken = default)
        {
            return await SendAsync(path, httpMethod, null, null, cancellationToken);
        }

        private async Task<HttpResponseMessage> SendAsync(string path, HttpMethod httpMethod, RequestHeader[] headers, CancellationToken cancellationToken = default)
        {
            return await SendAsync(path, httpMethod, null, headers, cancellationToken);
        }

        private async Task<HttpResponseMessage> SendAsync(string path, HttpMethod httpMethod, string body, CancellationToken cancellationToken = default)
        {
            return await SendAsync(path, httpMethod, body, null, cancellationToken);
        }

        private async Task<HttpResponseMessage> SendAsync(string path, HttpMethod httpMethod, string body, RequestHeader[] headers, CancellationToken cancellationToken = default)
        {
            var response = await TrySendAsync(path, httpMethod, body, headers, cancellationToken);
            response.EnsureSuccess();
            return response;
        }

        private async Task<HttpResponseMessage> TrySendAsync(string path, HttpMethod httpMethod, string body, RequestHeader[] headers, CancellationToken cancellationToken = default)
        {
            try
            {
                var httpRequestMessage = new HttpRequestMessage(httpMethod, path);

                httpRequestMessage.Headers.Add("x-ms-date", DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture));
                httpRequestMessage.Headers.Add("x-ms-version", "2019-07-07");

                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        httpRequestMessage.Headers.Add(header.Key, header.Value);
                    }
                }

                if (!string.IsNullOrEmpty(body))
                {
                    httpRequestMessage.Content = new StringContent(body);
                }

                return await _httpClient.SendAsync(httpRequestMessage, cancellationToken);
            }
            catch (Exception e)
            {
                throw new ReplicationFailedException($"Http transport failure for {httpMethod} operation", e);
            }
        }

        private class RequestHeader
        {
            public string Key { get; set; }
            public string Value { get; set; }

            public RequestHeader(string key, string value)
            {
                Key = key;
                Value = value;
            }
        }

        private class UTF8StringWriter : StringWriter
        {
            public override Encoding Encoding { get { return Encoding.UTF8; } }
        }

        [XmlRoot(ElementName = "BlockList")]
        public class BlockList
        {
            [XmlElement(ElementName = "Latest")]
            public List<string> Latest { get; set; }
        }
    }
}
