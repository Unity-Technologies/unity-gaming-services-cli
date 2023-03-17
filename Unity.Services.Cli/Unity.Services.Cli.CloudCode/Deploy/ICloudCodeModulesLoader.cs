using Unity.Services.Cli.Authoring.Model;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;

namespace Unity.Services.Cli.CloudCode.Deploy;

interface ICloudCodeModulesLoader
{
    Task<List<IScript>> LoadPrecompiledModulesAsync(
        IReadOnlyList<string> paths,
        string serviceType,
        string extension,
        ICollection<DeployContent> deployContents);
}
