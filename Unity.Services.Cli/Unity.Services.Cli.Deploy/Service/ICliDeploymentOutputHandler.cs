using Unity.Services.Cli.Deploy.Model;

namespace Unity.Services.Cli.Deploy.Service;

/// <summary>
/// Interface to handle deployment output
/// </summary>
public interface ICliDeploymentOutputHandler
{
    /// <summary>
    /// Collection of contents to be delopyed
    /// </summary>
    ICollection<DeployContent> Contents { get; }
}
