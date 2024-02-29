using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        IReadOnlyDictionary<string,IResourceDeploymentItem> m_LocalMap;
        IReadOnlyDictionary<string,IResourceDeploymentItem> m_RemoteMap;
        protected IModuleTemplateClient Client { get; }

        protected ModuleTemplateFetchDeployBase(IModuleTemplateClient client)
        {
            Client = client;
        }

        protected void SetupMaps(IReadOnlyList<IResourceDeploymentItem> filteredLocalResources, IReadOnlyList<IResourceDeploymentItem> remoteResources)
        {
            //TODO: Verify the right nomenclature for your ID here, or use `Name`
            m_LocalMap = filteredLocalResources.ToDictionary(l => l.Resource.Id, l => l);
            m_RemoteMap = remoteResources.ToDictionary(l => l.Resource.Id, l => l);
        }

        protected async Task<IReadOnlyList<IResourceDeploymentItem>> GetRemoteItems(
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

        protected bool ExistsRemotely(IResourceDeploymentItem resource)
        {
            return m_RemoteMap.ContainsKey(resource.Resource.Id);
        }

        protected bool DoesNotExistRemotely(IResourceDeploymentItem resource)
        {
            return !m_RemoteMap.ContainsKey(resource.Resource.Id);
        }

        protected bool DoesNotExistLocally(IResourceDeploymentItem resource)
        {
            return !m_LocalMap.ContainsKey(resource.Resource.Id);
        }

        protected IResourceDeploymentItem GetRemoteResourceItem(string id)
        {
            return m_RemoteMap[id];
        }

        protected void UpdateDuplicateResourceStatus(
            IReadOnlyList<IGrouping<string, IResourceDeploymentItem>> duplicateGroups)
        {
            foreach (var group in duplicateGroups)
            {
                foreach (var resourceItem in group)
                {
                    var (message, shortMessage) = DuplicateResourceValidation.GetDuplicateResourceErrorMessages(resourceItem, group.ToList());
                    resourceItem.Status = GetFailedStatus(shortMessage);
                }
            }
        }

        protected virtual IResourceDeploymentItem CreateItem(string rootDirectory, IResource resource)
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
            IReadOnlyList<IResourceDeploymentItem> toUpdate,
            IReadOnlyList<IResourceDeploymentItem> toDelete,
            IReadOnlyList<IResourceDeploymentItem> toCreate,
            IReadOnlyList<ICompoundResourceDeploymentItem> localCompoundItems = null)
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

        protected void UpdateCompoundItemStatus(IReadOnlyList<ICompoundResourceDeploymentItem> localCompoundItems)
        {
            foreach (var item in localCompoundItems)
            {
                var failedItemCount =
                    item.Items.Count(nested => nested.Status.MessageSeverity != SeverityLevel.Success);

                if (failedItemCount == 0)
                {
                    item.Status = GetSuccessStatus("All items were successfully deployed");
                    item.Progress = 100f;
                }
                else if (failedItemCount != item.Items.Count)
                {
                    item.Status = GetPartialStatus();
                }
                else
                {
                    item.Status = GetFailedStatus("No items were deployed");
                }
            }
        }

        protected abstract DeploymentStatus GetSuccessStatus(string message);

        protected abstract DeploymentStatus GetFailedStatus(string message);

        protected virtual DeploymentStatus GetPartialStatus(string message = null)
        {
            return Statuses.GetPartialDeploy(message);
        }
    }
}
