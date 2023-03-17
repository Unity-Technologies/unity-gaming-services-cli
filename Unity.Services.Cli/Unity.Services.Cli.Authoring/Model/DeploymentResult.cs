namespace Unity.Services.Cli.Authoring.Model;

[Serializable]
public class DeploymentResult
{

    public bool DryRun;
    public IReadOnlyCollection<DeployContent> Created;
    public IReadOnlyCollection<DeployContent> Updated;
    public IReadOnlyCollection<DeployContent> Deleted;
    public readonly IReadOnlyCollection<DeployContent> Deployed;
    public readonly IReadOnlyCollection<DeployContent> Failed;

    public DeploymentResult(
        IReadOnlyCollection<DeployContent> created,
        IReadOnlyCollection<DeployContent> updated,
        IReadOnlyCollection<DeployContent> deleted,
        IReadOnlyCollection<DeployContent> deployed,
        IReadOnlyCollection<DeployContent> failed,
        bool dryRun = false
        )
    {
        Created = created;
        Updated = updated;
        Deleted = deleted;
        Deployed = deployed;
        Failed = failed;

        DryRun = dryRun;
    }

    public DeploymentResult(IReadOnlyCollection<DeployContent> contents)
    {
        Created = new List<DeployContent>();
        Updated = new List<DeployContent>();
        Deleted = new List<DeployContent>();
        Deployed = contents.Where(c => c.Progress >= 100).ToList();
        Failed = contents.Where(c => c.Progress < 100f).ToList();
    }

    public override string ToString()
    {
        var result = "";

        if (DryRun)
        {
            result += $"This is a Dry Run. The result below is the expected result for this operation.";
        }

        result += Environment.NewLine;

        if (Created.Any())
        {
            var createdListString = string.Join(Environment.NewLine + "    ", Created.Select(x => x.Name));
            var info = DryRun ? "Will create:" : "Created:";
            result += info + Environment.NewLine + "    " + createdListString;
        }

        result += Environment.NewLine;

        if (Updated.Any())
        {
            var updatedListString = string.Join(Environment.NewLine + "    ", Updated.Select(x => x.Name));
            var info = DryRun ? "Will update:" : "Updated:";
            result += info + Environment.NewLine + "    " + updatedListString;
        }

        result += Environment.NewLine;

        if (Deleted.Any())
        {
            var deletedListString = string.Join(Environment.NewLine + "    ", Deleted.Select(x => x.Name));
            var info = DryRun ? "Will delete:" : "Deleted:";
            result += info + Environment.NewLine + "    " + deletedListString;
        }

        result += Environment.NewLine;

        if (Deployed.Any())
        {
            var deployedListString = string.Join(Environment.NewLine + "    ", Deployed.Select(x => x.Name));
            var info = DryRun ? "Will deploy:" : "Deployed:";
            result += info + Environment.NewLine + "    " + deployedListString;
        }
        else
        {
            result += "No content deployed";
        }

        // Log scripts that failed to deploy
        if (!Failed.Any()) return result;

        result += Environment.NewLine;

        result = Failed.Aggregate(result, (current, content) =>
            current + $"{Environment.NewLine}Failed to deploy:{Environment.NewLine}"+
                      $"    '{Path.GetFileName(content.Name)}' - Status: {content.Status}{Environment.NewLine}"+
                      $"    {content.Detail}");

        return result;
    }
}
