using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.GameServerHosting.Input;

class BuildUpdateInput : BuildIdInput
{
    public const string NameKey = "--name";

    public static readonly Option<string> BuildNameOption = new(NameKey, "The name of the build")
    {
        IsRequired = true
    };

    [InputBinding(nameof(BuildNameOption))]
    public string? BuildName { get; init; }
}
