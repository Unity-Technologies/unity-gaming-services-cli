using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Unity.Services.Cli.Authoring.Templates;
using Unity.Services.Leaderboards.Authoring.Core.Model;

namespace Unity.Services.Cli.Leaderboards.Deploy;

[Serializable]
class LeaderboardConfigFile : IFileTemplate
{
    [JsonConstructor]
    public LeaderboardConfigFile(string name) : this(null, name, SortOrder.Asc, UpdateType.KeepBest)
    {

    }

    public LeaderboardConfigFile() : this(null, "My Leaderboard", SortOrder.Asc, UpdateType.KeepBest)
    {
        ResetConfig = new()
        {
            Start = DateTime.Today.AddDays(10).Date,
            Schedule = "0 12 1 * *"
        };
        TieringConfig = new TieringConfig()
        {
            Strategy = Strategy.Score,
            Tiers = new List<Tier>()
            {
                new (){ Cutoff = 200.0, Id = "Gold"},
                new (){ Cutoff = 100, Id = "Silver"},
                new (){ Id = "Bronze"},
            }
        };
    }

    public LeaderboardConfigFile(string? id, string name, SortOrder sortOrder, UpdateType updateType)
    {
        Id = id;
        Name = name;
        SortOrder = sortOrder;
        UpdateType = updateType;
    }

    public SortOrder SortOrder { get; set; }
    public UpdateType UpdateType { get; set; }

    /// <summary>
    /// Defaults to file-name if unspecified
    /// </summary>
    public string? Id { get; set; }
    public string Name { get; set; }
    public decimal BucketSize { get; set; }
    public ResetConfig? ResetConfig { get; set; }
    public TieringConfig? TieringConfig { get; set; }

    [JsonProperty("$schema")]
    public string Value = "https://ugs-config-schemas.unity3d.com/v1/leaderboards.schema.json";

    [JsonIgnore]
    public string Extension => ".lb";

    [JsonIgnore]
    public string FileBodyText => JsonConvert.SerializeObject(this, GetSerializationSettings());

    public static JsonSerializerSettings GetSerializationSettings()
    {
        var settings = new JsonSerializerSettings()
        {
            Converters = { new StringEnumConverter() },
            Formatting = Formatting.Indented,
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
        };
        return settings;
    }
}
