namespace Unity.Services.Cli.Authoring.Import;

public class ImportResult<T>
{
    public readonly IEnumerable<T> Imported;
    public readonly IEnumerable<T> Failed;

    public ImportResult(IEnumerable<T> imported, IEnumerable<T> failed)
    {
        Imported = imported;
        Failed = failed;
    }
}
