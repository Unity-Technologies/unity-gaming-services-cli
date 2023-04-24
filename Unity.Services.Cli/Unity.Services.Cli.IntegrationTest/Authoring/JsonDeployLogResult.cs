using System;
using System.Collections.Generic;
using Unity.Services.Cli.Authoring.Model;

namespace Unity.Services.Cli.IntegrationTest.Authoring;

[Serializable]
class JsonDeployLogResult
{
    public readonly DeploymentResult Result;
    public readonly List<string> Messages;

    public JsonDeployLogResult(DeploymentResult result)
    {
        Result = result;
        Messages = new List<string>();
    }
}
