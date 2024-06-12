namespace Unity.Services.Cli.CloudSave.Utils;

static class CustomIndexVisibilityTypes
{
    public const string Default = "default";
    public const string Private = "private";

    public static bool IsValidType(string? type)
    {
        return type is Default or Private;
    }

    public static IEnumerable<string> GetTypes()
    {
        return new List<string> { Default, Private };
    }
}
