using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;

namespace Unity.Services.Cli.Authoring.Service;

public interface IDeploymentService
{
    string ServiceType { get; }
    public string DeployFileExtension { get; }

    Task<DeploymentResult> Deploy(
        DeployInput deployInput,
        IReadOnlyList<string> filePaths,
        string projectId,
        string environmentId,
        StatusContext? loadingContext,
        CancellationToken cancellationToken);
}
