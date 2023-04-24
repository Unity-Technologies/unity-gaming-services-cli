using System.Runtime.Serialization;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Process;

namespace Unity.Services.Cli.CloudCode.Exceptions;

[Serializable]
public class ScriptEvaluationException : CliException
{
    protected ScriptEvaluationException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    public ScriptEvaluationException(ProcessException exception) : base($"{exception.Message}", Common.Exceptions.ExitCode.HandledError) { }

    public ScriptEvaluationException(ArgumentOutOfRangeException exception) : base($"{exception.Message}", Common.Exceptions.ExitCode.HandledError) { }

    public ScriptEvaluationException(string message) : base($"{message}", Common.Exceptions.ExitCode.HandledError) { }
}
