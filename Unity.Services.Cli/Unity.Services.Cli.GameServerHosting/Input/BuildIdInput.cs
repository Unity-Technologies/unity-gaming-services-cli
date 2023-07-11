using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.GameServerHosting.Input;

class BuildIdInput : CommonInput
{
    public const string BuildIdKey = "build-id";

    public static readonly Argument<string> BuildIdArgument = new(BuildIdKey, "The unique ID of the build");

    static BuildIdInput()
    {
        BuildIdArgument.AddValidator(result =>
        {
            var value = result.GetValueOrDefault<string>();
            try
            {
                _ = long.Parse(value);
            }
            catch (Exception)
            {
                result.ErrorMessage = $"Build ID '{value}' not a valid ID.";
            }
        });
    }

    [InputBinding(nameof(BuildIdArgument))]
    public string? BuildId { get; init; }
}
