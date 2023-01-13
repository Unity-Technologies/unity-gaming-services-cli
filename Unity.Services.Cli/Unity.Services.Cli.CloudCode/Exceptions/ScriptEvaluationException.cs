using System.Runtime.Serialization;
using Jint.Runtime;
using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.CloudCode.Exceptions;

[Serializable]
public class ScriptEvaluationException : CliException
{
    protected ScriptEvaluationException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    public ScriptEvaluationException(JintException exception) : base($"Invalid script: {exception.Message}", Common.Exceptions.ExitCode.HandledError) { }

    public ScriptEvaluationException(ArgumentOutOfRangeException exception) : base($"Invalid script: {exception.Message}", Common.Exceptions.ExitCode.HandledError) { }

    public ScriptEvaluationException(string message) : base($"Invalid script: {message}", Common.Exceptions.ExitCode.HandledError) { }
}
