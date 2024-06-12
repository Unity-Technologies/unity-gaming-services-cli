using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.CloudSave.Authoring.Core.Model;

namespace Unity.Services.CloudSave.Authoring.Core.Validations
{
    static class DuplicateResourceValidation
    {
        public static List<IResourceDeploymentItem> FilterDuplicateResources(
            IReadOnlyList<IResourceDeploymentItem> resources,
            out IReadOnlyList<IGrouping<string, IResourceDeploymentItem>> duplicateGroups)
        {
            //TODO: Revisit this to use name, or whatever ID is appropriate for your implementation
            duplicateGroups = resources
                .GroupBy(r => r.Resource.Id)
                .Where(g => g.Count() > 1)
                .ToList();

            var hashset = new HashSet<string>(duplicateGroups.Select(g => g.Key));

            return resources
                .Where(r => !hashset.Contains(r.Resource.Id))
                .ToList();
        }

        public static (string, string) GetDuplicateResourceErrorMessages(
            IResourceDeploymentItem targetResource,
            IReadOnlyList<IResourceDeploymentItem> group)
        {
            var duplicates = group
                .Except(new[] { targetResource })
                .ToList();

            var duplicatesStr = string.Join(", ", duplicates.Select(d => $"'{d.Path}'"));
            var shortMessage = $"'{targetResource.Path}' was found duplicated in other files: {duplicatesStr}";
            var message = $"Multiple resources with the same identifier '{targetResource.Resource.Id}' were found. "
                      + "Only a single resource for a given identifier may be deployed/fetched at the same time. "
                      + "Give all resources unique identifiers or deploy/fetch them separately to proceed.\n"
                      + shortMessage;
            return (shortMessage, message);
        }
    }
}
