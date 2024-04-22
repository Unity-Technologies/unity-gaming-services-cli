using Spectre.Console;

namespace Unity.Services.Cli.Common.Console;

public interface IProgressBar
{
    /// <summary>
    /// Method used to show progress bars on the console.
    /// </summary>
    /// <param name="callback">The callback that the progress bars will be tracking</param>
    /// <returns></returns>
    Task StartProgressAsync(Func<ProgressContext?, Task> callback);
}
