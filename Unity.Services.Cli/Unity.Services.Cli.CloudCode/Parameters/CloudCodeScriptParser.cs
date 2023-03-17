using System.Reflection;
using Jint;
using Jint.Native;
using Jint.Runtime;
using Unity.Services.Cli.CloudCode.Exceptions;

namespace Unity.Services.Cli.CloudCode.Parameters;

internal class CloudCodeScriptParser : ICloudCodeScriptParser
{
    const string k_EmbeddedParameterScript = "Unity.Services.Cli.CloudCode.JavaScripts.script_parameters.js";
    const string k_TimeoutExceptionMessage = "Script took too much time to parse (timed out).";
    const int k_TimeoutInSeconds = 5;
    const int k_MemoryLimitInByte = 256000000;

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
            options.LimitMemory(k_MemoryLimitInByte);
            options.TimeoutInterval(TimeSpan.FromSeconds(k_TimeoutInSeconds));
            options.CancellationToken(token);
        });

        var paramParser = engine.Execute(parameterParseScript).GetValue("scriptParameters");
        try
        {
            return engine.Invoke(paramParser, cloudCodeScript);
        }
        catch (TimeoutException)
        {
            throw new ScriptEvaluationException(k_TimeoutExceptionMessage);
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
