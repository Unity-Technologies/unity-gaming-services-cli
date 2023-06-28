using System.Globalization;
using Unity.Services.Cli.Authoring.Model;
using Newtonsoft.Json;
using Unity.Services.Cli.CloudCode.Utils;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using LanguageType = Unity.Services.CloudCode.Authoring.Editor.Core.Model.Language;

namespace Unity.Services.Cli.CloudCode.Deploy;

class CloudCodeScript : DeployContent, IScript
{
    [JsonConverter(typeof(ScriptNameJsonConverter))]
    public new ScriptName Name { get; init; }
    public LanguageType? Language { get; set; }
    public string Body { get; set; }
    public List<CloudCodeParameter> Parameters { get; set; }
    public string LastPublishedDate { get; set; }

    public CloudCodeScript()
        : this(
            default,
            default,
            "",
            "",
            new List<CloudCodeParameter>(),
            "")
    { }

    public CloudCodeScript(string name, string path, float progress, DeploymentStatus? status)
        : base(name, CloudCodeConstants.ServiceType, path, progress, status)
    {
        Name = ScriptName.FromPath(path);
        Language = LanguageType.JS;
        Path = path;
        Body = string.Empty;
        Parameters = new List<CloudCodeParameter>();
        LastPublishedDate = string.Empty;
    }

    public CloudCodeScript(
        ScriptName name,
        LanguageType? language,
        string path,
        string body,
        List<CloudCodeParameter> parameters,
        string lastPublishedDate)
    : base(name.GetNameWithoutExtension(), CloudCodeConstants.ServiceType, path, 0F, DeploymentStatus.Empty)
    {
        Name = name;
        Language = language;
        Path = path;
        Body = body;
        Parameters = parameters;
        LastPublishedDate = lastPublishedDate;
    }

    public CloudCodeScript(GetScriptResponse response)
    : base(response.Name, CloudCodeConstants.ServiceType, "", 0F, DeploymentStatus.Empty)
    {
        Path = "";
        Name = new ScriptName(response.Name);
        Language = (LanguageType)Enum.Parse(typeof(LanguageType), response.Language.ToString());
        if (response.ActiveScript is not null)
        {
            Body = response.ActiveScript.Code;
            Parameters = response.ActiveScript.Params.Select(p => p.ToCloudCodeParameter())
                .ToList();
            LastPublishedDate = response.ActiveScript.DatePublished.ToString(CultureInfo.InvariantCulture);
            return;
        }

        var lastVersion = response.Versions.LastOrDefault();
        Body = lastVersion is not null ? lastVersion.Code : "";

        Parameters = new List<CloudCodeParameter>();
        LastPublishedDate = "";
    }

    public CloudCodeScript(IScript script)
        : this(
            script.Name,
            script.Language,
            script.Path,
            script.Body,
            script.Parameters,
            script.LastPublishedDate)
    { }
}
