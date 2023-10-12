using System.Reflection;

namespace Unity.Services.Cli.CloudCode.IO;

interface IAssemblyLoader
{
    Assembly Load(string assemblyString);
}
