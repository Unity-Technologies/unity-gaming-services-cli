using Unity.Services.Cli.Authoring.Service;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;

namespace Unity.Services.Cli.CloudCode.Deploy;

interface IDeploymentHandlerWithOutput : ICliDeploymentOutputHandler, ICloudCodeDeploymentHandler { }
