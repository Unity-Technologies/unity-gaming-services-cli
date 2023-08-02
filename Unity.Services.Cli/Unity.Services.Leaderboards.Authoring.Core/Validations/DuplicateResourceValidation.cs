using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Leaderboards.Authoring.Core.Model;

namespace Unity.Services.Leaderboards.Authoring.Core.Validations
{
    public static class DuplicateResourceValidation
    {
        public static IReadOnlyList<ILeaderboardConfig> FilterDuplicateResources(
            IReadOnlyList<ILeaderboardConfig> resources,
            out IReadOnlyList<IGrouping<string, ILeaderboardConfig>> duplicateGroups)
        {
            duplicateGroups = resources
                .GroupBy(r => r.Id)
                .Where(g => g.Count() > 1)
                .ToList();

            var hashset = new HashSet<string>(duplicateGroups.Select(g => g.Key));

            return resources
                .Where(r => !hashset.Contains(r.Id))
                .ToList();
        }

        public static (string, string) GetDuplicateResourceErrorMessages(
            ILeaderboardConfig targetLeaderboardConfig,
            IReadOnlyList<ILeaderboardConfig> group)
        {
            var duplicates = group
                .Except(new[] { targetLeaderboardConfig })
                .ToList();

            var duplicatesStr = string.Join(", ", duplicates.Select(d => $"'{d.Path}'"));
            var shortMessage = $"'{targetLeaderboardConfig.Path}' was found duplicated in other files: {duplicatesStr}";
            var message = $"Multiple resources with the same identifier '{targetLeaderboardConfig.Id}' were found. "
                      + "Only a single resource for a given identifier may be deployed/fetched at the same time. "
                      + "Give all resources unique identifiers or deploy/fetch them separately to proceed.\n"
                      + shortMessage;
            return (shortMessage, message);
        }
    }
}
