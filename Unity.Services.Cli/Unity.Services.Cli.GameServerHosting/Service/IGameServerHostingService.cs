using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Api;

namespace Unity.Services.Cli.GameServerHosting.Service;

public interface IGameServerHostingService
{
    public IBuildsApi BuildsApi { get; }
    public IBuildConfigurationsApi BuildConfigurationsApi { get; }
    public IFilesApi FilesApi { get; }
    public IFleetsApi FleetsApi { get; }
    public IMachinesApi MachinesApi { get; }
    public IServersApi ServersApi { get; }

    public Task AuthorizeGameServerHostingService(CancellationToken cancellationToken = default);
}
