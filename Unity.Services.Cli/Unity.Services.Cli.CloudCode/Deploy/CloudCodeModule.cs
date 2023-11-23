using System.Globalization;
using Newtonsoft.Json;
using Unity.Services.Cli.CloudCode.Utils;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using LanguageType = Unity.Services.CloudCode.Authoring.Editor.Core.Model.Language;

namespace Unity.Services.Cli.CloudCode.Deploy;

class CloudCodeModule : ModuleDeployContent, IScript, IModuleItem
{
    [JsonConverter(typeof(ScriptNameJsonConverter))]
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global used for deserialization
    public new ScriptName Name { get; set; }
    public LanguageType? Language { get; set; }
    public string Body { get; set; }
    public List<CloudCodeParameter> Parameters { get; }
    public string LastPublishedDate { get; set; }
    [JsonIgnore]
    public string SignedUrl { get; set; }

    public CloudCodeModule(string solutionPath)
        : this(
            default,
            default,
            "",
            "",
            new List<CloudCodeParameter>(),
            "")
    {
        SolutionPath = solutionPath;
    }

    public CloudCodeModule()
        : this(
            default,
            default,
            "",
            "",
            new List<CloudCodeParameter>(),
            "")
    { }

    public CloudCodeModule(
        ScriptName name,
        LanguageType? language,
        string path,
        string body,
        List<CloudCodeParameter> parameters,
        string lastPublishedDate)
        : base(
            name.GetNameWithoutExtension(),
            CloudCodeConstants.ServiceTypeModules,
            path,
            0f,
            DeploymentStatus.Empty)
    {
        Name = name;
        Language = language;
        Path = path;
        Body = body;
        Parameters = parameters;
        LastPublishedDate = lastPublishedDate;
        SignedUrl = "";
    }

    public CloudCodeModule(string name, string path, float progress, DeploymentStatus? status, string signedUrl = "")
        : base(name, CloudCodeConstants.ServiceTypeModules, path, progress, status)
    {
        Name = ScriptName.FromPath(path);
        Language = LanguageType.JS;
        Path = path;
        Body = string.Empty;
        Parameters = new List<CloudCodeParameter>();
        LastPublishedDate = string.Empty;
        SignedUrl = signedUrl;
    }

    public CloudCodeModule(
        ScriptName name,
        LanguageType language,
        string path,
        string signedUrl = "")
        : base(
            name.GetNameWithoutExtension(),
            "",
            path,
            0f,
            DeploymentStatus.Empty)
    {
        Name = name;
        Language = language;
        Path = path;
        Body = "";
        Parameters = new List<CloudCodeParameter>();
        LastPublishedDate = "";
        SignedUrl = signedUrl;
    }

    public CloudCodeModule(GetModuleResponse response)
        : base(
            response.Name,
            CloudCodeConstants.ServiceTypeModules,
            "",
            0F,
            DeploymentStatus.Empty)
    {
        Name = new ScriptName(response.Name);
        Language = (LanguageType)Enum.Parse(typeof(LanguageType), response.Language.ToString());
        Body = "";
        Parameters = new List<CloudCodeParameter>();
        LastPublishedDate = response.DateModified.ToString(CultureInfo.InvariantCulture);
        SignedUrl = "";
    }

    public string SolutionPath { get; } = "";

    public string CcmPath
    {
        get => Path;
        set => Path = value;
    }

    public string ModuleName
    {
        get => Name.ToString();
        set
        {
            Name = new ScriptName(value);
            base.Name = System.IO.Path.GetFileNameWithoutExtension(value);
        }
    }
}
