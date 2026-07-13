namespace Unity.Services.Cli.ServiceAccountAuthentication;

public class LoginResult
{
    public static readonly LoginResult ServiceAccount = new();

    public string? DisplayName { get; }

    public LoginResult(string? displayName = null)
    {
        DisplayName = displayName;
    }
}
