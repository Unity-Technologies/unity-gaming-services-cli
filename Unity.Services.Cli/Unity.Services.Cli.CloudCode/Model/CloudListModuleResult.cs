using System.Globalization;

namespace Unity.Services.Cli.CloudCode.Model;

public record CloudListModuleResult(string Name, DateTime? DateModified)
{
    public override string ToString()
    {
        return $"{Name} -  Date Modified: {DateModified?.ToString("s", CultureInfo.InvariantCulture) ?? "Never"}";
    }
}
