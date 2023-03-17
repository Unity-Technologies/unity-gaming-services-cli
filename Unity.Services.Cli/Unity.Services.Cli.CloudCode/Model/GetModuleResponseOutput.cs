using System.Globalization;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.CloudCode.Model;

internal class GetModuleResponseOutput
{
    public string Name { get; }
    public Language Language { get; }
    public String DateModified { get; }
    public String DateCreated { get; }

    public GetModuleResponseOutput(GetModuleResponse response)
    {
        Name = response.Name;
        Language = response.Language;
        DateModified = TruncateDate(response.DateModified);
        DateCreated = TruncateDate(response.DateCreated);
    }

    public override string ToString()
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(this);
    }

    static String TruncateDate(DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFK", CultureInfo.InvariantCulture);
    }
}
