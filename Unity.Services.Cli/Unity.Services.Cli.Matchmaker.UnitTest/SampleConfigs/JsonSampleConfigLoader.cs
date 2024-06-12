using System.Reflection;
using System.Runtime.InteropServices;

namespace Unity.Services.Cli.Matchmaker.UnitTest.SampleConfigs;

static class JsonSampleConfigLoader
{
    internal static readonly string QueueConfig = GetJsonFromResources("TestQueueConfig.json");
    internal static readonly string EmptyQueueConfig = GetJsonFromResources("TestEmptyQueueConfig.json");
    internal static readonly string TemplateQueueConfig = GetJsonFromResources("TemplateQueueConfig.json");
    internal static readonly string EnvironmentConfig = GetJsonFromResources("TestEnvironmentConfig.json");

    // Windows would serialize those with \n which is fine so fix that in SampleConfig
    internal static string WindowsLineEnding(string originalJson)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return originalJson;

        string replacedJson = originalJson.Replace("\n", "\r\n");
        if (replacedJson.EndsWith('\n'))
            replacedJson = replacedJson.Remove(replacedJson.Length - 2) + "\n";

        return replacedJson;
    }

    static string GetJsonFromResources(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();

        using Stream? stream = assembly.GetManifestResourceStream($"Unity.Services.Cli.Matchmaker.UnitTest.SampleConfigs.{resourceName}");
        if (stream != null)
        {
            using StreamReader reader = new StreamReader(stream);
            return WindowsLineEnding(reader.ReadToEnd());
        }

        return string.Empty;
    }
}
