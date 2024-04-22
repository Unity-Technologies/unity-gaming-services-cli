using Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.Model;
using Unity.Services.Gateway.AccessApiV1.Generated.Model;

namespace Unity.Services.Cli.Access.UnitTest.Utils;

class TestMocks
{
    public static Statement GetStatement(string sid = "statement-1")
    {
        List<string> action = new List<string>();
        action.Add("*");

        Statement statement = new Statement(sid: sid, action: action, effect: "Deny", principal: "Player",
            resource: "urn:ugs:*");
        return statement;
    }

    public static Policy GetPolicy(List<Statement> statements)
    {
        var policy = new Policy(statements);
        return policy;
    }

    public static AccessControlStatement GetAuthoringStatement(
        string sid = "statement-1",
        List<string>? action = null,
        string effect = "Deny",
        string principal = "Player",
        string resource = "urn:ugs:*"
        )
    {
        action ??= new List<string>()
        {
            "*"
        };
        AccessControlStatement statement = new AccessControlStatement
        {
            Sid = sid,
            Action = action,
            Effect = effect,
            Principal = principal,
            Resource = resource
        };

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

    public static DeleteOptions GetDeleteOptions(List<string> statementIDs)
    {
        var deleteOptions = new DeleteOptions(statementIDs);
        return deleteOptions;
    }

    public static ProjectAccessFile GetProjectAccessFile(string path, List<AccessControlStatement> statements)
    {
        return new ProjectAccessFile()
        {
            Path = path,
            Name = Path.GetFileName(path),
            Statements = statements
        };
    }
}
