using System.Text;
using Unity.Services.Deployment.Core.Model;

namespace Unity.Services.Cli.Authoring.DeploymentDefinition;

public class DeploymentDefinitionFileIntersectionException : Exception
{
    readonly Dictionary<IDeploymentDefinition, List<string>> m_FileIntersection;
    readonly bool m_IsExclude;

    public override string Message => GetIntersectionMessage();

    internal DeploymentDefinitionFileIntersectionException(
        Dictionary<IDeploymentDefinition, List<string>> fileIntersection,
        bool isExcludes)
    {
        m_FileIntersection = fileIntersection;
        m_IsExclude = isExcludes;
    }

    string GetIntersectionMessage()
    {
        var sb = new StringBuilder();
        sb.Append(
            m_IsExclude
                ? "A conflict was found between the deployment definition exclusions and the other arguments:"
                : "A conflict was found between the deployment definitions and the other arguments:");

        foreach (var (ddef, excludedFiles) in m_FileIntersection)
        {
            foreach (var excludedFile in excludedFiles)
            {
                sb.Append($"{Environment.NewLine}\t'{excludedFile}' [{ddef.Name}.ddef]");
            }
        }

        return sb.ToString();
    }
}
