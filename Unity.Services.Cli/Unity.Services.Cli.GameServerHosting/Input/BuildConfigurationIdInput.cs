using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.GameServerHosting.Input;

public class BuildConfigurationIdInput : CommonInput
{
    public const string BuildConfigurationIdKey = "build-configuration-id";

    public static readonly Argument<string> BuildConfigurationIdArgument = new(
        BuildConfigurationIdKey,
        "The unique ID of the build configuration"
    );

    [InputBinding(nameof(BuildConfigurationIdArgument))]
    public string? BuildConfigurationId { get; init; }

    static BuildConfigurationIdInput()
    {
        BuildConfigurationIdArgument.AddValidator(result =>
        {
            var value = result.GetValueOrDefault<string>();
            try
            {
                _ = long.Parse(value);
            }
            catch (Exception)
            {
                result.ErrorMessage = $"Build Configuration ID '{value}' not a valid ID.";
            }
        });
    }
}
