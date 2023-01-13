using System;
using System.Collections.Generic;
using Unity.Services.Cli.Deploy.Model;

namespace Unity.Services.Cli.IntegrationTest.Deploy;

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
