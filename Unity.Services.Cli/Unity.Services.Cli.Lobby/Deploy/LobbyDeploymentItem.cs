using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Lobby.Handlers;
using Unity.Services.Cli.Lobby.Handlers.Config;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Lobby.Deploy;

class LobbyDeploymentItem : DeployContent
{
    public LobbyConfig? Config { get; }

    public LobbyDeploymentItem(string path, LobbyConfig? config = null, DeploymentStatus? status = null)
        : base(
            name: LobbyConstants.ConfigDisplayName,
            type: LobbyConstants.ServiceType,
            path: path,
            status: status)
    {
        Config = config;
    }
}
