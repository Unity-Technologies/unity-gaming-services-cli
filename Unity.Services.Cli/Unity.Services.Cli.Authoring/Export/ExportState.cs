using Unity.Services.Cli.Authoring.Model;

namespace Unity.Services.Cli.Authoring.Export;

public record ExportState<T>(IReadOnlyCollection<ImportExportEntry<T>> ToExport)
{
    internal IEnumerable<ImportExportItem> ExportedItems() => ToExport.Select(e => e.ToImportExportItem(ImportExportAction.Export));
}
