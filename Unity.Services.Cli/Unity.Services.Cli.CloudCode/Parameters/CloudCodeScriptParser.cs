using System.IO.Abstractions;
using System.Reflection;
using Unity.Services.Cli.CloudCode.Exceptions;
using Unity.Services.Cli.CloudCode.Model;
using Unity.Services.Cli.Common.Process;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using Unity.Services.Cli.Common.Utils;
using Unity.Services.Cli.Common.Telemetry;

namespace Unity.Services.Cli.CloudCode.Parameters;

class CloudCodeScriptParser : ICloudCodeScriptParser
{
    const int k_ParsingTimeLimitInMillisecond = 5000;
    const int k_MemorySizeLimitInMB = 256;
    const string k_ParameterScriptFileName = "script_parameters";
    static readonly string k_CliVersion = TelemetryConfigurationProvider.GetCliVersion();
    const string k_ParameterScriptExtension = ".js";
    const string k_EmbeddedParameterScript = $"Unity.Services.Cli.CloudCode.JavaScripts.{k_ParameterScriptFileName + k_ParameterScriptExtension}";
    static readonly Version k_MinimumVersion = new(14, 0, 0);

    internal static readonly string CloudCodePath = Path
        .Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
        "UnityServices", "CloudCode");

    static readonly string k_ParameterScriptFile = Path
        .Combine(CloudCodePath, k_ParameterScriptFileName + "_" + k_CliVersion + k_ParameterScriptExtension);

    readonly ICliProcess m_CliProcess;
    readonly ICloudScriptParametersParser m_CloudScriptParametersParser;
    readonly IFile m_File;

    public CloudCodeScriptParser(ICliProcess cliProcess, ICloudScriptParametersParser parametersParser, IFile file)
    {
        m_CliProcess = cliProcess;
        m_CloudScriptParametersParser = parametersParser;
        m_File = file;
    }

    async Task CreateScriptParameterParserAsync(CancellationToken token)
    {
        if (!m_File.Exists(k_ParameterScriptFile))
        {
            var parameterParseScript = await ResourceFileHelper
                .ReadResourceFileAsync(Assembly.GetExecutingAssembly(), k_EmbeddedParameterScript);
            await m_File.WriteAllTextAsync(k_ParameterScriptFile, parameterParseScript, token);
        }
    }

    public async Task<ParseScriptParametersResult> ParseScriptParametersAsync(string scriptCode, CancellationToken cancellationToken)
    {
        var parameterInJson = await ParseToScriptParamsJsonAsync(scriptCode, cancellationToken);
        var parameters = new List<ScriptParameter>();

        if (parameterInJson is not null)
        {
            parameters = m_CloudScriptParametersParser.ParseToScriptParameters(parameterInJson);
        }

        return new ParseScriptParametersResult(!string.IsNullOrEmpty(parameterInJson), parameters);
    }

    internal async Task<string?> ParseToScriptParamsJsonAsync(string scriptCode, CancellationToken token)
    {
        Directory.CreateDirectory(CloudCodePath);

        if (!await CheckNodeInstalledAsync(token))
        {
            var requirementMessage =
                $"CLI Cloud Code service require Node.js with version > {k_MinimumVersion}. "
                + $"Please install Node.js and config it's executable path in PATH environment variable. ";
            throw new ScriptEvaluationException(requirementMessage);
        }

        await CreateScriptParameterParserAsync(token);

        try
        {
            var parseTimeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
            parseTimeoutTokenSource.CancelAfter(k_ParsingTimeLimitInMillisecond);
            var paramString = await m_CliProcess.ExecuteAsync("node", Directory.GetCurrentDirectory(), new[]
            {
                $"--max-old-space-size={k_MemorySizeLimitInMB}",
                k_ParameterScriptFile
            }, parseTimeoutTokenSource.Token, writeToStandardInput: WriteToProcessStandardInput);

            paramString = paramString.Replace(System.Environment.NewLine, "");
            return string.IsNullOrEmpty(paramString) ? null : paramString;
        }
        catch (OperationCanceledException)
        {
            throw new ScriptEvaluationException(
                $"The in-script parameter parsing is taking too long:{System.Environment.NewLine}{scriptCode}.");
        }
        catch (ProcessException ex)
        {
            throw new ScriptEvaluationException(ex);
        }

        void WriteToProcessStandardInput(StreamWriter writer)
        {
            const int chunkSize = 1000;
            for (var index = 0; index < scriptCode.Length; index += chunkSize)
            {
                var subString = scriptCode.Substring(index, Math.Min(chunkSize, scriptCode.Length - index));
                writer.Write(subString);
            }
        }
    }

    internal async Task<bool> CheckNodeInstalledAsync(CancellationToken token)
    {
        try
        {
            var nodeVersionString = await m_CliProcess.ExecuteAsync("node", Directory.GetCurrentDirectory(), new[]
            {
                "-v"
            }, token);
            if (Version.TryParse(nodeVersionString.Replace("v", ""), out var version) && version.CompareTo(k_MinimumVersion) >= 0)
            {
                return true;
            }
        }
        catch (ProcessException)
        {
            return false;
        }

        return false;
    }
}
