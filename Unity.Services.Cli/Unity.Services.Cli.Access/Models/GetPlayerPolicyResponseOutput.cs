using Newtonsoft.Json;
using Unity.Services.Gateway.AccessApiV1.Generated.Model;

namespace Unity.Services.Cli.Access.Models;

internal class GetPlayerPolicyResponseOutput
{
    public PlayerPolicy Policy { get; }

    public GetPlayerPolicyResponseOutput(PlayerPolicy policy)
    {
        Policy = policy;
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this.Policy, Formatting.Indented);
    }
}
