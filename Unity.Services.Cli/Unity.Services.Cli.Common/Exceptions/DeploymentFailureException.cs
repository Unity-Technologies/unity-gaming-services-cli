using System.Runtime.Serialization;

namespace Unity.Services.Cli.Common.Exceptions;

/// <summary>
/// Exception caused by a failure in the deployment process
/// </summary>
public class DeploymentFailureException : Exception
{
    public int ExitCode { get; }

    public DeploymentFailureException()
        : base("", null)
    {
        ExitCode = Common.Exceptions.ExitCode.HandledError;
    }
}
