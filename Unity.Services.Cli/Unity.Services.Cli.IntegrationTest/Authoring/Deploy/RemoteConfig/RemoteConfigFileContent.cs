using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.RemoteConfig.Deploy;
using Unity.Services.Cli.RemoteConfig.Model;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.IntegrationTest.Authoring.Deploy.RemoteConfig;

public class RemoteConfigFileContent
{
    public Dictionary<string, string> entries;

    public RemoteConfigFileContent()
    {
        entries = new Dictionary<string, string>();
    }

    public static List<DeployContent> RemoteConfigToDeployContents(
        AuthoringTestCase testCase,
        DeploymentStatus? status = null,
        float? progress = null)
    {
        var config = testCase.ConfigValue;
        return RemoteConfigContentToDeployContents(
            config,
            testCase.ConfigFilePath,
            progress ?? testCase.DeployedContent.Progress,
            status ?? testCase.DeployedContent.Status);
    }

    public static List<DeployContent> RemoteConfigContentToDeployContents(
        string config,
        string path,
        float progress,
        DeploymentStatus status)
    {
        var deployContents = new List<DeployContent>();
        var json = JsonConvert.DeserializeObject<RemoteConfigFileContent>(config);

        foreach (var kvp in json!.entries)
        {
            deployContents.Add(new CliRemoteConfigEntry(
                kvp.Key,
                "RemoteConfig Entry",
                path,
                progress,
                status.Message,
                status.MessageDetail,
                status.MessageSeverity));
        }

        return deployContents;
    }
}
