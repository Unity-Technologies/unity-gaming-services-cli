using System.Text;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;

namespace Unity.Services.Cli.CloudCode.Authoring;

static class ScriptDuplicateUtils
{
    public static IReadOnlyList<IScript> FilterDuplicates(
        this IReadOnlyList<IScript> resources,
        out Dictionary<string, List<IScript>> duplicateGroups,
        out List<IScript> duplicates)
    {
        duplicateGroups = resources.GroupBy(x => x.Name.ToString())
            .Where(x => x.Count() > 1)
            .ToDictionary(x => x.Key, x => x.ToList());
        duplicates = duplicateGroups.SelectMany(x => x.Value)
            .ToList();

        return resources.Except(duplicates)
            .ToList();
    }

    public static string GetDuplicatesMessage(this IReadOnlyDictionary<string, List<IScript>> duplicateGroups)
    {
        const string errorMessage = "Multiple resources with the same identifiers were found. "
            + "Only a single resource for a given identifier may be deployed/fetched at the same time. "
            + "Give all resources unique identifiers or deploy/fetch them separately to proceed.";
        var builder = new StringBuilder(errorMessage)
            .AppendLine();
        foreach (var (key, resources) in duplicateGroups)
        {
            builder.AppendLine($"    Duplicates for \"{key}\" were found:");
            foreach (var resource in resources)
            {
                builder.AppendLine($"        \"{resource.Path}\"");
            }
        }

        return builder.ToString();
    }
}
