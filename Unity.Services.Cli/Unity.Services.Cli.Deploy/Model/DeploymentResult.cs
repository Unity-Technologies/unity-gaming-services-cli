namespace Unity.Services.Cli.Deploy.Model;

[Serializable]
public class DeploymentResult
{
    public readonly IReadOnlyCollection<DeployContent> Deployed;
    public readonly IReadOnlyCollection<DeployContent> Failed;

    public DeploymentResult(IReadOnlyCollection<DeployContent> deployed, IReadOnlyCollection<DeployContent> failed)
    {
        Deployed = deployed;
        Failed = failed;
    }

    public DeploymentResult(IReadOnlyCollection<DeployContent> contents)
    {
        Deployed = contents.Where(c => c.Progress >= 100).ToList();
        Failed = contents.Where(c => c.Progress < 100).ToList();
    }

    public override string ToString()
    {
        var result = "";
        if (Deployed.Any())
        {
            var deployedListString = string.Join(Environment.NewLine + "    ", Deployed.Select(x => x.Name));
            result = "Successfully deployed the following contents:" +
                Environment.NewLine + "    " + deployedListString;
        }
        else
        {
            result = "No content deployed";
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
