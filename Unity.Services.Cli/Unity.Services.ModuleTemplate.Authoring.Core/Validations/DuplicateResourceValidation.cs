using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.ModuleTemplate.Authoring.Core.Model;

namespace Unity.Services.ModuleTemplate.Authoring.Core.Validations
{
    static class DuplicateResourceValidation
    {
        public static IReadOnlyList<IResource> FilterDuplicateResources(
            IReadOnlyList<IResource> resources,
            out IReadOnlyList<IGrouping<string, IResource>> duplicateGroups)
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
            IResource targetResource,
            IReadOnlyList<IResource> group)
        {
            var duplicates = group
                .Except(new[] { targetResource })
                .ToList();

            var duplicatesStr = string.Join(", ", duplicates.Select(d => $"'{d.Path}'"));
            var shortMessage = $"'{targetResource.Path}' was found duplicated in other files: {duplicatesStr}";
            var message = $"Multiple resources with the same identifier '{targetResource.Id}' were found. "
                      + "Only a single resource for a given identifier may be deployed/fetched at the same time. "
                      + "Give all resources unique identifiers or deploy/fetch them separately to proceed.\n"
                      + shortMessage;
            return (shortMessage, message);
        }
    }
}
