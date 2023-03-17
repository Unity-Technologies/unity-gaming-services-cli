using Unity.Services.Cli.Authoring.Import.Input;

namespace Unity.Services.Cli.Authoring.Import;

public interface IImporter
{
    Task ImportAsync(ImportInput input, CancellationToken cancellationToken);
}
