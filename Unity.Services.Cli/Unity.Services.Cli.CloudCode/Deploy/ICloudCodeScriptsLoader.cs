using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.Cli.CloudCode.Parameters;

namespace Unity.Services.Cli.CloudCode.Deploy;

interface ICloudCodeScriptsLoader
{
    Task<CloudCodeScriptLoadResult> LoadScriptsAsync(
        IReadOnlyCollection<string> paths,
        string serviceType,
        string extension,
        ICloudCodeInputParser cloudCodeInputParser,
        ICloudCodeScriptParser cloudCodeScriptParser,
        CancellationToken cancellationToken);
}
