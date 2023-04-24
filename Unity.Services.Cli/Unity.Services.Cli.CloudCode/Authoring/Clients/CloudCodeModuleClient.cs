using Unity.Services.Cli.CloudCode.Authoring;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using Language = Unity.Services.Gateway.CloudCodeApiV1.Generated.Model.Language;

namespace Unity.Services.Cli.CloudCode.Deploy;

class CloudCodeModuleClient : ICSharpClient
{
    readonly ICloudCodeService m_CloudCodeService;
    readonly ICloudCodeInputParser m_CloudCodeInputParser;

    internal string ProjectId { get; set; }

    internal string EnvironmentId { get; set; }

    internal CancellationToken CancellationToken { get; set; }

    public CloudCodeModuleClient(
        ICloudCodeService service,
        ICloudCodeInputParser parser,
        string projectId = "",
        string environmentId = "",
        CancellationToken cancellationToken = default)
    {
        m_CloudCodeService = service;
        m_CloudCodeInputParser = parser;
        ProjectId = projectId;
        EnvironmentId = environmentId;
        CancellationToken = cancellationToken;
    }

    public void Initialize(string environmentId, string projectId, CancellationToken cancellationToken)
    {
        EnvironmentId = environmentId;
        ProjectId = projectId;
        CancellationToken = cancellationToken;
    }

    public async Task<ScriptName> UploadFromFile(IScript module)
    {
        var moduleNameWithoutExt = module.Name.GetNameWithoutExtension();
        await using var fileHandle = await m_CloudCodeInputParser.LoadModuleContentsAsync(module.Path);
        await m_CloudCodeService.UpdateModuleAsync(
            ProjectId,
            EnvironmentId,
            moduleNameWithoutExt,
            fileHandle,
            CancellationToken);

        return module.Name;
    }

    // Modules publish immediately upon creation.
    public Task Publish(ScriptName scriptName) { return Task.CompletedTask; }

    public async Task Delete(ScriptName moduleName)
    {
        await m_CloudCodeService.DeleteModuleAsync(
            ProjectId, EnvironmentId, moduleName.GetNameWithoutExtension(), CancellationToken);
    }

    public async Task<IScript> Get(ScriptName moduleName)
    {
        var resp = await m_CloudCodeService.GetModuleAsync(
            ProjectId, EnvironmentId, moduleName.GetNameWithoutExtension(), CancellationToken);

        return new CloudCodeModule(resp);
    }

    public async Task<List<ScriptInfo>> ListScripts()
    {
        var modules = await m_CloudCodeService.ListModulesAsync(ProjectId, EnvironmentId, CancellationToken);
        return modules.Select(ConvertToScriptInfo).ToList();

        ScriptInfo ConvertToScriptInfo(ListModulesResponseResultsInner result)
        {
            var extension = result.Language == Language.CS ? ".ccm" : "";
            var scriptInfo = new ScriptInfo(result.Name, extension);
            return scriptInfo;
        }
    }
}
