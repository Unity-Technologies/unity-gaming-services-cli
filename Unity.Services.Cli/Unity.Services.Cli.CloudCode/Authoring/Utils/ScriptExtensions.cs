using System.Text;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;

namespace Unity.Services.Cli.CloudCode;

static class ScriptExtensions
{
    public static void InjectJavaScriptParametersToBody(this IScript script, StringBuilder builder)
    {
        var javaScriptParams = script.Parameters.ToJavaScript();
        builder.Clear()
            .AppendLine(script.Body)
            .AppendLine($"module.exports.params = {javaScriptParams};");
        script.Body = builder.ToString();
    }
}
