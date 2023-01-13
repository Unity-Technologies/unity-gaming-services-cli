using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.RemoteConfig.Model;
using Unity.Services.Cli.RemoteConfig.Service;
using Unity.Services.Cli.RemoteConfig.Types;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;
using Unity.Services.RemoteConfig.Editor.Authoring.Core.Networking;
using ValueType = Unity.Services.Cli.RemoteConfig.Types.ValueType;

namespace Unity.Services.Cli.RemoteConfig.Deploy;

class RemoteConfigClient : ICliRemoteConfigClient
{
    internal const string k_ConfigType = "settings";

    internal string ConfigId { get; private set; }
    public string ProjectId { get; private set; }
    public string EnvironmentId { get; private set; }
    public CancellationToken CancellationToken { get; private set; }

    readonly IRemoteConfigService m_Service;

    static readonly JsonSerializerSettings k_JsonSerializerSettings = new ()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    public void Initialize(string projectId, string environmentId, CancellationToken cancellationToken)
    {
        ProjectId = projectId;
        EnvironmentId = environmentId;
        CancellationToken = cancellationToken;
        ConfigId = string.Empty;
    }

    internal RemoteConfigClient(
        IRemoteConfigService service,
        string projectId,
        string environmentId,
        CancellationToken cancellationToken)
    {
        m_Service = service;
        ProjectId = projectId;
        EnvironmentId = environmentId;
        CancellationToken = cancellationToken;
        ConfigId = string.Empty;
    }

    public RemoteConfigClient(IRemoteConfigService service)
    {
        m_Service = service;
        ProjectId = string.Empty;
        EnvironmentId = string.Empty;
        CancellationToken = CancellationToken.None;
        ConfigId = string.Empty;
    }

    public Task UpdateAsync(IEnumerable<RemoteConfigEntry> remoteConfigEntries)
    {
        var kvps = remoteConfigEntries
            .Select(re => new ConfigValue(re.key, GetValueFromString(re.type), re.value))
            .ToList();
        return m_Service.UpdateConfigAsync(ProjectId, ConfigId, k_ConfigType, kvps, CancellationToken);
    }

    public async Task CreateAsync(IEnumerable<RemoteConfigEntry> remoteConfigEntries)
    {
        var kvps = remoteConfigEntries
            .Select(re => new ConfigValue(re.key, GetValueFromString(re.type), re.value))
            .ToList();

        string id = await m_Service.CreateConfigAsync(ProjectId, EnvironmentId, k_ConfigType, kvps, CancellationToken);

        ConfigId = id;
    }

    public async Task<GetConfigsResult> GetAsync()
    {
        var rawResponse = await m_Service.GetAllConfigsFromEnvironmentAsync(ProjectId, EnvironmentId, k_ConfigType, CancellationToken);
        var res = new GetConfigsResult(false, null);

        var response = JsonConvert.DeserializeObject<GetResponse>(rawResponse, k_JsonSerializerSettings)!;
        if (response.Configs?.Count == 0)
            return res;

        var config = response.Configs?.FirstOrDefault(c => c.Type == k_ConfigType);
        if (config == null)
            return res;

        ConfigId = config.Id!;
        res = new GetConfigsResult(true, config.Value);

        return res;
    }

    internal static ValueType GetValueFromString(string type)
    {
        if (!Enum.TryParse(type, ignoreCase: true, out ValueType result))
        {
            throw new CliException($"Failed to parse RemoteConfig key type '{type}'", ExitCode.UnhandledError);
        }

        return result;
    }
}
