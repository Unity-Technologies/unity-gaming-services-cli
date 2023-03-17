using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;

namespace Unity.Services.Cli.CloudCode.Deploy;

interface ICloudCodeScriptsLoader
{
    Task<List<IScript>> LoadScriptsAsync(
        IReadOnlyCollection<string> paths,
        string serviceType,
        string extension,
        ICloudCodeInputParser cloudCodeInputParser,
        ICloudCodeService cloudCodeService,
        ICollection<DeployContent> deployContents,
        CancellationToken cancellationToken);
}
