using System.CommandLine;

namespace Unity.Services.Cli.Common.UnitTest;

/// <summary>
/// Dummy implementation of <see cref="ICommandModule"/> to test its default implementations.
/// </summary>
/// <remarks>
/// Can't use Mock as it overrides default behaviours.
/// </remarks>
class DummyModule : ICommandModule
{
    public Command? ModuleRootCommand { get; init; }
}
