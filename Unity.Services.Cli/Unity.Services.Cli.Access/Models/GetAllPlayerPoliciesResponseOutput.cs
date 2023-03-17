using Newtonsoft.Json;
using Unity.Services.Gateway.AccessApiV1.Generated.Model;

namespace Unity.Services.Cli.Access.Models;

internal class GetAllPlayerPoliciesResponseOutput
{
    public List<PlayerPolicy> Results { get; }

    public GetAllPlayerPoliciesResponseOutput(List<PlayerPolicy> results)
    {
        Results = results;
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this.Results, Formatting.Indented);
    }
}
