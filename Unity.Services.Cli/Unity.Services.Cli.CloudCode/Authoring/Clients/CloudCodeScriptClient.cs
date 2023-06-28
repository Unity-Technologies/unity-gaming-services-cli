using System.Net;
using Newtonsoft.Json;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Model;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.Cli.CloudCode.Service;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Client;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using Language = Unity.Services.Gateway.CloudCodeApiV1.Generated.Model.Language;

namespace Unity.Services.Cli.CloudCode.Authoring;

class CloudCodeScriptClient : IJavaScriptClient
{
    internal const string ContentTypeHeaderKey = "Content-Type";
    internal const string ProblemJsonHeader = "application/problem+json";
    internal const int DuplicatePublishErrorCode = 9018;

    readonly ICloudCodeService m_CloudCodeService;
    readonly ICloudCodeInputParser m_CloudCodeInputInputParser;
    readonly ICloudCodeScriptParser m_CloudCodeScriptParser;

    internal string ProjectId { get; set; }

    internal string EnvironmentId { get; set; }

    internal CancellationToken CancellationToken { get; set; }

    public CloudCodeScriptClient(
        ICloudCodeService service,
        ICloudCodeInputParser inputParser,
        ICloudCodeScriptParser scriptParser,
        string projectId = "",
        string environmentId = "",
        CancellationToken cancellationToken = default)
    {
        m_CloudCodeService = service;
        m_CloudCodeInputInputParser = inputParser;
        m_CloudCodeScriptParser = scriptParser;
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

    public async Task<ScriptName> UploadFromFile(IScript script)
    {
        var scriptNameWithoutExt = script.Name.GetNameWithoutExtension();
        var code = await m_CloudCodeInputInputParser.LoadScriptCodeAsync(script.Path, CancellationToken);
        var parametersParsingResult = await m_CloudCodeScriptParser.ParseScriptParametersAsync(code, CancellationToken);
        try
        {
            await m_CloudCodeService.UpdateAsync(
                ProjectId,
                EnvironmentId,
                scriptNameWithoutExt,
                code,
                parametersParsingResult.Parameters,
                CancellationToken);
        }
        catch (ApiException ex)
            when ((HttpStatusCode)ex.ErrorCode == HttpStatusCode.NotFound)
        {
            await m_CloudCodeService.CreateAsync(
                ProjectId,
                EnvironmentId,
                scriptNameWithoutExt,
                ScriptType.API,
                Language.JS,
                code,
                parametersParsingResult.Parameters,
                CancellationToken);
        }

        return script.Name;
    }

    public async Task Publish(ScriptName scriptName)
    {
        try
        {
            await m_CloudCodeService.PublishAsync(
                ProjectId,
                EnvironmentId,
                scriptName.GetNameWithoutExtension(),
                0,
                CancellationToken);
        }
        catch (ApiException e)
            when (IsDuplicatePublishError(e))
        {
            // Silence duplicate publish error.
        }
    }

    internal static bool IsDuplicatePublishError(ApiException e)
    {
        if (!HasProblemJson(e))
            return false;

        var json = e.ErrorContent?.ToString() ?? "";
        var parsedJsonError = JsonConvert.DeserializeObject<ApiJsonProblem>(json);
        return parsedJsonError is { Code: DuplicatePublishErrorCode };
    }

    internal static bool HasProblemJson(ApiException e)
    {
        return e.Headers != null
            && e.Headers.TryGetValue(ContentTypeHeaderKey, out var contentType)
            && contentType != null
            && contentType.Any(x => x.StartsWith(ProblemJsonHeader));
    }

    public async Task Delete(ScriptName scriptName)
    {
        await m_CloudCodeService.DeleteAsync(
            ProjectId, EnvironmentId, scriptName.GetNameWithoutExtension(), CancellationToken);
    }

    public async Task<IScript> Get(ScriptName scriptName)
    {
        var scriptResponse = await m_CloudCodeService.GetAsync(
            ProjectId, EnvironmentId, scriptName.GetNameWithoutExtension(), CancellationToken);

        return new CloudCodeScript(scriptResponse);
    }

    public async Task<List<ScriptInfo>> ListScripts()
    {
        var scripts = await m_CloudCodeService.ListAsync(ProjectId, EnvironmentId, CancellationToken);
        return scripts.Select(ConvertToScriptInfo).ToList();

        ScriptInfo ConvertToScriptInfo(ListScriptsResponseResultsInner result)
        {
            var extension = result.Language == Language.JS ? ".js" : "";
            var scriptInfo = new ScriptInfo(result.Name, extension, result.LastPublishedDate.ToString());
            return scriptInfo;
        }
    }
}
