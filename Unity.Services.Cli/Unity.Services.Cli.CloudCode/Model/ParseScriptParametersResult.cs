using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.Model;


public class ParseScriptParametersResult
{
    public bool ScriptContainsParametersJson { get; }
    public IReadOnlyList<ScriptParameter> Parameters { get; }

    public ParseScriptParametersResult(bool scriptContainsParametersJson, IReadOnlyList<ScriptParameter> parameters)
    {
        ScriptContainsParametersJson = scriptContainsParametersJson;
        Parameters = parameters;
    }
}

