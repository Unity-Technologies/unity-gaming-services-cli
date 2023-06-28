using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Authoring.Model;

[Serializable]
public abstract class AuthorResult
{
    internal const string UpdatedHeader = "Updated";
    internal const string CreatedHeader = "Created";
    internal const string DeletedHeader = "Deleted";
    internal const string DryUpdatedHeader = "Will update";
    internal const string DryCreatedHeader = "Will create";
    internal const string DryDeletedHeader = "Will delete";

    internal string AuthoredHeader => $"Successfully {Operation}ed the following files";
    internal string DryAuthoredHeader => $"Will {Operation} following files";
    internal string DryFailedHeader => $"Will fail to {Operation}";
    internal string FailedHeader => $"Failed to {Operation}";
    internal string NoActionMessage => $"No content {Operation}ed";

    internal abstract string Operation { get; }

    static AuthorResult()
    {
        JsonConvert.DefaultSettings = () =>
        {
            return new JsonSerializerSettings()
            {
                ContractResolver = new DeployContentContractResolver()
            };
        };
    }

    /// <summary>
    /// All local resources modified by the fetch command (created, updated, and deleted).
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<IDeploymentItem> Authored { get; protected set; }

    /// <summary>
    /// All resources (local or remote) that couldn't be handled by the fetch command.
    /// </summary>
    public IReadOnlyList<IDeploymentItem> Failed { get; protected set; }

    /// <summary>
    /// Whether this result constitutes a dry-run, or not.
    /// A dry-run is a prediction on what will happen on the deploy/fetch execution
    /// </summary>
    public bool DryRun { get; }

    /// <summary>
    /// All local resources updated by the fetch command.
    /// </summary>
    public IReadOnlyList<IDeploymentItem> Updated { get; protected set; }

    /// <summary>
    /// All local resources created by the fetch command.
    /// </summary>
    public IReadOnlyList<IDeploymentItem> Created { get; protected set; }

    /// <summary>
    /// All local resources deleted by the fetch command.
    /// </summary>
    public IReadOnlyList<IDeploymentItem> Deleted { get; protected set; }

    protected AuthorResult(IReadOnlyList<IDeploymentItem> results)
    {
        Authored = results.Where(r => r.Progress >= 100).ToList();
        Failed = results.Where(r => r.Progress < 100).ToList();
        Created = Array.Empty<IDeploymentItem>();
        Updated = Array.Empty<IDeploymentItem>();
        Deleted = Array.Empty<IDeploymentItem>();
    }

    protected AuthorResult(
        IReadOnlyList<IDeploymentItem> updated,
        IReadOnlyList<IDeploymentItem> deleted,
        IReadOnlyList<IDeploymentItem> created,
        IReadOnlyList<IDeploymentItem> authored,
        IReadOnlyList<IDeploymentItem> failed,
        bool dryRun)
    {
        Updated = updated;
        Created = created;
        Deleted = deleted;
        Authored = authored;
        Failed = failed;
        DryRun = dryRun;
    }

    protected AuthorResult(IReadOnlyList<AuthorResult> results, bool dryRun = false)
    {
        Updated = results.SelectMany(r => r.Updated).ToList();
        Created = results.SelectMany(r => r.Created).ToList();
        Deleted = results.SelectMany(r => r.Deleted).ToList();
        Authored = results.SelectMany(r => r.Authored).ToList();
        Failed = results.SelectMany(r => r.Failed).ToList();
        DryRun = dryRun;
    }

    public override string ToString()
    {
        var result = new StringBuilder();

        if (DryRun)
        {
            result.AppendLine($"This is a Dry Run. The result below is the expected result for this operation.");
        }

        AppendFetched(result);
        AppendResult(result, Failed, !DryRun ? FailedHeader : DryFailedHeader);
        AppendResult(result, Updated, !DryRun ? UpdatedHeader : DryUpdatedHeader);
        AppendResult(result, Deleted, !DryRun ? DeletedHeader : DryDeletedHeader);
        AppendResult(result, Created, !DryRun ? CreatedHeader : DryCreatedHeader);

        return result.ToString();
    }

    void AppendFetched(StringBuilder builder)
    {
        if (Authored.Any())
        {
            AppendResult(builder, Authored, !DryRun ? AuthoredHeader : DryAuthoredHeader);
        }
        else
        {
            builder.AppendLine(NoActionMessage);
        }
    }

    internal static void AppendResult(StringBuilder builder, IEnumerable<IDeploymentItem> results, string resultHeader)
    {
        var resultList = results.Select(d =>
        {
            if (d.Status.MessageSeverity is SeverityLevel.Success or SeverityLevel.Info or SeverityLevel.None)
                return $"{d}";
            return $"{d} - Status: {d.Status.Message} - {d.Status.MessageDetail}";
        }).ToList();

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

    public class DeployContentContractResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            if (!type.IsAssignableTo(typeof(IDeploymentItem)))
                return base.CreateProperties(type, memberSerialization);

            IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);
            var propertiesToInclude = typeof(DeployContent).GetProperties().Select(p => p.Name).ToList();

            properties =
                properties.Where(p => propertiesToInclude.Contains( p.PropertyName!) ).ToList();

            return properties;
        }
    }
}
