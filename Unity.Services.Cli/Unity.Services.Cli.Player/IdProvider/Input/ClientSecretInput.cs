using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.Player.IdProvider.Input;

/// <summary>
/// Standard input, the only channel a client secret enters the identity provider commands through.
/// </summary>
public interface IClientSecretInput
{
    bool IsRedirected { get; }

    Task<string?> ReadLineAsync(CancellationToken cancellationToken);
}

class ConsoleClientSecretInput : IClientSecretInput
{
    public bool IsRedirected => Console.IsInputRedirected;

    public Task<string?> ReadLineAsync(CancellationToken cancellationToken) =>
        Console.In.ReadLineAsync(cancellationToken).AsTask();
}

static class ClientSecretReader
{
    /// <summary>
    /// Read the client secret from a redirected standard input, as <c>ugs login --secret-key-stdin</c> does.
    /// </summary>
    /// <param name="command">The id-provider command reading it, named in the hint when standard input is a terminal.</param>
    /// <exception cref="CliException">Standard input is a terminal, or carries no secret.</exception>
    public static async Task<string> ReadAsync(IClientSecretInput input, string command, CancellationToken cancellationToken)
    {
        if (!input.IsRedirected)
        {
            throw new CliException(
                $"{IdProviderInput.ClientSecretStdinAlias} reads the secret from standard input, but it is a terminal. "
                + $"Pipe it in, for example from a secret store: `<read-secret> | ugs player id-provider {command} ... {IdProviderInput.ClientSecretStdinAlias}`.",
                ExitCode.HandledError);
        }

        var secret = await input.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new CliException("Standard input carried no client secret.", ExitCode.HandledError);
        }

        return secret;
    }
}
