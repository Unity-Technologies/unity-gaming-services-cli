using System.Reflection;
using Unity.Services.Cli.Authoring.Templates;
using Unity.Services.Cli.Common.Utils;

namespace Unity.Services.Cli.CloudCode.Templates;

public class CloudCodeTemplate :  IFileTemplate
{
    const string k_EmbeddedTemplateScript = "Unity.Services.Cli.CloudCode.JavaScripts.script_template.js";

    public string Extension => ".js";

    public string FileBodyText => ResourceFileHelper
        .ReadResourceFileAsync(Assembly.GetExecutingAssembly(), k_EmbeddedTemplateScript).Result;
}
