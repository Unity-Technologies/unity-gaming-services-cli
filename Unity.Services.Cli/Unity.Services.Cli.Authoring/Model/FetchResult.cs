using System.Text;

namespace Unity.Services.Cli.Authoring.Model;

/// <summary>
/// Contain the data summary of a fetch operation.
/// </summary>
[Serializable]
public class FetchResult
{
    internal const string EmptyFetchMessage = "No content fetched";
    internal const string FetchedHeader = "Successfully fetched into the following files";
    internal const string FailedHeader = "Failed to fetch";
    internal const string UpdatedHeader = "Updated";
    internal const string CreatedHeader = "Created";
    internal const string DeletedHeader = "Deleted";

    /// <summary>
    /// All local resources modified by the fetch command (created, updated, and deleted).
    /// </summary>
    public IReadOnlyList<string> Fetched { get; }

    /// <summary>
    /// All resources (local or remote) that couldn't be handled by the fetch command.
    /// </summary>
    public IReadOnlyList<string> Failed { get; }

    /// <summary>
    /// All local resources updated by the fetch command.
    /// </summary>
    public IReadOnlyList<string> Updated { get; }

    /// <summary>
    /// All local resources created by the fetch command.
    /// </summary>
    public IReadOnlyList<string> Created { get; }

    /// <summary>
    /// All local resources deleted by the fetch command.
    /// </summary>
    public IReadOnlyList<string> Deleted { get; }

    public FetchResult(
        IReadOnlyList<string> updated,
        IReadOnlyList<string> deleted,
        IReadOnlyList<string> created,
        IReadOnlyList<string> fetched,
        IReadOnlyList<string> failed)
    {
        Updated = updated;
        Created = created;
        Deleted = deleted;
        Fetched = fetched;
        Failed = failed;
    }

    public FetchResult(IReadOnlyList<FetchResult> results)
    {
        Updated = results.SelectMany(r => r.Updated).ToList();
        Created = results.SelectMany(r => r.Created).ToList();
        Deleted = results.SelectMany(r => r.Deleted).ToList();
        Fetched = results.SelectMany(r => r.Fetched).ToList();
        Failed = results.SelectMany(r => r.Failed).ToList();
    }

    public override string ToString()
    {
        var result = new StringBuilder();
        AppendFetched(result);
        AppendResult(result, Failed, FailedHeader);
        AppendResult(result, Updated, UpdatedHeader);
        AppendResult(result, Deleted, DeletedHeader);
        AppendResult(result, Created, CreatedHeader);

        return result.ToString();
    }

    void AppendFetched(StringBuilder builder)
    {
        if (Fetched.Any())
        {
            AppendResult(builder, Fetched, FetchedHeader);
        }
        else
        {
            builder.AppendLine(EmptyFetchMessage);
        }
    }

    internal static void AppendResult(StringBuilder builder, IEnumerable<string> results, string resultHeader)
    {
        var resultList = results.ToList();
        if (!resultList.Any())
            return;

        // Add empty entry at first to start at next line.
        var joinedUpdated = string.Join($"{Environment.NewLine}    ", resultList.Prepend(""));
        if (builder.Length > 0)
        {
            builder.AppendLine();
        }

        builder.AppendLine($"{resultHeader}:{joinedUpdated}");
    }
}
