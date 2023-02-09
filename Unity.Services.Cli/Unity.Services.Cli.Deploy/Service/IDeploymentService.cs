using Spectre.Console;
using Unity.Services.Cli.Deploy.Input;
using Unity.Services.Cli.Deploy.Model;

namespace Unity.Services.Cli.Deploy.Service;

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
