namespace Unity.Services.Cli.CloudSave.Utils;

static class PlayerIndexVisibilityTypes
{
    public const string Default = "default";
    public const string Public = "public";
    public const string Protected = "protected";

    public static bool IsValidType(string? type)
    {
        return type is Default or Public or Protected;
    }

    public static IEnumerable<string> GetTypes()
    {
        return new List<string> { Default, Public, Protected };
    }
}
