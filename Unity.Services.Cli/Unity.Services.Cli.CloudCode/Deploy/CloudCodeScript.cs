using System.Globalization;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using Language = Unity.Services.CloudCode.Authoring.Editor.Core.Model.Language;

namespace Unity.Services.Cli.CloudCode.Deploy;

class CloudCodeScript : IScript
{
    public ScriptName Name { get; set; }
    public Language? Language { get; set; }
    public string? Path { get; set; }
    public string Body { get; }
    public List<CloudCodeParameter> Parameters { get; }
    public string LastPublishedDate { get; set; }

    public CloudCodeScript(
        ScriptName name,
        Language language,
        string path,
        string body,
        List<CloudCodeParameter> parameters,
        string lastPublishedDate)
    {
        Name = name;
        Language = language;
        Path = path;
        Body = body;
        Parameters = parameters;
        LastPublishedDate = lastPublishedDate;
    }

    public CloudCodeScript(GetScriptResponse response)
    {
        Name = new ScriptName(response.Name);
        Language = (Language) Enum.Parse(typeof(Language), response.Language.ToString());
        Body = response.ActiveScript.Code;
        Parameters = response.ActiveScript.Params.Select(p => p.ToCloudCodeParameter())
            .ToList();
        LastPublishedDate = response.ActiveScript.DatePublished.ToString(CultureInfo.InvariantCulture);
    }
}
