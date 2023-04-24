using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.Gateway.PlayerAdminApiV3.Generated.Model;

namespace Unity.Services.Cli.Player.Model;

public class PlayerListResponseResult
{
    public PlayerAuthListProjectUserResponse Players;

    public PlayerListResponseResult(PlayerAuthListProjectUserResponse players)
    {
        Players = players;
    }

    public override string ToString()
    {
        var jsonString = JsonConvert.SerializeObject(Players);
        var formattedJson = JToken.Parse(jsonString).ToString(Formatting.Indented);
        return formattedJson;
    }
}
