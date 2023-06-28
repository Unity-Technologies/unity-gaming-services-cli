using System.Globalization;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.Model;

public class ActiveScriptOutput
{
    public int Version { get; }
    public string DatePublished { get; }
    public List<ScriptParameter> Params { get; }
    public string Code { get; }

    public ActiveScriptOutput()
    {
        Code = String.Empty;
        Params = new List<ScriptParameter>();
        DatePublished = String.Empty;
    }
    public ActiveScriptOutput(GetScriptResponseActiveScript activeScript)
    {
        Version = activeScript._Version;
        DatePublished = activeScript.DatePublished.ToString("s", CultureInfo.InvariantCulture);
        Params = activeScript.Params;
        Code = activeScript.Code;
    }
}
