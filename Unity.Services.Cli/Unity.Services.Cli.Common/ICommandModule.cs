using System.CommandLine;

namespace Unity.Services.Cli.Common;

/// <summary>
/// Command Module interface. Each service command module should implement this interface to be added as a ugs service command
/// </summary>
public interface ICommandModule
{
    /// <summary>
    /// The root command of a service command module
    /// </summary>
    Command? ModuleRootCommand { get; }

    /// <summary>
    /// Get the list of commands to add directly to the CLI's base command if any.
    /// </summary>
    /// <returns>
    /// Return the list of commands to add directly to the CLI's base command if any;
    /// return empty array otherwise.
    /// </returns>
    /// <remarks>
    /// Override this method only if you need to inject multiple commands to the CLI's base command.
    /// </remarks>
    IEnumerable<Command> GetCommandsForCliRoot()
    {
        if (ModuleRootCommand is null)
            return Array.Empty<Command>();

        return new[]
        {
            ModuleRootCommand
        };
    }
}
