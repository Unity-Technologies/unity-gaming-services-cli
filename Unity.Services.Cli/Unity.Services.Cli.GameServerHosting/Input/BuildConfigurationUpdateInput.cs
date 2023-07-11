using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.GameServerHosting.Input;

public class BuildConfigurationUpdateInput : BuildConfigurationCreateInput
{
    public const string BuildConfigIdKey = "build-configuration-id";

    public static readonly Argument<long> BuildConfigIdArgument = new(BuildConfigIdKey, "The ID of the build configuration to update");

    [InputBinding(nameof(BuildConfigIdArgument))]
    public long BuildConfigId { get; init; }
}
