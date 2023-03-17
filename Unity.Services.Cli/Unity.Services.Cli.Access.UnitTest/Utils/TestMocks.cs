using Unity.Services.Gateway.AccessApiV1.Generated.Model;

namespace Unity.Services.Cli.Access.UnitTest.Utils;

public class TestMocks
{
    public static Statement GetStatement()
    {
        List<string> action = new List<string>();
        action.Add("*");

        Statement statement = new Statement(sid: "statement-1", action: action, effect: "Deny", principal: "Player",
            resource: "urn:ugs:*");
        return statement;
    }

    public static PlayerPolicy GetPlayerPolicy()
    {
        List<Statement> statements = new List<Statement>();
        statements.Add(GetStatement());

        PlayerPolicy playerPolicy = new PlayerPolicy(playerId: TestValues.ValidPlayerId, statements);
        return playerPolicy;
    }

    public static PlayerPolicies GetPlayerPolicies()
    {
        List<PlayerPolicy> playerPolicyList = new List<PlayerPolicy>();
        playerPolicyList.Add(GetPlayerPolicy());

        PlayerPolicies playerPolicies = new PlayerPolicies(next: null, results: playerPolicyList);

        return playerPolicies;
    }
}
