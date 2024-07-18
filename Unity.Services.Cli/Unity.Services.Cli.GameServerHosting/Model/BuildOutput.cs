using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Model;

class BuildOutput
{
    public BuildOutput(BuildListInner build)
    {
        BuildName = build.BuildName;
        BuildId = build.BuildID;
        OsFamily = build.OsFamily;
        BuildVersionName = build.BuildVersionName;
        Updated = build.Updated;
        BuildConfigurations = build.BuildConfigurations;
        SyncStatus = build.SyncStatus;
        // Ccd can be null, the codegen doesn't handle this case
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (build.Ccd != null) Ccd = new CcdOutput(build.Ccd);
        Container = build.Container;
        S3 = build.S3;
        Gcs = build.Gcs;
    }

    public BuildOutput(CreateBuild200Response build)
    {
        BuildName = build.BuildName;
        BuildId = build.BuildID;
        OsFamily = (BuildListInner.OsFamilyEnum?)build.OsFamily;
        BuildVersionName = build.BuildVersionName;
        Updated = build.Updated;
        SyncStatus = (BuildListInner.SyncStatusEnum)build.SyncStatus;
        // Ccd can be null, the codegen doesn't handle this case
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (build.Ccd != null) Ccd = new CcdOutput(build.Ccd);
        Container = build.Container;
        S3 = build.S3;
        Gcs = build.Gcs;
    }

    public string BuildVersionName { get; }

    public string BuildName { get; }

    public long BuildId { get; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public BuildListInner.OsFamilyEnum? OsFamily { get; }

    [YamlMember(SerializeAs = typeof(string))]
    public DateTime Updated { get; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
    public long? BuildConfigurations { get; }

    public BuildListInner.SyncStatusEnum SyncStatus { get; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public CcdOutput? Ccd { get; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public ContainerImage Container { get; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public AmazonS3Details S3 { get; }

    [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
    public GoogleCloudStorageDetails Gcs { get; }
    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
