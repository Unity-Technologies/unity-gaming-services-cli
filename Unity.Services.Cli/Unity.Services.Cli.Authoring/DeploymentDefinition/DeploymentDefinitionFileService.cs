using System.IO.Abstractions;
using Unity.Services.Cli.Authoring.DeploymentDefinition;
using Unity.Services.Deployment.Core.Model;

namespace Unity.Services.Cli.Authoring.Service;

class DeploymentDefinitionFileService : DeployFileService, IDeploymentDefinitionFileService
{
    readonly IPath m_Path;
    readonly IDeploymentDefinitionFactory m_Factory;

    public DeploymentDefinitionFileService(
        IFile file,
        IDirectory directory,
        IPath path,
        IDeploymentDefinitionFactory factory)
        : base(file, directory, path)
    {
        m_Path = path;
        m_Factory = factory;
    }

    public DeploymentDefinitionInputResult GetDeploymentDefinitionsForInput(IEnumerable<string> inputPaths)
    {
        var inputDdefPaths = ListFilesToDeploy(inputPaths.ToList(), CliDeploymentDefinitionService.Extension, true);
        var allDdefPaths = new List<string>();
        foreach (var inputDdefPath in inputDdefPaths)
        {
            var directoryName = m_Path.GetDirectoryName(inputDdefPath);
            if (directoryName != null)
            {
                allDdefPaths.AddRange(ListFilesToDeploy(directoryName, CliDeploymentDefinitionService.Extension, false));
            }
        }

        allDdefPaths = allDdefPaths.Distinct().ToList();

        var inputDdefs = new List<IDeploymentDefinition>();
        var allDdefs = new List<IDeploymentDefinition>();
        var ddefDirectories = new Dictionary<string, IDeploymentDefinition>();
        foreach (var ddefPath in allDdefPaths)
        {
            var ddef = m_Factory.CreateDeploymentDefinition(ddefPath);

            var ddefDirectory = m_Path.GetDirectoryName(ddefPath);
            if (ddefDirectory != null)
            {
                if (!ddefDirectories.ContainsKey(ddefDirectory))
                {
                    ddefDirectories.Add(ddefDirectory, ddef);
                }
                else
                {
                    throw new MultipleDeploymentDefinitionInDirectoryException(
                        ddefDirectories[ddefDirectory],
                        ddef,
                        ddefDirectory);
                }
            }

            allDdefs.Add(ddef);
            if (inputDdefPaths.Contains(ddefPath))
            {
                inputDdefs.Add(ddef);
            }
        }

        return new DeploymentDefinitionInputResult(inputDdefs, allDdefs);
    }

    public IReadOnlyList<string> GetFilesForDeploymentDefinition(
        IDeploymentDefinition deploymentDefinition,
        string extension)
    {
        var files = new List<string>();
        var ddefDirectory = m_Path.GetDirectoryName(deploymentDefinition.Path);
        if (ddefDirectory != null)
        {
            var filesForExtension = ListFilesToDeploy(
                ddefDirectory,
                extension,
                false);
            files.AddRange(filesForExtension);
        }

        return files;
    }
}
