namespace Unity.Services.Cli.Authoring.DeploymentDefinition;

interface IDeploymentDefinitionFilteringResult
{
    public IDeploymentDefinitionFiles DefinitionFiles { get; }
    public Dictionary<string, IReadOnlyList<string>> AllFilesByExtension { get; }
    public string GetExclusionsLogMessage();
}
