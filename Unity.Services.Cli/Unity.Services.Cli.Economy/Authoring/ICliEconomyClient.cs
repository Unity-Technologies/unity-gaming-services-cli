using Unity.Services.Economy.Editor.Authoring.Core.Service;

namespace Unity.Services.Cli.Economy.Authoring;

interface ICliEconomyClient : IEconomyClient
{
    void Initialize(string environmentId, string projectId, CancellationToken cancellationToken);
}
