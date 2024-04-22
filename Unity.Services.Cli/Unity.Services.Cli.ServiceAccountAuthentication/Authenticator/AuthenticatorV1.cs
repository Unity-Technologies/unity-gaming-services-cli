using System.Text;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Persister;
using Unity.Services.Cli.Common.SystemEnvironment;
using Unity.Services.Cli.ServiceAccountAuthentication.Exceptions;
using Unity.Services.Cli.ServiceAccountAuthentication.Input;

namespace Unity.Services.Cli.ServiceAccountAuthentication;

/// <summary>
/// Authenticator to the Identity API V1.
/// </summary>
class AuthenticatorV1 : IAuthenticator
{
    internal const string ServiceKeyId = "UGS_CLI_SERVICE_KEY_ID";
    internal const string ServiceSecretKey = "UGS_CLI_SERVICE_SECRET_KEY";
    internal const string EnvironmentVariablesAndConfigSetWarning
        = $"Because {ServiceKeyId} and {ServiceSecretKey} are set, you will still be able to make authenticated"
        + " service calls. Clear login-related system environment variables to fully logout.";

    internal static readonly TextPrompt<string> KeyIdPrompt = new TextPrompt<string>("Enter your key-id:")
        .Validate(s => s.Any(char.IsWhiteSpace) ? ValidationResult.Error("key-id should not contain white space.") : ValidationResult.Success());
    internal static readonly TextPrompt<string> SecretKeyPrompt = new TextPrompt<string>("Enter your secret-key:").Secret()
        .Validate(s => s.Any(char.IsWhiteSpace) ? ValidationResult.Error("secret-key should not contain white space.") : ValidationResult.Success());

    readonly IPersister<string> m_Persister;
    readonly IConsolePrompt m_CliPrompt;

    public AuthenticatorV1(IPersister<string> persister, IConsolePrompt cliPrompt)
    {
        m_Persister = persister;
        m_CliPrompt = cliPrompt;
    }

    /// <inheritdoc cref="IAuthenticator.LoginAsync"/>
    public async Task LoginAsync(LoginInput input, CancellationToken cancellationToken = default)
    {
        string keyId;
        string secretKey;
        if (input.HasSecretKeyOption
            || input.ServiceKeyId is not null)
        {
            (keyId, secretKey) = await ParseServiceAccountOptionsAsync(input);
        }
        else
        {
            if (!m_CliPrompt.InteractiveEnabled)
            {
                throw new InvalidLoginInputException($"Standard Input is redirected, please use the " +
                    $"\"{LoginInput.ServiceKeyIdAlias}\" and \"{LoginInput.ServiceSecretKeyAlias}\" options to login.");
            }
            (keyId, secretKey) = await PromptForServiceAccountKeysAsync(m_CliPrompt, cancellationToken);
        }

        //TODO: validate token?
        var token = CreateToken(keyId, secretKey);
        await m_Persister.SaveAsync(token, cancellationToken);
    }

    internal static async Task<(string, string)> PromptForServiceAccountKeysAsync(
        IConsolePrompt cliPrompt, CancellationToken cancellationToken)
    {
        var keyId = await cliPrompt.PromptAsync(KeyIdPrompt, cancellationToken);
        var secretKey = await cliPrompt.PromptAsync(SecretKeyPrompt, cancellationToken);
        return (keyId, secretKey);
    }

    internal static async Task<(string keyId, string secretKey)> ParseServiceAccountOptionsAsync(LoginInput input)
    {
        var hasKeyIdOption = input.ServiceKeyId is not null;
        if (hasKeyIdOption != input.HasSecretKeyOption)
        {
            string usedOptionAlias;
            string missingOptionAlias;
            if (hasKeyIdOption)
            {
                usedOptionAlias = LoginInput.ServiceKeyIdAlias;
                missingOptionAlias = LoginInput.ServiceSecretKeyAlias;
            }
            else
            {
                usedOptionAlias = LoginInput.ServiceSecretKeyAlias;
                missingOptionAlias = LoginInput.ServiceKeyIdAlias;
            }

            throw new InvalidLoginInputException(
                $"\"{missingOptionAlias}\" option must be used along \"{usedOptionAlias}\".");
        }

        if (string.IsNullOrWhiteSpace(input.ServiceKeyId))
        {
            throw new InvalidLoginInputException("The service key ID can't be empty.");
        }

        if (!Console.IsInputRedirected)
        {
            throw new InvalidLoginInputException(
                "You have to provide your service secret key through standard"
                + $" input when using {LoginInput.ServiceKeyIdAlias}.");
        }

        var secretKey = await Console.In.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidLoginInputException("The service secret key can't be empty.");

        return (input.ServiceKeyId!, secretKey);
    }

    internal static string CreateToken(string serviceKey, string serviceSecret)
    {
        var decodedToken = $"{serviceKey}:{serviceSecret}";
        var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(decodedToken));
        return token;
    }

    public static string? GetTokenFromEnvironmentVariables(ISystemEnvironmentProvider environmentProvider,
        out string warning)
    {
        string? serviceKey = environmentProvider
            .GetSystemEnvironmentVariable(ServiceKeyId, out _);
        string? serviceSecret = environmentProvider
            .GetSystemEnvironmentVariable(ServiceSecretKey, out _);
        warning = "";

        if (!string.IsNullOrWhiteSpace(serviceKey) && !string.IsNullOrWhiteSpace(serviceSecret))
        {
            return CreateToken(serviceKey, serviceSecret);
        }

        return null;
    }

    /// <inheritdoc cref="IAuthenticator.LogoutAsync"/>
    public async Task<LogoutResponse> LogoutAsync(ISystemEnvironmentProvider environmentProvider,
        CancellationToken cancellationToken = default)
    {
        const string logoutInfo = "Service Account key cleared from local configuration.";
        string? environmentVarWarning = null;

        await m_Persister.DeleteAsync(cancellationToken);

        var envToken = GetTokenFromEnvironmentVariables(environmentProvider, out _);
        if (!string.IsNullOrEmpty(envToken))
        {
            environmentVarWarning = EnvironmentVariablesAndConfigSetWarning;
        }

        return new LogoutResponse(logoutInfo, environmentVarWarning);
    }

    /// <inheritdoc cref="IAuthenticator.GetTokenAsync"/>
    public async Task<string?> GetTokenAsync(CancellationToken cancellationToken = default)
        => await m_Persister.LoadAsync(cancellationToken);
}
