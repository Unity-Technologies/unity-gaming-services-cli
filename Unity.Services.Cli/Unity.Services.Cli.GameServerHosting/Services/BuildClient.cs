using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Api;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Unity.Services.Multiplay.Authoring.Core.Builds;
using Unity.Services.Multiplay.Authoring.Core.MultiplayApi;
using IBuildsApi = Unity.Services.Multiplay.Authoring.Core.MultiplayApi.IBuildsApi;

namespace Unity.Services.Cli.GameServerHosting.Services;

class BuildClient : IBuildsApi
{
    readonly IBuildsApiAsync m_BuildsApiAsync;
    readonly GameServerHostingApiConfig m_ApiConfig;

    public BuildClient(IBuildsApiAsync buildsApiAsync, GameServerHostingApiConfig apiConfig)
    {
        m_BuildsApiAsync = buildsApiAsync;
        m_ApiConfig = apiConfig;
    }

    public async Task<(BuildId, CloudBucketId)?> FindByName(string name, CancellationToken cancellationToken = default)
    {
        var res = await m_BuildsApiAsync.ListBuildsAsync(
            m_ApiConfig.ProjectId,
            m_ApiConfig.EnvironmentId,
            partialFilter: name,
            cancellationToken: cancellationToken);
        switch (res.Count(b => b.BuildName == name))
        {
            case 0:
                return null;
            case 1:
                return (
                    new BuildId { Id = res[0].BuildID },
                    new CloudBucketId { Id = res[0].Ccd.BucketID }
                );
            default:
                throw new DuplicateResourceException("Build", name);
        }
    }

    public async Task<(BuildId, CloudBucketId)> Create(string name, MultiplayConfig.BuildDefinition definition, CancellationToken cancellationToken = default)
    {
        var res = await m_BuildsApiAsync.CreateBuildAsync(m_ApiConfig.ProjectId,
            m_ApiConfig.EnvironmentId,
            new CreateBuildRequest(name, ccd: new CCDDetails1()), cancellationToken: cancellationToken);
        return (
            new BuildId { Id = res.BuildID },
            new CloudBucketId { Id = res.Ccd.BucketID }
        );
    }

    public Task CreateVersion(BuildId id, CloudBucketId bucket, CancellationToken cancellationToken = default)
    {
        return m_BuildsApiAsync.CreateNewBuildVersionAsync(m_ApiConfig.ProjectId,
            m_ApiConfig.EnvironmentId,
            id.ToLong(),
            new CreateNewBuildVersionRequest(ccd: new CCDDetails2(bucketID: bucket.ToGuid())), cancellationToken: cancellationToken);
    }

    public async Task<bool> IsSynced(BuildId id, CancellationToken cancellationToken = default)
    {
        var res = await m_BuildsApiAsync.GetBuildAsync(m_ApiConfig.ProjectId, m_ApiConfig.EnvironmentId, id.ToLong(), cancellationToken: cancellationToken);
        if (res.SyncStatus == CreateBuild200Response.SyncStatusEnum.FAILED)
        {
            throw new SyncFailedException();
        }
        return res.SyncStatus == CreateBuild200Response.SyncStatusEnum.SYNCED;
    }
}
