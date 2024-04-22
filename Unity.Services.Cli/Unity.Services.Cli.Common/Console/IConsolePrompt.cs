using Spectre.Console;

namespace Unity.Services.Cli.Common.Console;

/// <summary>
/// Cli Prompt to wrap IPrompt in Spectre Console
/// </summary>
public interface IConsolePrompt
{
    /// <summary>
    /// Is the standard input redirected.
    /// Should default to System.Console.IsInputRedirected.
    /// </summary>
    bool InteractiveEnabled { get; }

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

    Task<T> PromptAsync<T>(
        string title,
        CancellationToken cancellationToken);

    Task<T> SelectionPromptAsync<T>(
        string title,
        ICollection<T> choices,
        CancellationToken cancellationToken,
        int pageSize)
        where T : notnull;

    Task<List<T>> MultiSelectionPromptAsync<T>(
        string title,
        ICollection<T> choices,
        CancellationToken cancellationToken,
        int pageSize)
        where T : notnull;

    Task<bool> ConfirmPromptAsync(string title, CancellationToken cancellationToken);
}
