using Spectre.Console;

namespace Unity.Services.Cli.Common.Console;

public interface ILoadingIndicator
{
    /// <summary>
    /// Method used to show a loading indicator with a description on the console.
    /// </summary>
    /// <param name="description">Description of the loading indicator</param>
    /// <param name="callback">The callback that the status will be tracking</param>
    /// <returns></returns>
    public Task StartLoadingAsync(string description, Func<StatusContext?, Task> callback);
}
