namespace Unity.Services.Cli.RemoteConfig.Model;

[Serializable]
public class UpdateConfigRequest
{
    public string? Type { get; set; }
    public IEnumerable<object>? Value { get; set; }
}
