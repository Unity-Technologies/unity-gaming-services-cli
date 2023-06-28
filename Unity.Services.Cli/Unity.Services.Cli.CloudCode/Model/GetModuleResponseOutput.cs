using System.Globalization;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.CloudCode.Model;

class GetModuleResponseOutput
{
    public string Name { get; }
    public Language Language { get; }
    public String DateModified { get; }
    public String DateCreated { get; }

    public GetModuleResponseOutput(GetModuleResponse response)
    {
        Name = response.Name;
        Language = response.Language;
        DateModified = response.DateModified.ToString("s", CultureInfo.InvariantCulture);
        DateCreated = response.DateCreated.ToString("s", CultureInfo.InvariantCulture);
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
