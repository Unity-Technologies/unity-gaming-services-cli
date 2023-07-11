using Spectre.Console;

namespace Unity.Services.Cli.Common.Console;

/// <summary>
/// Cli Prompt to wrap IPrompt in Spectre Console
/// </summary>
public interface ICliPrompt
{
    /// <summary>
    /// Is the standard input redirected.
    /// Should default to System.Console.IsInputRedirected.
    /// </summary>
    bool IsStandardInputRedirected { get; }

    /// <summary>
    /// Execute expected prompt and return user input for the prompt.
    /// </summary>
    /// <param name="prompt">
    /// The prompt to use.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to cancel this task.
    /// </param>
    /// <typeparam name="T">
    /// The type of input expected from user.
    /// </typeparam>
    /// <returns>
    /// Return the value from user.
    /// </returns>
    Task<T> PromptAsync<T>(IPrompt<T> prompt, CancellationToken cancellationToken);
}
