using Unity.Services.Cli.Authoring.Model;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Language = Unity.Services.CloudCode.Authoring.Editor.Core.Model.Language;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.CloudCode.Deploy;

class CloudCodeModulesLoader : ICloudCodeModulesLoader
{
    public Task<List<IScript>> LoadPrecompiledModulesAsync(
        IReadOnlyList<string> paths,
        string serviceType)
    {
        var modules = new List<IScript>();

        foreach (var path in paths)
        {
            modules.Add(
                new CloudCodeModule(
                    ScriptName.FromPath(path).ToString(),
                    path,
                    0,
                    new DeploymentStatus(Statuses.Loaded)));
        }

        return Task.FromResult(modules);
    }
}
