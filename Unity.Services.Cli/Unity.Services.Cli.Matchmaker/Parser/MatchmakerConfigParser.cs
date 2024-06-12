using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Matchmaker.Authoring.Core.Fetch;
using Unity.Services.Matchmaker.Authoring.Core.IO;
using Unity.Services.Matchmaker.Authoring.Core.Model;
using Unity.Services.Matchmaker.Authoring.Core.Parser;

namespace Unity.Services.Cli.Matchmaker.Parser;

class MatchmakerConfigParser : IMatchmakerConfigParser, IDeepEqualityComparer
{
    readonly IFileSystem m_FileSystem;

    public class CustomContractResolver : CamelCasePropertyNamesContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            // Check if a DataMember attribute is present
            var dataMemberAttribute = member.GetCustomAttribute<DataMemberAttribute>();
            if (dataMemberAttribute != null)
            {
                // If a Name has been provided, use it
                if (!string.IsNullOrEmpty(dataMemberAttribute.Name))
                {
                    property.PropertyName = dataMemberAttribute.Name;
                }

                if (dataMemberAttribute.IsRequired)
                {
                    property.Required = Required.Always;
                }
            }
            return property;
        }
    }

    public static readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        Formatting = Formatting.Indented,
        ContractResolver = new CustomContractResolver(),
        NullValueHandling = NullValueHandling.Ignore,
        Converters = new List<JsonConverter>
        {
            new DataMemberEnumConverter(),
            new ResourceNameConverter(),
            new MatchHostingConfigTypeConverted(),
            new JsonObjectSpecializedConverter(),
        }
    };

    public MatchmakerConfigParser(IFileSystem fileSystem)
    {
        m_FileSystem = fileSystem;
    }

    public async Task<IMatchmakerConfigParser.ParsingResult> Parse(IReadOnlyList<string> filePaths,
        CancellationToken ct)
    {
        var result = new IMatchmakerConfigParser.ParsingResult();
        var queueNames = new List<string>();
        var environmentConfigPresent = false;

        foreach (var filePath in filePaths)
        {
            string fileText;
            MatchmakerConfigResource mmConfigFile = new MatchmakerConfigResource
            {
                Name = "",
                Path = filePath,
            };
            try
            {
                fileText = await m_FileSystem.ReadAllText(filePath, ct);
            }
            catch (FileSystemException ex)
            {
                mmConfigFile.Status = new DeploymentStatus($"Failed to read file {filePath}", ex.ToString(), SeverityLevel.Error);
                result.failed.Add(mmConfigFile);
                continue;
            }

            try
            {
                switch (Path.GetExtension(filePath))
                {
                    case IMatchmakerConfigParser.QueueConfigExtension:
                        var queueConfig = JsonConvert.DeserializeObject<QueueConfig>(fileText, JsonSerializerSettings);
                        mmConfigFile.Content = queueConfig;
                        if (queueConfig == null)
                        {
                            break;
                        }
                        if (queueNames.Contains(queueConfig.Name.ToString()))
                        {
                            mmConfigFile.Status = new DeploymentStatus(
                                $"Multiple queue config files named {queueConfig.Name} found",
                                $"Multiple queue config files named {queueConfig.Name} found in {filePath}",
                                SeverityLevel.Error);
                            result.failed.Add(mmConfigFile);
                            continue;
                        }

                        queueNames.Add(queueConfig.Name.ToString());
                        mmConfigFile.Name = queueConfig.Name.ToString();

                        break;
                    case IMatchmakerConfigParser.EnvironmentConfigExtension:
                        mmConfigFile.Content = JsonConvert.DeserializeObject<EnvironmentConfig>(fileText, JsonSerializerSettings);
                        if (environmentConfigPresent)
                        {
                            mmConfigFile.Status = new DeploymentStatus($"Multiple environment config files found", $"Multiple environment config files found in {filePath}", SeverityLevel.Error);
                            result.failed.Add(mmConfigFile);
                            continue;
                        }
                        environmentConfigPresent = true;
                        mmConfigFile.Name = "EnvironmentConfig";
                        break;
                }

                if (mmConfigFile.Content == null)
                {
                    // This can only happen for empty file. Other case either work or throw exception
                    mmConfigFile.Status = new DeploymentStatus($"Invalid matchmaker config file", "Is the file empty ?", SeverityLevel.Error);
                    result.failed.Add(mmConfigFile);
                    continue;
                }
                result.parsed.Add(mmConfigFile);
            }
            catch (JsonSerializationException ex)
            {
                mmConfigFile.Status = new DeploymentStatus($"Invalid values in file {filePath}", $"Invalid values in {filePath} at line {ex.LineNumber}: {ex.Message}", SeverityLevel.Error);
                result.failed.Add(mmConfigFile);
            }
            catch (JsonException ex)
            {
                mmConfigFile.Status = new DeploymentStatus($"Invalid json in file {filePath}", $"Invalid json in file {filePath}: {ex.Message}", SeverityLevel.Error);
                result.failed.Add(mmConfigFile);
            }
        }

        return result;
    }

    public async Task<(bool, string)> SerializeToFile(IMatchmakerConfig config, string path, CancellationToken ct)
    {
        var targetJson = string.Empty;
        var originalJson = string.Empty;
        try
        {
            originalJson = await m_FileSystem.ReadAllText(path, ct);
        }
        catch (IOException)
        {
            // if no original file, just write the new one
        }

        switch (config)
        {
            case QueueConfig queueConfig:
                targetJson = JsonConvert.SerializeObject(queueConfig, JsonSerializerSettings);
                break;
            case EnvironmentConfig environmentConfig:
                targetJson = JsonConvert.SerializeObject(environmentConfig, JsonSerializerSettings);
                break;
        }

        targetJson += "\n"; // add newline at end of file

        try
        {
            if (targetJson == string.Empty || originalJson == targetJson)
                return (false, string.Empty);
            await m_FileSystem.WriteAllText(path, targetJson, ct);
        }
        catch (FileSystemException ex)
        {
            return (false, ex.ToString());
        }

        return (true, string.Empty);
    }

    public bool IsDeepEqual<T>(T source, T target)
    {
        if (source == null || target == null)
        {
            return source == null && target == null;
        }

        var sourceJson = JsonConvert.SerializeObject(source, JsonSerializerSettings);
        var targetJson = JsonConvert.SerializeObject(target, JsonSerializerSettings);
        return sourceJson == targetJson;
    }
}
