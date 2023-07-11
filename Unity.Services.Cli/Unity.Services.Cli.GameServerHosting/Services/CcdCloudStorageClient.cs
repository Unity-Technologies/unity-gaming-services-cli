using System.Net.Http.Headers;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Api;
using Unity.Services.Gateway.ContentDeliveryManagementApiV1.Generated.Model;
using Unity.Services.Multiplay.Authoring.Core.Builds;
using Unity.Services.Multiplay.Authoring.Core.CloudContent;

namespace Unity.Services.Cli.GameServerHosting.Services
{
    class CcdCloudStorageClient : ICloudStorage
    {
        const int k_BatchSize = 10;

        readonly IBucketsApiAsync m_BucketsApiClient;
        readonly IEntriesApiAsync m_EntriesApiClient;
        readonly HttpClient m_UploadClient;
        readonly GameServerHostingApiConfig m_ApiConfig;

        public CcdCloudStorageClient(
            IBucketsApiAsync bucketsApiClient,
            IEntriesApiAsync entriesApiClient,
            HttpClient uploadClient,
            GameServerHostingApiConfig apiConfig)
        {
            m_BucketsApiClient = bucketsApiClient;
            m_EntriesApiClient = entriesApiClient;
            m_UploadClient = uploadClient;
            m_ApiConfig = apiConfig;
        }

        public async Task<CloudBucketId> FindBucket(string name, CancellationToken cancellationToken = default)
        {
            var buckets = await m_BucketsApiClient.ListBucketsByProjectEnvAsync(m_ApiConfig.ProjectId.ToString(), m_ApiConfig.EnvironmentId.ToString(), name: name, cancellationToken: cancellationToken);
            return buckets.Select(b => new CloudBucketId { Id = b.Id }).FirstOrDefault();
        }

        public async Task<CloudBucketId> CreateBucket(string name, CancellationToken cancellationToken = default)
        {
            var res = await m_BucketsApiClient.CreateBucketByProjectEnvAsync(m_ApiConfig.ProjectId.ToString(), m_ApiConfig.EnvironmentId.ToString(), new CcdBucketCreate(name: name, projectguid: m_ApiConfig.ProjectId), cancellationToken: cancellationToken);
            return new CloudBucketId { Id = res.Id };
        }

        public async Task<int> UploadBuildEntries(CloudBucketId bucket, IList<BuildEntry> localEntries, CancellationToken cancellationToken = default)
        {
            var changes = 0;
            var remoteEntries = await ListAllRemoteEntries(bucket, cancellationToken);
            var orphans = remoteEntries.Keys.ToHashSet();

            await localEntries.BatchAsync(k_BatchSize, async local =>
            {
                var (path, content) = local;
                var normalizedPath = Normalize(path);
                var hash = content.CcdHash();
                if (remoteEntries.TryGetValue(normalizedPath, out var remoteEntry))
                {
                    orphans.Remove(normalizedPath);
                    if (remoteEntry.ContentHash.ToLowerInvariant() == hash)
                    {
                        return;
                    }
                }

                var entry = await CreateOrUpdateEntry(bucket, normalizedPath, hash, (int)content.Length, cancellationToken);
                changes++;
                await UploadSignedContent(entry, content, cancellationToken);
            });

            await orphans.BatchAsync(k_BatchSize, async orphan =>
            {
                var entry = remoteEntries[orphan];
                changes++;
                await DeleteEntry(bucket, entry, cancellationToken);
            });

            return changes;
        }

        async Task DeleteEntry(CloudBucketId bucket, CcdEntry entry, CancellationToken cancellationToken = default)
        {
            await m_EntriesApiClient.DeleteEntryEnvAsync(m_ApiConfig.EnvironmentId.ToString(), bucket.ToString(), entry.Entryid.ToString(), cancellationToken: cancellationToken);
        }

        async Task<CcdEntry> CreateOrUpdateEntry(CloudBucketId bucket, string path, string hash, int length, CancellationToken cancellationToken = default)
        {
            var create = new CcdEntryCreateByPath(hash, length, signedUrl: true);
            var res = await m_EntriesApiClient.CreateOrUpdateEntryByPathEnvAsync(m_ApiConfig.EnvironmentId.ToString(), bucket.ToString(), path, create, updateIfExists: true, cancellationToken: cancellationToken);
            return res;
        }

        async Task UploadSignedContent(CcdEntry entry, Stream content, CancellationToken cancellationToken = default)
        {
            // Signed uploads need to be done using HTTP Client
            // Unity generated client does not support sending application/offset+octet-stream
            // Timeout and retry is hard to handle here
            var streamContent = new StreamContent(content);
            streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/offset+octet-stream");
            var res = await m_UploadClient.PutAsync(entry.SignedUrl, streamContent, cancellationToken: cancellationToken);
            res.EnsureSuccessStatusCode();
        }

        async Task<IDictionary<string, CcdEntry>> ListAllRemoteEntries(CloudBucketId bucket, CancellationToken cancellationToken = default)
        {
            const int entriesPerPage = 100;
            var entries = new Dictionary<string, CcdEntry>();

            List<CcdEntry> res;
            var page = 1;
            do
            {
                res = await m_EntriesApiClient.GetEntriesEnvAsync(m_ApiConfig.EnvironmentId.ToString(), bucket.ToString(), page: page, perPage: entriesPerPage, cancellationToken: cancellationToken);

                foreach (var entry in res)
                {
                    entries.Add(entry.Path, entry);
                }
                page++;
            }
            while (res.Count == entriesPerPage);

            return entries;
        }

        static string Normalize(string path)
        {
            return path.TrimStart('/');
        }
    }
}
