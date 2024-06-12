using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Parameters;

namespace Unity.Services.Cli.CloudCode.Service;

interface ICloudCodeInputParser
{
    public ICloudCodeScriptParser CloudCodeScriptParser { get; }

    public string ParseLanguage(CloudCodeInput input);

    public string ParseScriptType(CloudCodeInput input);

    public Task<string> LoadScriptCodeAsync(CloudCodeInput input, CancellationToken cancellationToken);

    public Task<string> LoadScriptCodeAsync(string filePath, CancellationToken cancellationToken);

    public Task<Stream> LoadModuleContentsAsync(string filePath);
}
