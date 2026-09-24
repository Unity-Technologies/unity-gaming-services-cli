using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.ModuleTemplate.Authoring.Core.Model;
using Unity.Services.ModuleTemplate.Authoring.Core.Service;
using Unity.Services.ModuleTemplate.Authoring.Core.Validations;

namespace Unity.Services.ModuleTemplate.Authoring.Core.Deploy
{
    public abstract class ModuleTemplateFetchDeployBase
    {
        IReadOnlyDictionary<string, SimpleResourceDeploymentItem> m_LocalMap;
        IReadOnlyDictionary<string, SimpleResourceDeploymentItem> m_RemoteMap;
        protected IModuleTemplateClient Client { get; }

        protected ModuleTemplateFetchDeployBase(IModuleTemplateClient client)
        {
            Client = client;
        }

        protected List<T> FilterInvalidItems<T>(IReadOnlyList<T> localResources) where T : SimpleResourceDeploymentItem
        {
            var filteredLocalResources = localResources.Where(f =>
            {
                var valid = f.Validate();
                if (!valid)
                    f.Status = GetFailedStatus("Catalog item is invalid and will not be processed");
                return valid;
            }).ToList();

            filteredLocalResources = DuplicateResourceValidation.FilterDuplicateResources(
                filteredLocalResources, out var duplicateGroups);

            UpdateDuplicateResourceStatus(duplicateGroups);
            return filteredLocalResources;
        }

        protected void SetupMaps(IReadOnlyList<SimpleResourceDeploymentItem> filteredLocalResources, IReadOnlyList<SimpleResourceDeploymentItem> remoteResources)
        {
            //TODO: Verify the right nomenclature for your ID here, or use `Name`
            m_LocalMap = filteredLocalResources.ToDictionary(l => l.Resource.Id, l => l);
            m_RemoteMap = remoteResources.ToDictionary(l => l.Resource.Id, l => l);
        }

        protected async Task<IReadOnlyList<SimpleResourceDeploymentItem>> GetRemoteItems(
            string rootDirectory = null,
            CancellationToken cancellationToken = default)
        {
            // TODO: if you fail to get a remote resource during the list,
            // you must set the status accordingly.
            // We're operating under the assumption that List will either completely succeed or not
            // if you have to make multiple GET calls, update this method accordingly
            var remoteResources = await Client.List(cancellationToken);
            var remoteItems = remoteResources
                .Select(
                    resource =>
                    {
                        var deploymentItem = CreateItem(rootDirectory, resource);
                        return deploymentItem;
                    })
                .ToList();
            return remoteItems;
        }

        protected bool ExistsRemotely(SimpleResourceDeploymentItem resource)
        {
            return m_RemoteMap.ContainsKey(resource.Resource.Id);
        }

        protected bool DoesNotExistRemotely(SimpleResourceDeploymentItem resource)
        {
            return !m_RemoteMap.ContainsKey(resource.Resource.Id);
        }

        protected bool DoesNotExistLocally(SimpleResourceDeploymentItem resource)
        {
            return !m_LocalMap.ContainsKey(resource.Resource.Id);
        }

        protected SimpleResourceDeploymentItem GetRemoteResourceItem(string id)
        {
            return m_RemoteMap[id];
        }

        protected static async Task DeployResource(
            Func<SimpleResource, CancellationToken, Task> task,
            SimpleResourceDeploymentItem resource,
            string taskAction,
            CancellationToken token)
        {
            try
            {
                resource.Status = Statuses.GetDeploying();
                await task(resource.Resource, token);
                resource.Status = Statuses.GetDeployed(taskAction);
                resource.Progress = 100f;
            }
            catch (ClientException e)
            {
                resource.Status = Statuses.GetFailedToDeploy(e.Message);
            }
            catch (Exception e)
            {
                resource.Status = Statuses.GetFailedToDeploy(e.ToString());
            }
        }

        protected void UpdateDuplicateResourceStatus<T>(
            IReadOnlyList<IGrouping<string, T>> duplicateGroups) where T : SimpleResourceDeploymentItem
        {
            foreach (var group in duplicateGroups)
            {
                foreach (var resourceItem in group)
                {
                    var (message, shortMessage) = DuplicateResourceValidation.GetDuplicateResourceErrorMessages(resourceItem, group.Cast<SimpleResourceDeploymentItem>().ToList());
                    resourceItem.Status = GetFailedStatus(shortMessage);
                }
            }
        }

        protected virtual SimpleResourceDeploymentItem CreateItem(string rootDirectory, SimpleResource resource)
        {
            var path = rootDirectory != null
                ? Path.Combine(rootDirectory, resource.Id + Constants.SimpleFileExtension)
                : "Remote";
            return new SimpleResourceDeploymentItem(path)
            {
                Resource = resource
            };
        }

        protected virtual void UpdateDryRunResult(
            IReadOnlyList<SimpleResourceDeploymentItem> toUpdate,
            IReadOnlyList<SimpleResourceDeploymentItem> toDelete,
            IReadOnlyList<SimpleResourceDeploymentItem> toCreate,
            IReadOnlyList<CompoundResourceDeploymentItem> localCompoundItems = null)
        {
            foreach (var i in toUpdate)
            {
                i.Status = GetSuccessStatus(Constants.Updated);
            }

            foreach (var i in toDelete)
            {
                i.Status = GetSuccessStatus(Constants.Deleted);
            }

            foreach (var i in toCreate)
            {
                i.Status = GetSuccessStatus(Constants.Created);
            }

            if (localCompoundItems != null)
            {
                UpdateCompoundItemStatus(localCompoundItems);
            }
        }

        protected void UpdateCompoundItemStatus(IReadOnlyList<CompoundResourceDeploymentItem> localCompoundItems)
        {
            foreach (var item in localCompoundItems)
            {
                var failedItems = GetNestedFailedItems(item);

                if (failedItems.Count == 0)
                {
                    item.Status = GetSuccessStatus("All items were successfully deployed");
                    item.Progress = 100f;
                }
                else if (failedItems.Count != item.Items.Count)
                {
                    item.Status = GetPartialStatus("Some items were not deployed.", failedItems);
                }
                else
                {
                    item.Status = GetFailedStatus("No items were deployed.", failedItems);
                }
            }
        }

        protected abstract DeploymentStatus GetSuccessStatus(string message);

        protected abstract DeploymentStatus GetFailedStatus(string message, IReadOnlyList<IDeploymentItem> failedItems = null);

        protected virtual DeploymentStatus GetPartialStatus(string message, IReadOnlyList<IDeploymentItem> failedItems = null)
        {
            var str = GetNestedDetails(failedItems);
            return Statuses.GetPartialDeploy(str);
        }

        protected static string GetNestedDetails(IReadOnlyList<IDeploymentItem> failedItems)
        {
            if (failedItems == null || failedItems.Count == 0)
                return string.Empty;
            var strBuild = new StringBuilder();
            strBuild.AppendLine("Failed items:");
            foreach (var nested in failedItems)
            {
                var status = nested.Status.Message;
                string detail = "";
                if (!string.IsNullOrEmpty(nested.Status.MessageDetail))
                    detail = $" - {nested.Status.MessageDetail}";
                strBuild.AppendLine($"  - {nested.Name}: {status}{detail}");
            }

            return strBuild.ToString();
        }

        protected static List<NestedResourceDeploymentItem> GetNestedFailedItems(CompoundResourceDeploymentItem item)
        {
            return item.Items
                .Where(nested => nested.Status.MessageSeverity != SeverityLevel.Success)
                .ToList();
        }
    }
}
