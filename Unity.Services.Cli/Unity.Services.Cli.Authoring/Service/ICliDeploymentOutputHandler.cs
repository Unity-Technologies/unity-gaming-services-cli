using Unity.Services.Cli.Authoring.Model;

namespace Unity.Services.Cli.Authoring.Service;

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
