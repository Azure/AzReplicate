using AzReplicate.Contracts.Messaging;
using System;
using System.Security.Cryptography;
using System.Text;

namespace AzReplicate.Core.Extensions
{
    public static class ReplicatableExtensions
    {
        public static string PartitionKey(this Replicatable replicatable)
        {
            return Hash(ExtractHostFromUrl(replicatable.Source));
        }

        public static string RowKey(this Replicatable replicatable)
        {
            return Hash($"{replicatable.Source}-{replicatable.Destination}");
        }

        private static string ExtractHostFromUrl(string url)
        {
            return new Uri(url).Host;
        }

        private static string Hash(string value)
        {
            var md5 = MD5.Create();
            var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(value));

            var hash = new StringBuilder();

            for (int i = 0; i < hashBytes.Length; i++)
            {
                hash.Append(hashBytes[i].ToString("x2"));
            }

            return hash.ToString();
        }
    }
}
