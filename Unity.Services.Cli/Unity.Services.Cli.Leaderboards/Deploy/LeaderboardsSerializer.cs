using System.Runtime.Serialization;
using Newtonsoft.Json;
using Unity.Services.Leaderboards.Authoring.Core.Model;
using Unity.Services.Leaderboards.Authoring.Core.Serialization;

namespace Unity.Services.Cli.Leaderboards.Deploy;

class LeaderboardsSerializer : ILeaderboardsSerializer
{
    enum ErrorMessage
    {
        ParsingError,
        Error,
    }

    static JsonSerializerSettings s_DefaultSerializerSettings = new JsonSerializerSettings()
    {
        // this option is necessary to prevent default values to be set instead of
        // leaving them to null when absent from the json
        DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,

        // this is a workaround to make sure this throws an exception in case there
        // is extra characters at the end of the json
        CheckAdditionalContent = true,

        // we want it to fail if an unknown field is present in the json
        MissingMemberHandling = MissingMemberHandling.Error,
    };

    public LeaderboardConfig Deserialize(string path)
    {
        var lbcfg = new LeaderboardConfig(default);
        DeserializeAndPopulate(lbcfg);
        return lbcfg;
    }

    public void DeserializeAndPopulate(LeaderboardConfig config)
    {
        if (string.IsNullOrEmpty(config.Path))
        {
            throw new ArgumentNullException(nameof(config.Path), "impossible to deserialize an asset with empty path.");
        }
        if (config is null)
        {
            throw new ArgumentNullException(nameof(config));
        }
        try
        {
            var content = File.ReadAllText(config.Path);

            var id = Path.GetFileNameWithoutExtension(config.Path);

            // PopulateObject() will append already existing tiers if FromJson() called more than once
            // see https://www.newtonsoft.com/json/help/html/PopulateObject.htm
            config.TieringConfig?.Tiers?.Clear();

            JsonConvert.PopulateObject(content, config, s_DefaultSerializerSettings);

            // Overriding logic:
            // 1. if Name is not set in JSON, name is same as Id
            // 2. if Id is not set in JSON, Id is same as filename without extension
            config.Id ??= id;
            config.Name ??= config.Id;
        }
        catch (Exception e) when (e is SerializationException
                                      or JsonSerializationException
                                      or JsonReaderException)
        {
            throw new LeaderboardsDeserializeException(ErrorMessage.ParsingError.ToString(), e.Message, e);
        }
        catch (Exception e)
        {
            throw new LeaderboardsDeserializeException(ErrorMessage.Error.ToString(), e.Message, e);
        }
    }

    public string Serialize(ILeaderboardConfig config)
    {
        var fileName = Path.GetFileNameWithoutExtension(config.Path);
        var leaderboardFile = new LeaderboardConfigFile(
            fileName == config.Id ? null : config.Id,
            config.Name,
            config.SortOrder,
            config.UpdateType)
        {
            BucketSize = config.BucketSize,
            ResetConfig = config.ResetConfig,
            TieringConfig = config.TieringConfig,
        };
        return leaderboardFile.FileBodyText;
    }
}

class LeaderboardsDeserializeException : Exception
{
    public string ErrorMessage;
    public string Details;

    public LeaderboardsDeserializeException(string message, string details, Exception exception)
        : base(message, exception)
    {
        ErrorMessage = message;
        Details = details;
    }
}
