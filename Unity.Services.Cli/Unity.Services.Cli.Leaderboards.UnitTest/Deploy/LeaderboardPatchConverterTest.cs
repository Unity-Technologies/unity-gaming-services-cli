using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Services.Cli.Leaderboards.Deploy;
using Unity.Services.Gateway.LeaderboardApiV1.Generated.Model;
using TieringConfig = Unity.Services.Gateway.LeaderboardApiV1.Generated.Model.TieringConfig;

namespace Unity.Services.Cli.Leaderboards.UnitTest.Deploy;

[TestFixture]
public class LeaderboardPatchConverterTest
{

    [Test]
    public void Converter_SerializesNullPropsToEmptyObj()
    {
        var obj = new LeaderboardPatchConfig("name");

        var str = JsonConvert.SerializeObject(obj, new LeaderboardPatchConverter());

        var jObj = JsonConvert.DeserializeObject<JObject>(str)!;

        Assert.AreEqual(jObj[nameof(LeaderboardConfig.TieringConfig)]!.ToString(), (new JObject()).ToString());
    }

    [Test]
    public void Converter_SerializesNonNullPropsToRealObj()
    {
        var originalObj = new TieringConfig(
            TieringConfig.StrategyEnum.Rank,
            new List<TieringConfigTiersInner>
            {
                new ("one", 1)
            });

        var obj = new LeaderboardPatchConfig()
        {
            TieringConfig = originalObj,
            // ResetConfig is required bcs of the weird patch behavior by the service, causes the
            // class to be effectively asymmetrical. We can serialize it, but not deserialize it.....
            ResetConfig = new ResetConfig(DateTime.UnixEpoch)
        };

        var str = JsonConvert.SerializeObject(obj, new LeaderboardPatchConverter());

        var deserializeObject = JsonConvert.DeserializeObject<LeaderboardPatchConfig>(str)!.TieringConfig;

        Assert.AreEqual(originalObj, deserializeObject);
    }
}
