using System.Reflection;
using Jint;
using Jint.Native;
using Jint.Runtime;
using Unity.Services.Cli.CloudCode.Exceptions;

namespace Unity.Services.Cli.CloudCode.Parameters;

internal class CloudCodeScriptParser : ICloudCodeScriptParser
{
    const string k_EmbeddedParameterScript = "Unity.Services.Cli.CloudCode.JavaScripts.script_parameters.js";
    const int k_TimeoutInSeconds = 1;
    const int k_MemoryLimitInByte = 1000000;
    const int k_StatementLimit = 100;
    const int k_RecursionLimit = 10;
    public async Task<string?> ParseToScriptParamsJsonAsync(string script, CancellationToken token)
    {
        var parameterParseScript = await ReadResourceFileAsync(k_EmbeddedParameterScript);
        var param = InvokeParameterParser(parameterParseScript, script, token);
        var paramString = param.ToString();
        return paramString.Equals("null") ? null : paramString;
    }

    static JsValue InvokeParameterParser(string parameterParseScript, string cloudCodeScript, CancellationToken token)
    {
        using var engine = new Engine(options =>
        {
            options.MaxStatements(k_StatementLimit);
            options.LimitMemory(k_MemoryLimitInByte);
            options.LimitRecursion(k_RecursionLimit);
            options.TimeoutInterval(TimeSpan.FromSeconds(k_TimeoutInSeconds));
            options.CancellationToken(token);
        });

        var paramParser = engine.Execute(parameterParseScript).GetValue("scriptParameters");
        try
        {
            return engine.Invoke(paramParser, cloudCodeScript);
        }
        catch (JintException ex)
        {
            throw new ScriptEvaluationException(ex);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new ScriptEvaluationException(ex);
        }
    }

    static async Task<string> ReadResourceFileAsync(string filename)
    {
        var assembly = Assembly.GetExecutingAssembly();
        await using var stream = assembly.GetManifestResourceStream(filename);
        using var reader = new StreamReader(stream!);
        return await reader.ReadToEndAsync();
    }
}
