using Newtonsoft.Json;
using Unity.Services.Gateway.AccessApiV1.Generated.Model;

namespace Unity.Services.Cli.Access.Models;

internal class GetPolicyResponseOutput
{
    public Policy Policy { get; }

    public GetPolicyResponseOutput(Policy policy)
    {
        Policy = policy;
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this.Policy, Formatting.Indented);
    }
}
