using System.Collections.Generic;
using Unity.Services.Cli.Deploy.Model;
using Unity.Services.Cli.Deploy.Service;
using Unity.Services.CloudCode.Authoring.Editor.Core.Analytics;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;
using Unity.Services.CloudCode.Authoring.Editor.Core.Logging;

namespace Unity.Services.Cli.CloudCode.UnitTest.Mock;

internal class MockCloudCodeDeploymentHandler : CloudCodeDeploymentHandler, ICliDeploymentOutputHandler
{
    public MockCloudCodeDeploymentHandler(
        ICloudCodeClient client,
        IDeploymentAnalytics deploymentAnalytics,
        IScriptCache scriptCache,
        ILogger logger,
        IPreDeployValidator preDeployValidator, ICollection<DeployContent> contents)
        : base(client, deploymentAnalytics, scriptCache, logger, preDeployValidator)
    {
        Contents = contents;
    }

    public ICollection<DeployContent> Contents { get; }
}
