using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.CloudCode.Model;

class GetScriptResponseOutput
{
    public string Name { get; }
    public string Language { get; }
    public string Type { get; }

    /// <summary>
    /// Either the published version numbers (the default, unchanged shape) or, when the caller asks
    /// for detail, a list of per-version maps. Holding the rendered maps rather than a typed list
    /// keeps the YAML and the -j JSON identical by construction, including which keys are omitted.
    /// </summary>
    public object Versions { get; }

    public ActiveScriptOutput ActiveScript { get; }

    public GetScriptResponseOutput(GetScriptResponse response, bool detailedVersions = false)
    {
        Name = response.Name;
        Language = response.Language;
        Type = response.Type;
        ActiveScript = new ActiveScriptOutput();
        if (response.ActiveScript is not null)
        {
            ActiveScript = new ActiveScriptOutput(response.ActiveScript);
        }

        Versions = detailedVersions
            // The working copy is included here but not in the default shape: it has a null version,
            // so the bare projection has never been able to represent it.
            ? response.Versions.Select(v => new ScriptVersionOutput(v)).ToList()
            : response.Versions.Where(v => v._Version is not null).ToList()
                .ConvertAll(v => v._Version);
    }

    public override string ToString()
    {
        // Built as a dictionary rather than serialising `this`, so the expanded versions can be
        // rendered through ScriptVersionOutput.ToDictionary() and keep the YAML's conditional keys.
        // The key order matches the previous property order, so the default output is unchanged.
        var data = new Dictionary<string, object?>
        {
            { "name", Name },
            { "language", Language },
            { "type", Type },
            { "versions", VersionsForYaml() },
            { "activeScript", ActiveScript }
        };

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(data);
    }

    object VersionsForYaml()
    {
        return Versions is List<ScriptVersionOutput> expanded
            ? expanded.Select(version => version.ToDictionary()).ToList()
            : Versions;
    }




}

