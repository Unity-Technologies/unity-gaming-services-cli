using Spectre.Console;
using Unity.Services.Cli.Deploy.Model;

namespace Unity.Services.Cli.Deploy.Input;

public interface IDeploymentService
{
    string ServiceType { get; }
    protected string DeployFileExtension { get; }

    Task<DeploymentResult> Deploy(
        DeployInput input,
        StatusContext? loadingContext,
        CancellationToken cancellationToken);
}
