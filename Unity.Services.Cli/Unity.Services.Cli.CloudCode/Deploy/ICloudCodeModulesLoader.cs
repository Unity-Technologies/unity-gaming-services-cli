using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.Deploy;

interface ICloudCodeModulesLoader
{
    Task<List<IScript>> LoadPrecompiledModulesAsync(
        IReadOnlyList<string> paths,
        string serviceType);
}
