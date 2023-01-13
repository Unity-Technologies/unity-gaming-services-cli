namespace Unity.Services.Cli.CloudCode.Deploy;

/// <remarks>
/// This provider will be consumed by the CloudCode Deploy handler
/// (from the CloudCode.Authoring library) to cache the environment.
/// </remarks>
class EnvironmentProvider : ICliEnvironmentProvider
{
    public string Current { get; set; } = "";
}
