using Unity.Services.Cli.Authoring.Model;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;

namespace Unity.Services.Cli.CloudCode.Deploy;

class CloudCodeModulesLoader : ICloudCodeModulesLoader
{
    public Task<List<IScript>> LoadPrecompiledModulesAsync(
        IReadOnlyList<string> paths,
        string serviceType,
        string extension,
        ICollection<DeployContent> deployContents)
    {
        var modules = new List<IScript>();

        foreach (var path in paths)
        {
            var zipNameWithExtension = Path.GetFileName(path);

            modules.Add(new CloudCodeModule(
                new ScriptName(zipNameWithExtension),
                Language.JS,
                path
            ));

            deployContents.Add(new DeployContent(
                ScriptName.FromPath(path).ToString(), serviceType, path, 0, "Loaded"));
        }

        return Task.FromResult(modules);
    }
}
