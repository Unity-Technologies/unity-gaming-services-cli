using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;

namespace Unity.Services.Cli.CloudCode.Deploy;

interface ICliEnvironmentProvider : IEnvironmentProvider
{
    new string Current { get; set; }
}
