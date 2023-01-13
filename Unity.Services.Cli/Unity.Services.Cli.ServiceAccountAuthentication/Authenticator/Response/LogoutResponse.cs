namespace Unity.Services.Cli.ServiceAccountAuthentication;

/// <summary>
/// This is the response format for LogoutAsync
/// </summary>
public class LogoutResponse
{
    public string Information { get; }
    public string? Warning { get; }

    public LogoutResponse(string information, string? warning)
    {
        Information = information;
        Warning = warning;
    }
}
