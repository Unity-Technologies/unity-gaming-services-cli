using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.CloudSave.Authoring.Core.Model;
using Unity.Services.CloudSave.Authoring.Core.Service;
using Unity.Services.CloudSave.Authoring.Core.Validations;

namespace Unity.Services.CloudSave.Authoring.Core.Deploy
{
    public class CloudSaveFetchDeployBase
    {
        IReadOnlyDictionary<string,IResourceDeploymentItem> m_LocalMap;
        IReadOnlyDictionary<string,IResourceDeploymentItem> m_RemoteMap;
        protected ICloudSaveClient Client { get; }

        public CloudSaveFetchDeployBase(ICloudSaveClient client)
        {
            Client = client;
        }

        protected void SetupMaps(List<IResourceDeploymentItem> filteredLocalResources, IReadOnlyList<IResourceDeploymentItem> remoteResources)
        {
            //TODO: Verify the right nomenclature for your ID here, or use `Name`
            m_LocalMap = filteredLocalResources.ToDictionary(l => l.Resource.Id, l => l);
            m_RemoteMap = remoteResources.ToDictionary(l => l.Resource.Id, l => l);
        }

        protected async Task<IReadOnlyList<IResourceDeploymentItem>> GetRemoteItems(
            string rootDirectory = null,
            CancellationToken cancellationToken = default)
        {
            var remoteResources = await Client.List(cancellationToken);
            var remoteItems = remoteResources
                .Select(
                    resource =>
                    {
                        var path = rootDirectory != null
                            ? Path.Combine(rootDirectory, resource.Id + Constants.SimpleFileExtension)
                            : "Remote";
                        var deploymentItem = new SimpleResourceDeploymentItem(resource.Id, path)
                        {
                            Resource = resource
                        };
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

        protected static void UpdateDuplicateResourceStatus(
            IReadOnlyList<IGrouping<string, IResourceDeploymentItem>> duplicateGroups)
        {
            foreach (var group in duplicateGroups)
            {
                foreach (var resourceItem in group)
                {
                    var (message, shortMessage) = DuplicateResourceValidation.GetDuplicateResourceErrorMessages(resourceItem, group.ToList());
                    resourceItem.Status = Statuses.GetFailedToFetch(shortMessage);
                }
            }
        }

    }
}
