using System.Collections.Generic;
using Newtonsoft.Json;
using Unity.Services.Cli.Authoring.Model;

namespace Unity.Services.Cli.IntegrationTest.Authoring.Deploy.RemoteConfig;

public class RemoteConfigFileContent
{
    public Dictionary<string, string> entries;

    public RemoteConfigFileContent()
    {
        entries = new Dictionary<string, string>();
    }

    public static List<DeployContent> RemoteConfigToDeployContents(AuthoringTestCase testCase)
    {
        var deployContents = new List<DeployContent>();
        var json = JsonConvert.DeserializeObject<RemoteConfigFileContent>(testCase.ConfigValue);

        foreach (var kvp in json!.entries)
        {
            deployContents.Add(new DeployContent(kvp.Key,
                "Remote Config",
                testCase.ConfigFilePath,
                testCase.DeployedContent.Progress,
                testCase.DeployedContent.Status,
                testCase.DeployedContent.Detail));
        }

        return deployContents;
    }
}
