using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Unity.Services.Access.Authoring.Core.Model;
using Unity.Services.Cli.Authoring.Templates;

namespace Unity.Services.Cli.Access.Models;

public class NewProjectAccessFile : IFileTemplate
{
    [JsonProperty("$schema")]
    public string Value { get; set; }

    [JsonIgnore]
    public string Extension => ".ac";

    [JsonIgnore]
    public string FileBodyText => JsonConvert.SerializeObject(this, GetSerializationSettings());

    static JsonSerializerSettings GetSerializationSettings()
    {
        var settings = new JsonSerializerSettings()
        {
            Converters =
            {
                new StringEnumConverter()
            },
            Formatting = Formatting.Indented,
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
        };
        return settings;
    }

    public NewProjectAccessFile() : this(
        name: "project-statements",
        path: "project-statements.ac",
        statements: GetDefaultStatements(),
        value: "https://ugs-config-schemas.unity3d.com/v1/project-access-policy.schema.json")
    {
    }

    public NewProjectAccessFile(string name, string path, List<AccessControlStatement> statements, string value)
    {
        Path = path;
        Name = name;
        Statements = statements;
        Value = value;
    }

    static List<AccessControlStatement> GetDefaultStatements()
    {
        return new List<AccessControlStatement>()
        {
            new AccessControlStatement()
            {
                Sid = "DenyAccessToAllServices",
                Effect = "Deny",
                Action = new List<string>
                {
                    "*"
                },
                Principal = "Player",
                Resource = "urn:ugs:*",
                Version = "1.0.0"
            }
        };
    }

    [JsonIgnore]
    public string Name { get; }

    [JsonIgnore]
    public string Path { get; set; }

    public List<AccessControlStatement> Statements { get; set; }
}
