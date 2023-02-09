using System.Text;

namespace Unity.Services.Cli.Deploy.Model;

[Serializable]
public class FetchResult
{
    public  IReadOnlyList<string> Fetched { get; }
    public IReadOnlyList<string> Failed { get; }
    public IReadOnlyList<string> Updated { get; }
    public IReadOnlyList<string> Created { get; }
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
        Updated = results.SelectMany( r=> r.Updated).ToList();
        Created = results.SelectMany( r=> r.Created).ToList();
        Deleted = results.SelectMany( r=> r.Deleted).ToList();
        Fetched = results.SelectMany(r => r.Fetched).ToList();
        Failed = results.SelectMany( r=> r.Failed).ToList();
    }

    public override string ToString()
    {
        var result = new StringBuilder();
        AddFetched(result);
        AddFailed(result);

        BuildResult(result, Updated, "Updated");
        BuildResult(result, Deleted, "Deleted");
        BuildResult(result, Created, "Created");

        return result.ToString();
    }

    private void AddFetched(StringBuilder result)
    {
        if (Fetched.Any())
        {
            var deployedListString = string.Join($"{Environment.NewLine}    ", Fetched);
            result.Append(
                $"Successfully fetched into the following files:{Environment.NewLine}    {deployedListString}");
        }
        else
        {
            result.Append("No content fetched");
        }
    }

    private void AddFailed(StringBuilder result)
    {
        if (!Failed.Any())
            return;

        result.AppendLine();

        foreach (var file in Failed)
        {
            result.Append($"{Environment.NewLine}Failed to fetch:");
            result.Append($"{Environment.NewLine}    '{Path.GetFileName(file)}'");
        }
    }

    static void BuildResult(StringBuilder strBuilder, IReadOnlyList<string> results, string resultHeader)
    {
        if (!results.Any())
            return;

        strBuilder.Append(Environment.NewLine);
        strBuilder.Append($"{Environment.NewLine}{resultHeader}:");
        foreach (var updated in results)
        {
            strBuilder.Append($"{Environment.NewLine}    {updated}");
        }
    }
}
