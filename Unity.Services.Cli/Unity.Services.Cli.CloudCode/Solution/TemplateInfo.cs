using System.Collections.Generic;
using System.IO;
using Unity.Services.Cli.CloudCode.IO;
using Unity.Services.CloudCode.Authoring.Editor.Core.Solution;

namespace Unity.Services.Cli.CloudCode.Solution;

class TemplateInfo : ITemplateInfo
{
    internal const string AssemblyString = "Unity.Services.CloudCode.Authoring.Editor.Core";

    static readonly Dictionary<string, string> k_ResourceToRelativePath = new()
    {
        [$"{AssemblyString}.Solution.sln"] = "Solution.sln",
        [$"{AssemblyString}.Project.csproj"] = Path.Combine("Project", "Project.csproj"),
        [$"{AssemblyString}.Example.cs"] = Path.Combine("Project", "Example.cs"),
        [$"{AssemblyString}.ModuleSetup.cs"] = Path.Combine("Project", "ModuleSetup.cs"),
        [$"{AssemblyString}.FolderProfile.pubxml"] = Path.Combine("Project", "Properties", "PublishProfiles", "FolderProfile.pubxml"),
        [$"{AssemblyString}.FolderProfile.pubxml.user"] = Path.Combine("Project", "Properties", "PublishProfiles", "FolderProfile.pubxml.user"),
        [$"{AssemblyString}.TestProject.csproj"] = Path.Combine("TestProject", "TestProject.csproj"),
        [$"{AssemblyString}.UnitTest.cs"] = Path.Combine("TestProject", "UnitTest.cs"),
        [$"{AssemblyString}.gitignore"] = ".gitignore",
    };

    readonly string m_TemplateDir;

    public TemplateInfo(IAssemblyLoader assemblyLoader)
    {
        m_TemplateDir = ExtractToTempDir(assemblyLoader);
    }

    string ExtractToTempDir(IAssemblyLoader assemblyLoader)
    {
        var dir = Path.Combine(Path.GetTempPath(), "ugs", "cloudcode-template");
        var assembly = assemblyLoader.Load(AssemblyString);

        foreach (var (resourceName, relativePath) in k_ResourceToRelativePath)
        {
            var destPath = Path.Combine(dir, relativePath);
            var destDir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destDir))
                Directory.CreateDirectory(destDir);

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new FileLoadException($"Could not load resource '{resourceName}' from assembly '{AssemblyString}'");

            using var fileStream = File.Create(destPath);
            stream.CopyTo(fileStream);
        }

        return dir;
    }

    public string PathSolution => Path.Combine(m_TemplateDir, "Solution.sln");
    public string PathProject => Path.Combine(m_TemplateDir, "Project", "Project.csproj");
    public string PathExampleClass => Path.Combine(m_TemplateDir, "Project", "Example.cs");
    public string PathConfig => Path.Combine(m_TemplateDir, "Project", "Properties", "PublishProfiles", "FolderProfile.pubxml");
    public string PathConfigUser => Path.Combine(m_TemplateDir, "Project", "Properties", "PublishProfiles", "FolderProfile.pubxml.user");
}
