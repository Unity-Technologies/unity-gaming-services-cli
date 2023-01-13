using System.CommandLine.Builder;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Common;

/// <summary>
/// Helper to provide extension to CommandLineBuilder to add service command module
/// </summary>
public static class CommandLineBuilderHelper
{
    /// <summary>
    /// Extension for CommandLineBuilder to add a command module to current command
    /// </summary>
    /// <param name="builder">builder to call this method</param>
    /// <param name="commandModule">command module of a service to be added to builder</param>
    /// <typeparam name="T">type of a service module implementing ICommandModule</typeparam>
    public static CommandLineBuilder AddModule<T>(this CommandLineBuilder builder, T commandModule)
        where T : ICommandModule
    {
        foreach (var command in commandModule.GetCommandsForCliRoot())
        {
            builder.Command.Add(command);
        }

        return builder;
    }

    public static CommandLineBuilder AddGlobalCommonOptions(this CommandLineBuilder builder)
    {
        builder.Command.AddGlobalOption(CommonInput.QuietOption);
        builder.Command.AddGlobalOption(CommonInput.JsonOutputOption);
        return builder;
    }
}
