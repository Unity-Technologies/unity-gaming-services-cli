using Unity.Services.Cli.GameServerHosting.Exceptions;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Api;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Unity.Services.Multiplay.Authoring.Core.MultiplayApi;

namespace Unity.Services.Cli.GameServerHosting.Services;

class BuildConfigsClient : IBuildConfigApi
{
    readonly IBuildConfigurationsApiAsync m_BuildConfigurationsApiAsync;
    readonly GameServerHostingApiConfig m_ApiConfig;

    public BuildConfigsClient(IBuildConfigurationsApiAsync buildConfigurationsApiAsync, GameServerHostingApiConfig apiConfig)
    {
        m_BuildConfigurationsApiAsync = buildConfigurationsApiAsync;
        m_ApiConfig = apiConfig;
    }

    public async Task<BuildConfigurationId?> FindByName(string name, CancellationToken cancellationToken = default)
    {
        var res = await m_BuildConfigurationsApiAsync
            .ListBuildConfigurationsAsync(m_ApiConfig.ProjectId, m_ApiConfig.EnvironmentId, partialFilter: name, cancellationToken: cancellationToken);
        switch (res.Count(b => b.Name == name))
        {
            case 0:
                return null;
            case 1:
                return new BuildConfigurationId { Id = res[0].Id };
            default:
                throw new DuplicateResourceException("BuildConfiguration", name);
        }
    }

    public async Task<BuildConfigurationId> Create(string name, BuildId buildId, MultiplayConfig.BuildConfigurationDefinition definition, CancellationToken cancellationToken = default)
    {
        var request = new BuildConfigurationCreateRequest(
            name: name,
            buildID: buildId.ToLong(),
            queryType: definition.QueryType.ToString()!,
            binaryPath: definition.BinaryPath,
            commandLine: definition.CommandLine,
            cores: definition.Cores,
            speed: definition.SpeedMhz,
            memory: definition.MemoryMiB,
            configuration: definition.Variables.Select(kv => new ConfigurationPair(kv.Key, kv.Value)).ToList()
        );

        var res = await m_BuildConfigurationsApiAsync
            .CreateBuildConfigurationAsync(
                m_ApiConfig.ProjectId,
                m_ApiConfig.EnvironmentId,
                request,
                cancellationToken: cancellationToken
                );
        return new BuildConfigurationId { Id = res.Id };
    }

    public Task Update(BuildConfigurationId id, string name, BuildId buildId, MultiplayConfig.BuildConfigurationDefinition definition, CancellationToken cancellationToken = default)
    {
        return m_BuildConfigurationsApiAsync.UpdateBuildConfigurationAsync(
            m_ApiConfig.ProjectId,
            m_ApiConfig.EnvironmentId,
            id.ToLong(),
            new BuildConfigurationUpdateRequest(
                name: name,
                buildID: buildId.ToLong(),
                configuration: definition.Variables.Select(kv => new ConfigurationPair1(0, kv.Key, kv.Value)).ToList(),
                queryType: definition.QueryType.ToString()!,
                binaryPath: definition.BinaryPath,
                commandLine: definition.CommandLine,
                cores: definition.Cores,
                speed: definition.SpeedMhz,
                memory: definition.MemoryMiB
            ),
            cancellationToken: cancellationToken
            );
    }
}
