using System.Globalization;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Unity.Services.Cli.CloudCode.Model;

class GetModuleResponseOutput
{
    public string Name { get; }
    public string Language { get; }
    public string DateModified { get; }
    public string DateCreated { get; }
    public Dictionary<string,string>? Tags { get; }
    public string? SignedDownloadUrl { get; }
    public bool? HasError { get; }
    public string? ErrorMessage { get; }
    public object? Endpoints { get; }

    public GetModuleResponseOutput(GetModuleResponse response)
    {
        Name = response.Name;
        Language = response.Language;
        DateModified = response.DateModified.ToString("s", CultureInfo.InvariantCulture);
        DateCreated = response.DateCreated.ToString("s", CultureInfo.InvariantCulture);
        Tags = response.Tags;
        SignedDownloadUrl = response.SignedDownloadURL;
        HasError = response.HasError;
        ErrorMessage = HasError == true ? response.ErrorMessage : null;
        Endpoints = response.Endpoints;
    }

    public override string ToString()
    {
        var data = new Dictionary<string, object?>
        {
            { "name", Name },
            { "language", Language },
            { "dateModified", DateModified },
            { "dateCreated", DateCreated }
        };
        if (Tags != null && Tags.Count > 0)
            data["tags"] = Tags;
        if (!string.IsNullOrEmpty(SignedDownloadUrl))
            data["signedDownloadURL"] = SignedDownloadUrl;
        if (HasError.HasValue)
            data["hasError"] = HasError.Value;
        if (HasError == true && !string.IsNullOrEmpty(ErrorMessage))
            data["errorMessage"] = ErrorMessage;
        if (Endpoints is not null)
            data["endpoints"] = Endpoints;

        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .DisableAliases()
            .Build();
        return serializer.Serialize(data);
    }
}
