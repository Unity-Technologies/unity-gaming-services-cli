using Unity.Services.Cli.Authoring.Model;

namespace Unity.Services.Cli.Authoring.Import;

public record ImportState<T>(
    IReadOnlyCollection<ImportExportEntry<T>> ToCreate,
    IReadOnlyCollection<ImportExportEntry<T>> ToUpdate,
    IReadOnlyCollection<ImportExportEntry<T>> ToDelete
    )
{
    public AggregateException? ImportExceptions()
    {
        var exceptions = ToCreate.Select(c => c.Exception)
            .Concat(ToUpdate.Select(c => c.Exception))
            .Concat(ToDelete.Select(c => c.Exception))
            .Where(e => e != null)
            .Cast<Exception>()
            .ToList();

        if (exceptions.Any())
        {
            return new AggregateException(exceptions);
        }

        return null;
    }

    internal IEnumerable<ImportExportItem> CreatedItems() => ToCreate.Select(e => e.ToImportExportItem(ImportExportAction.Create));
    internal IEnumerable<ImportExportItem> UpdatedItems() => ToUpdate.Select(e => e.ToImportExportItem(ImportExportAction.Update));
    internal IEnumerable<ImportExportItem> DeletedItems() => ToDelete.Select(e => e.ToImportExportItem(ImportExportAction.Delete));
}
