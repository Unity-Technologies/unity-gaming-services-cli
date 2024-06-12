using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.CloudCode.Model;

class GetScriptResponseOutput
{
    public string Name { get; }
    public string Language { get; }
    public string Type { get; }

    public List<int?> Versions { get; }

    public ActiveScriptOutput ActiveScript { get; }

    public GetScriptResponseOutput(GetScriptResponse response)
    {
        Name = response.Name;
        Language = response.Language;
        Type = response.Type;
        ActiveScript = new ActiveScriptOutput();
        if (response.ActiveScript is not null)
        {
            ActiveScript = new ActiveScriptOutput(response.ActiveScript);
        }

        Versions = response.Versions.Where(v => v._Version is not null).ToList()
            .ConvertAll(v => v._Version);
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

