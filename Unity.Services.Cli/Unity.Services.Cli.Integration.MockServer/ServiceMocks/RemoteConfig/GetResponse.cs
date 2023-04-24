namespace Unity.Services.Cli.MockServer.ServiceMocks.RemoteConfig;

[Serializable]
class GetResponse
{
    public List<Config>? Configs { get; set; }
}
