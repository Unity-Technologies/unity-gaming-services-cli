using Unity.Services.Cli.CloudCode.Exceptions;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;

namespace Unity.Services.Cli.CloudCode.Deploy;

internal class CloudCodeScriptsLoader : ICloudCodeScriptsLoader
{
    public async Task<CloudCodeScriptLoadResult> LoadScriptsAsync(
        IReadOnlyCollection<string> paths,
        string serviceType,
        string extension,
        ICloudCodeInputParser cloudCodeInputParser,
        ICloudCodeScriptParser cloudCodeScriptParser,
        ICollection<DeployContent> deployContents,
        CancellationToken cancellationToken)
    {
        var scriptList = new List<IScript>();
        var failedContents = new List<DeployContent>();
        foreach (var path in paths)
        {
            try
            {
                var script = await LoadCloudCodeScriptFromFilePathAsync(
                    path, cloudCodeInputParser, cloudCodeScriptParser, cancellationToken);
                scriptList.Add(script);
                deployContents.Add(new DeployContent(
                    ScriptName.FromPath(path).ToString(), serviceType, path, 0, "Loaded"));
            }
            catch (ScriptEvaluationException ex)
            {
                failedContents.Add(new DeployContent(
                    ScriptName.FromPath(path).ToString(),
                    serviceType, path, 0, "Failed To Read", ex.Message));
            }
        }

        return new CloudCodeScriptLoadResult(scriptList, failedContents);
    }

    internal static async Task<CloudCodeScript> LoadCloudCodeScriptFromFilePathAsync(
        string filePath,
        ICloudCodeInputParser cloudCodeInputParser,
        ICloudCodeScriptParser cloudCodeScriptParser,
        CancellationToken cancellationToken)
    {
        var code = await cloudCodeInputParser.LoadScriptCodeAsync(filePath, cancellationToken);
        var scriptParameters = await cloudCodeScriptParser.ParseScriptParametersAsync(code, cancellationToken);
        var cloudCodeParameters = new List<CloudCodeParameter>();
        cloudCodeParameters.AddRange(scriptParameters.Select(p => p.ToCloudCodeParameter()));
        return new CloudCodeScript(
            ScriptName.FromPath(filePath), Language.JS, filePath, code, cloudCodeParameters, "");
    }
}
