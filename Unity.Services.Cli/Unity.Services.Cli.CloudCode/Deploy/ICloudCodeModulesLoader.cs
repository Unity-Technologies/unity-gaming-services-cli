using Unity.Services.CloudCode.Authoring.Editor.Core.Model;

namespace Unity.Services.Cli.CloudCode.Deploy;

interface ICloudCodeModulesLoader
{
    Task<(List<IScript>, List<IScript>)> LoadModulesAsync(
        IReadOnlyList<string> ccmFilePaths,
        IReadOnlyList<string> solutionFilePaths,
        CancellationToken cancellationToken);
}
