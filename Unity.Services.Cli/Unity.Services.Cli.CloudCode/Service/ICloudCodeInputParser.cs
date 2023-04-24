using Unity.Services.Cli.CloudCode.Input;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using Language = Unity.Services.Gateway.CloudCodeApiV1.Generated.Model.Language;

namespace Unity.Services.Cli.CloudCode.Service;

interface ICloudCodeInputParser
{
    public ICloudCodeScriptParser CloudCodeScriptParser { get; }

    public Language ParseLanguage(CloudCodeInput input);

    public ScriptType ParseScriptType(CloudCodeInput input);

    public Task<string> LoadScriptCodeAsync(CloudCodeInput input, CancellationToken cancellationToken);

    public Task<string> LoadScriptCodeAsync(string filePath, CancellationToken cancellationToken);

    public Task<Stream> LoadModuleContentsAsync(string filePath);
}
