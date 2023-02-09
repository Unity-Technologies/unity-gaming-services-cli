using Unity.Services.Cli.CloudCode.Exceptions;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.Deploy.Model;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;

namespace Unity.Services.Cli.CloudCode.Deploy;

internal class CloudCodeScriptsLoader : ICloudCodeScriptsLoader
{
    public async Task<List<IScript>> LoadScriptsAsync(
        IReadOnlyCollection<string> paths,
        string serviceType,
        string extension,
        ICloudCodeInputParser cloudCodeInputParser,
        ICloudCodeService cloudCodeService,
        ICollection<DeployContent> deployContents,
        CancellationToken cancellationToken)
    {
        var scriptList = new List<IScript>();
        foreach (var path in paths)
        {
            try
            {
                var script = await LoadCloudCodeScriptFromFilePathAsync(
                    path, cloudCodeInputParser, cloudCodeService, cancellationToken);
                scriptList.Add(script);
                deployContents.Add(new DeployContent(
                    ScriptName.FromPath(path).ToString(), serviceType, path, 0, "Loaded"));
            }
            catch (ScriptEvaluationException ex)
            {
                deployContents.Add(new DeployContent(
                    ScriptName.FromPath(path).ToString(),
                    serviceType, path, 0, "Failed To Read", ex.Message));
            }
        }

        return scriptList;
    }

    internal static async Task<CloudCodeScript> LoadCloudCodeScriptFromFilePathAsync(
        string filePath,
        ICloudCodeInputParser cloudCodeInputParser,
        ICloudCodeService cloudCodeService,
        CancellationToken cancellationToken)
    {
        var code = await cloudCodeInputParser.LoadScriptCodeAsync(filePath, cancellationToken);
        var scriptParameters = await cloudCodeService.GetScriptParameters(code, cancellationToken);
        var cloudCodeParameters = new List<CloudCodeParameter>();
        cloudCodeParameters.AddRange(scriptParameters.Select(p => p.ToCloudCodeParameter()));
        return new CloudCodeScript(
            ScriptName.FromPath(filePath), Language.JS, filePath, code, cloudCodeParameters, "");
    }
}
