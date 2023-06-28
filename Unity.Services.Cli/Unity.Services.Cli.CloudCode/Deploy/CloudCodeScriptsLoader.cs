using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.CloudCode.Exceptions;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.CloudCode.Deploy;

class CloudCodeScriptsLoader : ICloudCodeScriptsLoader
{
    public async Task<CloudCodeScriptLoadResult> LoadScriptsAsync(
        IReadOnlyCollection<string> paths,
        string serviceType,
        string extension,
        ICloudCodeInputParser cloudCodeInputParser,
        ICloudCodeScriptParser cloudCodeScriptParser,
        CancellationToken cancellationToken)
    {
        var succeedScripts = new List<IScript>();
        var filedScripts = new List<IScript>();

        foreach (var path in paths)
        {
            var script = new CloudCodeScript(
                ScriptName.FromPath(path).ToString(),
                path,
                0,
                new DeploymentStatus(Statuses.Loading));

            try
            {
                await LoadCloudCodeScriptAsync(
                        script,
                        cloudCodeInputParser,
                        cloudCodeScriptParser,
                        cancellationToken);
                script.Status = new DeploymentStatus(Statuses.Loaded, string.Empty);

                succeedScripts.Add(script);
            }
            catch (ScriptEvaluationException ex)
            {
                script.Status = new DeploymentStatus(Statuses.FailedToRead, ex.Message, SeverityLevel.Error);

                filedScripts.Add(script);
            }
        }

        return new CloudCodeScriptLoadResult(succeedScripts, filedScripts);
    }

    static async Task LoadCloudCodeScriptAsync(
        CloudCodeScript script,
        ICloudCodeInputParser cloudCodeInputParser,
        ICloudCodeScriptParser cloudCodeScriptParser,
        CancellationToken cancellationToken)
    {
        var code = await cloudCodeInputParser.LoadScriptCodeAsync(script.Path, cancellationToken);
        var parametersParsingResult = await cloudCodeScriptParser.ParseScriptParametersAsync(code, cancellationToken);

        script.Body = code;
        script.Parameters.AddRange(parametersParsingResult.Parameters.Select(p => p.ToCloudCodeParameter()));
    }
}
