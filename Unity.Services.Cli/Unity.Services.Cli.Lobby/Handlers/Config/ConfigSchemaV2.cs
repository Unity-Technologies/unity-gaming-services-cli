using System.Reflection;
using Unity.Services.Cli.Authoring.Templates;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.Lobby.Handlers.Config;

public class ConfigSchemaV2 : IFileTemplate
{
    const string k_EmbeddedConfigSchema = "Unity.Services.Cli.Lobby.Handlers.Config.lobby-config-schema-v2.json";

    public string Extension => ".json";

    public string FileBodyText => ResourceFileHelper
        .ReadResourceFileAsync(Assembly.GetExecutingAssembly(), k_EmbeddedConfigSchema).Result;
}
