using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;

namespace Unity.Services.Cli.CloudCode.Deploy;

interface ICliCloudCodeClient : ICloudCodeClient
{
    void Initialize(string environmentId, string projectId, CancellationToken cancellationToken);
}
