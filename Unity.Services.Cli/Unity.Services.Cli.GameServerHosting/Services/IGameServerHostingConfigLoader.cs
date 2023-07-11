using Unity.Services.Multiplay.Authoring.Core.Assets;

namespace Unity.Services.Cli.GameServerHosting.Services;

interface IGameServerHostingConfigLoader
{
    Task<MultiplayConfig> LoadAndValidateAsync(ICollection<string> paths, CancellationToken cancellationToken);
}
