using System.Reflection;

namespace Unity.Services.Cli.Common.Utils;

public static class ResourceFileHelper
{
    /// <summary>
    /// Returns the contents of a resource file linked in the assembly
    /// </summary>
    /// <param name="executingAssembly">The assembly where the file is located</param>
    /// <param name="filename">Name of the file (ex: Unity.Services.Cli.CloudCode.JavaScripts.script_template.js)</param>
    /// <returns>Contents of a resource file</returns>
    public static async Task<string> ReadResourceFileAsync(Assembly executingAssembly, string filename)
    {
        await using var stream = executingAssembly.GetManifestResourceStream(filename);
        using var reader = new StreamReader(stream!);
        return await reader.ReadToEndAsync();
    }
}
