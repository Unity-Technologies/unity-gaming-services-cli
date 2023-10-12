using Unity.Services.Cli.CloudCode.IO;
using Unity.Services.CloudCode.Authoring.Editor.Core.Solution;

namespace Unity.Services.Cli.CloudCode.Solution;

class FileContentRetriever : IFileContentRetriever
{
    internal const string AssemblyString = "Unity.Services.CloudCode.Authoring.Editor.Core";

    IAssemblyLoader m_AssemblyLoader;
    public FileContentRetriever(IAssemblyLoader assemblyLoader)
    {
        m_AssemblyLoader = assemblyLoader;
    }

    public Task<string> GetFileContent(string path, CancellationToken token = default)
    {
        var assembly = m_AssemblyLoader.Load(AssemblyString);
        var stream = assembly.GetManifestResourceStream(path);

        if (stream == null)
        {
            throw new FileLoadException($"Could not load file at path '{path}' from assembly '{assembly.FullName}'");
        }

        var streamReader = new StreamReader(stream);
        return streamReader.ReadToEndAsync();
    }
}
