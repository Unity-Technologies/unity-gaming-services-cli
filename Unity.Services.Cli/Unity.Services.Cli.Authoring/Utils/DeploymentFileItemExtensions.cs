using Unity.Services.Cli.Authoring.Model;
namespace Unity.Services.Cli.Authoring.Utils;

/// <summary>
/// Extension methods for <see cref="AuthoringFile"/>.
/// </summary>
public static class AuthoringFileExtensions
{
    /// <summary>
    /// Converts a read-only list of <see cref="AuthoringFile"/> to a read-only list of file paths.
    /// </summary>
    /// <param name="items">The collection of deployment file items.</param>
    /// <returns>A read-only list containing only the file paths from the items.</returns>
    public static IReadOnlyList<string> ToPaths(this IReadOnlyList<AuthoringFile> items)
    {
        return items.Select(item => item.Path).ToList();
    }
}
