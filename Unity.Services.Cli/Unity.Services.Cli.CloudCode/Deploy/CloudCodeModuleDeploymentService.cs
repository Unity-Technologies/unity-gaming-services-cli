using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.Authoring.Service;
using Unity.Services.Cli.CloudCode.Authoring;
using Unity.Services.Cli.CloudCode.Model;
using Unity.Services.Cli.CloudCode.Utils;
using Unity.Services.CloudCode.Authoring.Editor.Core.Deployment;
using Unity.Services.CloudCode.Authoring.Editor.Core.IO;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;

namespace Unity.Services.Cli.CloudCode.Deploy;

class CloudCodeModuleDeploymentService : IDeploymentService
{
    internal ICloudCodeModulesLoader CloudCodeModulesLoader { get; }
    internal ICliEnvironmentProvider EnvironmentProvider { get; }
    internal ICSharpClient CliCloudCodeClient { get; }
    ICloudCodeDeploymentHandler CloudCodeDeploymentHandler { get; }

    public string ServiceType => m_ServiceType;

    public string ServiceName => m_ServiceName;

    public static string OutputPath = "module-compilation";

    public IReadOnlyList<string> FileExtensions { get; } = new[]
    {
        CloudCodeConstants.FileExtensionModulesCcm,
        CloudCodeConstants.FileExtensionModulesSln,
    };

    readonly string m_ServiceType;
    readonly string m_ServiceName;
    readonly IDeployFileService m_DeployFileService;
    readonly ISolutionPublisher m_SolutionPublisher;
    readonly IModuleZipper m_ModuleZipper;
    readonly IFileSystem m_FileSystem;

    public CloudCodeModuleDeploymentService(
        ICloudCodeDeploymentHandler deployHandler,
        ICloudCodeModulesLoader cloudCodeModulesLoader,
        ICliEnvironmentProvider environmentProvider,
        ICSharpClient client,
        IDeployFileService deployFileService,
        ISolutionPublisher solutionPublisher,
        IModuleZipper moduleZipper,
        IFileSystem fileSystem)
    {
        CloudCodeModulesLoader = cloudCodeModulesLoader;
        EnvironmentProvider = environmentProvider;
        CliCloudCodeClient = client;
        CloudCodeDeploymentHandler = deployHandler;
        m_DeployFileService = deployFileService;
        m_SolutionPublisher = solutionPublisher;
        m_ModuleZipper = moduleZipper;
        m_FileSystem = fileSystem;

        m_ServiceType = CloudCodeConstants.ServiceTypeModules;
        m_ServiceName = CloudCodeConstants.ServiceNameModules;
    }

    public async Task<DeploymentResult> Deploy(
        DeployInput deployInput,
        IReadOnlyList<string> filePaths,
        string projectId,
        string environmentId,
        StatusContext? loadingContext,
        CancellationToken cancellationToken)
    {
        CliCloudCodeClient.Initialize(environmentId, projectId, cancellationToken);
        EnvironmentProvider.Current = environmentId;

        loadingContext?.Status($"Reading {m_ServiceType}...");

        var (ccmFilePaths, slnFilePaths) = ListFilesToDeploy(filePaths.ToList());

        var failedResultList = new List<IScript>();

        if (slnFilePaths.Count > 0)
        {
            loadingContext?.Status("Generating Cloud Code Modules for solution files...");

            var (generatedCcmFilePaths, failedGenerationResult) =
                await CompileModules(slnFilePaths, cancellationToken);

            ccmFilePaths.AddRange(generatedCcmFilePaths);
            ccmFilePaths = ccmFilePaths.Distinct().ToList();

            failedResultList.AddRange(failedGenerationResult);
        }

        loadingContext?.Status($"Loading {m_ServiceName} modules...");

        var loadResult = await CloudCodeModulesLoader.LoadPrecompiledModulesAsync(
            ccmFilePaths,
            m_ServiceType);

        loadingContext?.Status($"Deploying {m_ServiceType}...");

        var dryrun = deployInput.DryRun;
        var reconcile = deployInput.Reconcile;
        DeployResult result = null!;

        try
        {
            result = await CloudCodeDeploymentHandler.DeployAsync(loadResult, reconcile, dryrun);
            failedResultList.AddRange(result.Failed);
        }
        catch (ApiException)
        {
            /*
             * Ignoring this because we already catch exceptions from UpdateScriptStatus() for each script and we don't
             * want to stop execution when a script generates an exception.
             */
        }
        catch (DeploymentException ex)
        {
            result = ex.Result;
        }

        return ConstructResult(loadResult, result, deployInput, failedResultList);
    }

