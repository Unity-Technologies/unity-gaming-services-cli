using System.IO.Abstractions;
using Newtonsoft.Json;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Lobby.Handlers;
using Unity.Services.Cli.Lobby.Handlers.Config;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Lobby.Deploy;

interface ILobbyResourceLoader
{
    Task<LobbyDeploymentItem> LoadResource(
        string filePath,
        CancellationToken cancellationToken);
}

class LobbyResourceLoader : ILobbyResourceLoader
{
    readonly IFileSystem m_FileSystem;

    public LobbyResourceLoader(IFileSystem fileSystem)
    {
        m_FileSystem = fileSystem;
    }

    public async Task<LobbyDeploymentItem> LoadResource(
        string filePath,
        CancellationToken cancellationToken)
    {
        try
        {
            var json = await m_FileSystem.File.ReadAllTextAsync(filePath, cancellationToken);
            var fileContent = JsonConvert.DeserializeObject<LobbyConfigFileContent>(json);
            if (fileContent == null)
            {
                throw new JsonException("File is empty or invalid JSON");
            }

            var config = new LobbyConfig
            {
                SchemaId = string.IsNullOrEmpty(fileContent.SchemaId)
                    ? LobbyConstants.SchemaId
                    : fileContent.SchemaId,
                Config = fileContent.ToConfigJObject()
            };
            return new LobbyDeploymentItem(filePath, config);
        }
        catch (Exception e) when (e is JsonException or IOException)
        {
            return new LobbyDeploymentItem(
                filePath,
                status: new DeploymentStatus(
                    "Failed to Load",
                    $"Failed to read '{filePath}'. Reason: {e.Message}",
                    SeverityLevel.Error));
        }
    }
}
