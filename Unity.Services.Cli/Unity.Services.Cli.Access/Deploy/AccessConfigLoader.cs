using Newtonsoft.Json;
using System.IO.Abstractions;
using Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.Json;
using Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.Model;
using Unity.Services.DeploymentApi.Editor;
using IFileSystem = Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.IO.IFileSystem;
using Statuses = Unity.Services.Cli.Authoring.Model.Statuses;

namespace Unity.Services.Cli.Access.Deploy;

class AccessConfigLoader : IAccessConfigLoader
{
    readonly IPath m_Path;
    readonly IFileSystem m_FileSystem;
    readonly IJsonConverter m_JsonConverter;


    public AccessConfigLoader(IFileSystem fileSystem, IPath path, IJsonConverter jsonConverter)
    {
        m_Path = path;
        m_FileSystem = fileSystem;
        m_JsonConverter = jsonConverter;
    }

    public async Task<LoadResult> LoadFilesAsync(IReadOnlyList<string> filePaths, CancellationToken token)
    {
        var loaded = new List<ProjectAccessFile>();
        var failed = new List<ProjectAccessFile>();

        foreach (var filePath in filePaths)
        {
            var name = m_Path.GetFileName(filePath);
            var file = new ProjectAccessFile
            {
                Name = name,
                Path = filePath
            };

            try
            {
                var fileText = await m_FileSystem.ReadAllText(filePath, token);
                var content = m_JsonConverter.DeserializeObject<ProjectAccessFileContent>(fileText);
                if (content is null)
                {
                    throw new JsonException($"{filePath} is not a valid resource");
                }

                file.Statements = content.ToAuthoringStatements(file, new ProjectAccessParser());

                loaded.Add(file);
                file.Status = new DeploymentStatus(Statuses.Loaded, "");
            }
            catch (Exception ex)
            {
                file.Status = new DeploymentStatus(Statuses.FailedToRead, ex.Message, SeverityLevel.Error);
                failed.Add(file);
            }
        }

        return new LoadResult(loaded, failed);
    }
}
