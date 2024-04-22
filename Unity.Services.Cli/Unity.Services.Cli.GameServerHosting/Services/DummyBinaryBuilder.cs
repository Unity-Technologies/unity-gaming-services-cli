using Unity.Services.Multiplay.Authoring.Core;
using Unity.Services.Multiplay.Authoring.Core.Builds;

namespace Unity.Services.Cli.GameServerHosting.Services;

class DummyBinaryBuilder : IBinaryBuilder
{
    public ServerBuild BuildLinuxServer(string outDir, string executable)
    {
        return new ServerBuild(Path.Combine(outDir, executable));
    }

    public void WarnBuildTargetChanged() { }
}
