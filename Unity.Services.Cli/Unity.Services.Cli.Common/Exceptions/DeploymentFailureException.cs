using System.Runtime.Serialization;

namespace Unity.Services.Cli.Common.Exceptions;

/// <summary>
/// Exception caused by a failure in the deployment process
/// </summary>
[Serializable]
public class DeploymentFailureException : Exception
{
    public int ExitCode { get; }

    protected DeploymentFailureException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    public DeploymentFailureException()
        : base("", null)
    {
        ExitCode = Common.Exceptions.ExitCode.HandledError;
    }
}
