using System.Reflection;

namespace Unity.Services.Cli.CloudCode.IO;

class AssemblyLoader : IAssemblyLoader
{
    public Assembly Load(string assemblyString)
    {
        return Assembly.Load(assemblyString);
    }
}
