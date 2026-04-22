namespace Unity.Services.Cli.Common.Networking;

public class SchemaRegistryApiEndpoints : NetworkTargetEndpoints
{
    protected override string Prod { get; } = "https://services.api.unity.com/schema-registry/v1";

    protected override string Staging { get; } = "https://staging.services.api.unity.com/schema-registry/v1";
}
