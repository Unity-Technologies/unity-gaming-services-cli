namespace Unity.Services.Cli.RemoteConfig.Model;

[Serializable]
class GetResponse
{
    public List<Config>? Configs { get; set; }
}
