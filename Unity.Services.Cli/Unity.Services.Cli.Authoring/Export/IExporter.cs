using Unity.Services.Cli.Authoring.Export.Input;

namespace Unity.Services.Cli.Authoring.Export;

public interface IExporter
{
    Task ExportAsync(ExportInput input, CancellationToken cancellationToken);
}
