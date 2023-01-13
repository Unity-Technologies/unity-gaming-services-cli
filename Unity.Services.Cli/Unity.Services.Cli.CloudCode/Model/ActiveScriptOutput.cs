using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.Model;

public class ActiveScriptOutput
{
    public int Version { get; }
    public DateTime DatePublished { get; }
    public List<ScriptParameter> Params { get; }
    public string Code { get; }

    public ActiveScriptOutput()
    {
        Code = String.Empty;
        Params = new List<ScriptParameter>();
    }
    public ActiveScriptOutput(GetScriptResponseActiveScript activeScript)
    {
        Version = activeScript._Version;
        DatePublished = activeScript.DatePublished;
        Params = activeScript.Params;
        Code = activeScript.Code;
    }
}
