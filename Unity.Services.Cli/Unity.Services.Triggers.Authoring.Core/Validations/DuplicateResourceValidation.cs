using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Triggers.Authoring.Core.Model;

namespace Unity.Services.Triggers.Authoring.Core.Validations
{
    public static class DuplicateResourceValidation
    {
        public static IReadOnlyList<ITriggerConfig> FilterDuplicateResources(
            IReadOnlyList<ITriggerConfig> resources,
            out IReadOnlyList<IGrouping<string, ITriggerConfig>> duplicateGroups)
        {
            duplicateGroups = resources
                .GroupBy(r => r.Name)
                .Where(g => g.Count() > 1)
                .ToList();

            var hashset = new HashSet<string>(duplicateGroups.Select(g => g.Key));

            return resources
                .Where(r => !hashset.Contains(r.Name))
                .ToList();
        }

        public static (string, string) GetDuplicateResourceErrorMessages(
            ITriggerConfig targetTriggerConfig,
            IReadOnlyList<ITriggerConfig> group)
        {
            var duplicates = group
                .Except(new[] { targetTriggerConfig })
                .ToList();

            var duplicatesStr = string.Join(", ", duplicates.Select(d => $"'{d.Path}'"));
            var shortMessage = $"'{targetTriggerConfig.Path}' was found duplicated in other files: {duplicatesStr}";
            var message = $"Multiple resources with the same identifier '{targetTriggerConfig.Name}' were found. "
                      + "Only a single resource for a given identifier may be deployed/fetched at the same time. "
                      + "Give all resources unique identifiers or deploy/fetch them separately to proceed.\n"
                      + shortMessage;
            return (shortMessage, message);
        }
    }
}
