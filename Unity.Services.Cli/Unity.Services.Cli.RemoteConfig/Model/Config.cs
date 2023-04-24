using Unity.Services.RemoteConfig.Editor.Authoring.Core.Model;

namespace Unity.Services.Cli.RemoteConfig.Model;

[Serializable]
class Config
{
    public string? ProjectId { get; set; }
    public string? EnvironmentId { get; set; }
    public string? Type { get; set; }
    public string? Id { get; set; }
    public string? Version { get; set; }
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
    public List<RemoteConfigEntryDTO>? Value { get; set; }
}
