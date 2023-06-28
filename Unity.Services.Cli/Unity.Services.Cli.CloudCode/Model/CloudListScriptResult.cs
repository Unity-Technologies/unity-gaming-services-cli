using System.Globalization;

namespace Unity.Services.Cli.CloudCode.Model;

public record CloudListScriptResult(string Name, DateTime? DatePublished)
{
    public override string ToString()
    {
        return $"{Name} -  Date Created: {DatePublished?.ToString("s", CultureInfo.InvariantCulture) ?? "Never"}";
    }
}
