using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using File = Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model.File;

namespace Unity.Services.Cli.GameServerHosting.Model;

public class FilesItemOutput
{
    public FilesItemOutput(File file)
    {
        CreatedAt = file.CreatedAt;
        FileSize = file.FileSize;
        Filename = file.Filename;
        Fleet = new FilesItemFleetOutput(file.Fleet);
        LastModified = file.LastModified;
        Machine = new FilesItemMachineOutput(file.Machine);
        Path = file.Path;
        ServerId = file.ServerID;
    }

    public string Filename { get; }
    public string Path { get; }
    public DateTime CreatedAt { get; }
    public DateTime LastModified { get; }
    public long FileSize { get; }
    public FilesItemFleetOutput Fleet { get; }
    public FilesItemMachineOutput Machine { get; }
    public long ServerId { get; }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }
}