    static CloudCodeModule SetUpFailedCloudCodeModule(ModuleGenerationResult failedGenerationSolution)
    {
        return new CloudCodeModule(
            ScriptName.FromPath(failedGenerationSolution.SolutionPath).ToString(),
            failedGenerationSolution.SolutionPath,
            0,
            new DeploymentStatus(Statuses.FailedToRead, "Could not generate module for solution."));
    }

    async Task<(List<string>, List<IScript>)> CompileModules(List<string> slnFilePaths, CancellationToken cancellationToken)
    {
        var ccmFilePaths = new List<string>();
        var failedToGenerateList = new List<IScript>();
        var generateTasks = new List<Task<ModuleGenerationResult>>();
        foreach (var slnFilePath in slnFilePaths)
        {
            generateTasks.Add(CreateCloudCodeModuleFromSolution(slnFilePath, cancellationToken));
        }

        if (generateTasks.Count > 0)
        {
            var generationResultList = await Task.WhenAll(generateTasks);
            foreach (var generationResult in generationResultList)
            {
                if (generationResult.Success)
                {
                    ccmFilePaths.Add(generationResult.CcmPath);
                }
                else
                {
                    failedToGenerateList.Add(SetUpFailedCloudCodeModule(generationResult));
                }
            }
        }

        return (ccmFilePaths, failedToGenerateList);
    }

    (List<string>, List<string>) ListFilesToDeploy(List<string> filePaths)
    {
        List<string> ccmFilePaths = new List<string>();
        List<string> slnFilePaths = new List<string>();
        if (filePaths.Count > 0)
        {
            ccmFilePaths = m_DeployFileService.ListFilesToDeploy(
                filePaths,
                CloudCodeConstants.FileExtensionModulesCcm,
                false).ToList();
            slnFilePaths = m_DeployFileService.ListFilesToDeploy(
                filePaths,
                CloudCodeConstants.FileExtensionModulesSln,
                false).ToList();
        }

        return (ccmFilePaths, slnFilePaths);
    }

    static DeploymentResult ConstructResult(List<IScript> loadResult, DeployResult? result, DeployInput deployInput, List<IScript> failedModules)
    {
        DeploymentResult deployResult;
        if (result == null)
        {
            deployResult = new DeploymentResult(loadResult.OfType<IDeploymentItem>().ToList());
        }
        else
        {
            deployResult = new DeploymentResult(
                result.Updated.Select(item => item as IDeploymentItem).ToList() as IReadOnlyList<IDeploymentItem>,
                ToDeleteDeploymentItems(result.Deleted, deployInput.DryRun),
                result.Created.Select(item => item as IDeploymentItem).ToList() as IReadOnlyList<IDeploymentItem>,
                result.Deployed.Select(item => item as IDeploymentItem).ToList() as IReadOnlyList<IDeploymentItem>,
                failedModules.Select(item => item as IDeploymentItem).ToList() as IReadOnlyList<IDeploymentItem>,
                deployInput.DryRun);
        }

        return deployResult;
    }

    static IReadOnlyList<IDeploymentItem> ToDeleteDeploymentItems(IReadOnlyList<IScript> modules, bool dryRun)
    {
        var contents = new List<IDeploymentItem>();

        foreach (var module in modules)
        {
            var deletedCloudCode = new DeletedCloudCode(module.Name.ToString(), module.Language.ToString()!, module.Path);
            contents.Add(deletedCloudCode);
            if (!dryRun)
            {
                deletedCloudCode.Status = new DeploymentStatus("Deployed", "Deleted remotely", SeverityLevel.Success);
                deletedCloudCode.Progress = 100f;
            }
        }

        return contents;
    }

    async Task<ModuleGenerationResult> CreateCloudCodeModuleFromSolution(
        string solutionPath,
        CancellationToken cancellationToken)
    {
        var success = true;

        var slnName = Path.GetFileNameWithoutExtension(solutionPath);
        var dllOutputPath = Path.Combine(Path.GetTempPath(), slnName);
        var moduleCompilationPath = Path.Combine(dllOutputPath, "module-compilation");

        var ccmPath = dllOutputPath;
        try
        {
            var moduleName = await m_SolutionPublisher.PublishToFolder(
                solutionPath,
                moduleCompilationPath,
                cancellationToken);
            ccmPath = await m_ModuleZipper.ZipCompilation(moduleCompilationPath, moduleName, cancellationToken);
        }
        catch (Exception)
        {
            success = false;
        }

        return new ModuleGenerationResult(solutionPath, ccmPath, success);
    }

    class ModuleGenerationResult
    {
        public string SolutionPath { get; }
        public string CcmPath { get; }
        public bool Success { get; }

        public ModuleGenerationResult(string solutionPath, string ccmPath, bool success)
        {
            SolutionPath = solutionPath;
            CcmPath = ccmPath;
            Success = success;
        }
    }
}
