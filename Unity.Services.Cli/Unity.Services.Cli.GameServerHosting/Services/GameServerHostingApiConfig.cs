namespace Unity.Services.Cli.GameServerHosting.Services;

public class GameServerHostingApiConfig
{
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set; }
    public string? CcdApiKey { get; set; }
}
