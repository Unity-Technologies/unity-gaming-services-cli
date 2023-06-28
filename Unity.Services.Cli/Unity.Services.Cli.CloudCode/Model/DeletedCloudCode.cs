using Unity.Services.Cli.Authoring.Model;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.CloudCode.Model;

class DeletedCloudCode : DeployContent
{
    public DeletedCloudCode(string name, string type, string path, float progress = 0, DeploymentStatus? status = null)
        : base(name, type, path, progress, status) { }

    public override string ToString()
    {
        return $"'{Name}' - Deleted Remotely";
    }
}
