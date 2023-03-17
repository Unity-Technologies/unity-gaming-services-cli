using System.Globalization;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using Language = Unity.Services.CloudCode.Authoring.Editor.Core.Model.Language;

namespace Unity.Services.Cli.CloudCode.Deploy;

class CloudCodeModule : IScript
{
    public ScriptName Name { get; set; }
    public Language? Language { get; set; }
    public string? Path { get; set; }
    public string Body { get; }
    public List<CloudCodeParameter> Parameters { get; }
    public string LastPublishedDate { get; set; }

    public CloudCodeModule(
        ScriptName name,
        Language language,
        string path)
    {
        Name = name;
        Language = language;
        Path = path;
        Body = "";
        Parameters = new List<CloudCodeParameter>();
        LastPublishedDate = "";
    }

    public CloudCodeModule(GetModuleResponse response)
    {
        Name = new ScriptName(response.Name);
        Language = (Language)Enum.Parse(typeof(Language), response.Language.ToString());
        Body = "";
        Parameters = new List<CloudCodeParameter>();
        LastPublishedDate = response.DateModified.ToString(CultureInfo.InvariantCulture);
    }
}
