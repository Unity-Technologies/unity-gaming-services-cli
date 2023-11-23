using File = Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model.File;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.GameServerHosting.Model;

public class FilesOutput : List<FilesItemOutput>
{
    public FilesOutput(IReadOnlyCollection<File>? files)
    {
        if (files != null) AddRange(files.Select(f => new FilesItemOutput(f)));
    }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
