using Unity.Services.Cli.Authoring.Model;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment.ModuleGeneration;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.CloudCode.Deploy;

class CloudCodeModulesLoader : ICloudCodeModulesLoader
{
    readonly IModuleBuilder m_ModuleBuilder;

    public CloudCodeModulesLoader(IModuleBuilder moduleBuilder)
    {
        m_ModuleBuilder = moduleBuilder;
    }

    public async Task<(List<IScript>, List<IScript>)> LoadModulesAsync(
        IReadOnlyList<string> ccmFilePaths, IReadOnlyList<string> solutionPaths, CancellationToken cancellationToken)
    {
        List<IScript> failedModules = new List<IScript>();

        var loadResult = LoadPrecompiledModulesAsync(ccmFilePaths);

        if (solutionPaths.Count > 0)
        {
            List<IScript> generatedModules;
            (generatedModules, failedModules) = await LoadModulesFromSolutionsAsync(solutionPaths, cancellationToken);
            loadResult.AddRange(generatedModules);
        }

        return (loadResult, failedModules);
    }

    async Task<(List<IScript>, List<IScript>)> LoadModulesFromSolutionsAsync(
        IReadOnlyList<string> solutionPaths,
        CancellationToken cancellationToken)
    {
        var generationList = new List<CloudCodeModule>();

        foreach (var solutionPath in solutionPaths)
        {
            var ccmr = new CloudCodeModule(solutionPath);
            try
            {
                await m_ModuleBuilder
                    .CreateCloudCodeModuleFromSolution(ccmr, cancellationToken);
            }
            catch (Exception e)
            {
                ccmr.Status =
                    new DeploymentStatus("Failed to compile", e.Message, SeverityLevel.Error);
            }
            generationList.Add(ccmr);
        }

        return (
            new List<IScript>(generationList.Where(module => !string.IsNullOrEmpty(module.CcmPath)).ToList()),
            new List<IScript>(generationList.Where(module => string.IsNullOrEmpty(module.CcmPath)).ToList()));
    }

    static List<IScript> LoadPrecompiledModulesAsync(IReadOnlyList<string> paths)
    {
        var modules = new List<IScript>();

        foreach (var path in paths)
        {
            modules.Add(
                CreateCloudCodeModule(path, new DeploymentStatus(Statuses.Loaded)));
        }

        return modules;
    }

    static CloudCodeModule CreateCloudCodeModule(string ccmPath, DeploymentStatus deploymentStatus)
    {
        return new CloudCodeModule(
            ScriptName.FromPath(ccmPath).ToString(),
            ccmPath,
            0,
            deploymentStatus);
    }
}
