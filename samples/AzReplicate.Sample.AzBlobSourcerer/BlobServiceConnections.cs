using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace AzReplicate.Sample.AzBlobSourcerer
{
    public class BlobServiceConnections
    {
        private const string SourceStorageConnectionKey = "SourceStorageConnection";
        private const string DestinationStorageConnectionKey = "DestinationStorageConnection";
        private readonly Dictionary<string, string> _connectionStrings;
        private readonly Dictionary<string, BlobServiceClient> _blobServiceClients;
        private readonly Dictionary<string, SasQueryParameters> _sasQueryParameters;

        /// <summary>
        /// Get the source BlobServiceClient
        /// </summary>
        public BlobServiceClient SourceClient 
        {
            get
            {
                return GetBlobServiceClient(SourceStorageConnectionKey);
            }
        }

        /// <summary>
        /// Get the source SasQueryParameters
        /// </summary>
        public SasQueryParameters SourceSas
        {
            get
            {
                return GetSasQueryParameters(SourceStorageConnectionKey);
            }
        }

        /// <summary>
        /// Get the Destination BlobServiceClients
        /// </summary>
        public Dictionary<string, BlobServiceClient> DestinationClients
        {
            get
            {
                return _connectionStrings.Where(x => x.Key.StartsWith(DestinationStorageConnectionKey))
                    .ToDictionary(x => x.Key, y => GetBlobServiceClient(y.Key));
            }
        }

        /// <summary>
        /// Get the Destination SasQueryParameters
        /// </summary>
        public Dictionary<string, SasQueryParameters> DestinationSas
        {
            get
            {
                return _connectionStrings.Where(x => x.Key.StartsWith(DestinationStorageConnectionKey))
                    .ToDictionary(x => x.Key, y => GetSasQueryParameters(y.Key));
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionStrings">The Connection Strings section from config</param>
        public BlobServiceConnections(IEnumerable<IConfigurationSection> connectionStrings)
        {
            _connectionStrings = connectionStrings.ToDictionary(x => x.Key, y => y.Value);
            _blobServiceClients = new Dictionary<string, BlobServiceClient>();
            _sasQueryParameters = new Dictionary<string, SasQueryParameters>();
        }

        /// <summary>
        /// Lazy load a BlobServiceClient and cache it internally
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private BlobServiceClient GetBlobServiceClient(string key)
        { 
            if (!_blobServiceClients.ContainsKey(key))
                _blobServiceClients.Add(key, new BlobServiceClient(_connectionStrings[key]));

            return _blobServiceClients[key];
        }

        /// <summary>
        /// Lazy Load a SasQueryParameters and cache it internally
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private SasQueryParameters GetSasQueryParameters(string key)
        {
            if (!_sasQueryParameters.ContainsKey(key))
            {
                var csBuilder = new DbConnectionStringBuilder();
                csBuilder.ConnectionString = _connectionStrings[key];

                string accountName = csBuilder["AccountName"].ToString();
                string accountKey = csBuilder["AccountKey"].ToString();
                var keyCredential = new StorageSharedKeyCredential(accountName, accountKey);

                var sasBuilder = new AccountSasBuilder()
                {
                    StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                    ExpiresOn = DateTimeOffset.UtcNow.AddMonths(1),
                    Services = AccountSasServices.Blobs,
                    ResourceTypes = AccountSasResourceTypes.Object | AccountSasResourceTypes.Container
                };

                if (key.StartsWith(SourceStorageConnectionKey))
                    sasBuilder.SetPermissions(AccountSasPermissions.Read);
                else if (key.StartsWith(DestinationStorageConnectionKey))
                    sasBuilder.SetPermissions(AccountSasPermissions.Add | AccountSasPermissions.Create | AccountSasPermissions.Write | AccountSasPermissions.Update);

                _sasQueryParameters.Add(key, sasBuilder.ToSasQueryParameters(keyCredential));
            }

            return _sasQueryParameters[key];
        }
    }

}
