using Unity.Services.Cli.Triggers.Deploy;

namespace Unity.Services.Cli.Triggers.IO;

interface ITriggersResourceLoader
{
    Task<TriggersFileItem> LoadResource(
        string filePath,
        CancellationToken cancellationToken);
}
