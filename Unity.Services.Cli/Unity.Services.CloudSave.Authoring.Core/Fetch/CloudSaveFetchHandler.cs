using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.CloudSave.Authoring.Core.Deploy;
using Unity.Services.CloudSave.Authoring.Core.IO;
using Unity.Services.CloudSave.Authoring.Core.Model;
using Unity.Services.CloudSave.Authoring.Core.Service;
using Unity.Services.CloudSave.Authoring.Core.Validations;

namespace Unity.Services.CloudSave.Authoring.Core.Fetch
{
    public class CloudSaveFetchHandler : CloudSaveFetchDeployBase, ICloudSaveFetchHandler
    {
        readonly ICloudSaveSimpleResourceLoader m_ResourceLoader;

        public CloudSaveFetchHandler(
            ICloudSaveClient client,
            ICloudSaveSimpleResourceLoader resourceLoader)
            : base(client)
        {
            m_ResourceLoader = resourceLoader;
        }

        public async Task<FetchResult> FetchAsync(
            string rootDirectory,
            IReadOnlyList<IResourceDeploymentItem> localResources,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default)
        {
            localResources.ToList().ForEach(l => l.Progress = 0f);

            var filteredLocalResources = DuplicateResourceValidation.FilterDuplicateResources(
                localResources, out var duplicateGroups);

            UpdateDuplicateResourceStatus(duplicateGroups);

            var remoteResources = await GetRemoteItems(rootDirectory, token);

            SetupMaps(filteredLocalResources, remoteResources);

            var toUpdate = filteredLocalResources
                .Where(ExistsRemotely)
                .ToList();

            var toDelete = filteredLocalResources
                .Where(DoesNotExistRemotely)
                .ToList();

            var toCreate = new List<IResourceDeploymentItem>();
            if (reconcile)
            {
                toCreate = remoteResources
                    .Where(DoesNotExistLocally)
                    .ToList();
            }

            var res = new FetchResult
            {
                Fetched = localResources.Concat(toCreate).ToList()
            };

            if (dryRun)
            {
                UpdateDryRunResult(toUpdate, toDelete, toCreate);
                return res;
            }

            filteredLocalResources.ForEach(l => l.Progress = 50);

            var updateTasks = CreateOrUpdateResources(toUpdate, token);

            var deleteTasks = DeleteResources(toDelete, token);

            var createTasks = new List<(IResourceDeploymentItem, Task)>();
            if (reconcile)
            {
                createTasks = CreateOrUpdateResources(toCreate, token);
            }

            await WaitForTasks(updateTasks, Constants.Updated);
            await WaitForTasks(deleteTasks, Constants.Deleted);
            await WaitForTasks(createTasks, Constants.Created);

            return res;
        }

        static void UpdateDryRunResult(List<IResourceDeploymentItem> toUpdate, List<IResourceDeploymentItem> toDelete, List<IResourceDeploymentItem> toCreate)
        {
            foreach (var i in toUpdate)
            {
                i.Status = Statuses.GetFetched(Constants.Updated);
            }

            foreach (var i in toDelete)
            {
                i.Status = Statuses.GetFetched(Constants.Deleted);
            }

            foreach (var i in toCreate)
            {
                i.Status = Statuses.GetFetched(Constants.Created);
            }
        }

        List<(IResourceDeploymentItem, Task)> CreateOrUpdateResources(List<IResourceDeploymentItem> toUpdate, CancellationToken token)
        {
            List<(IResourceDeploymentItem, Task)> updateTasks = new List<(IResourceDeploymentItem, Task)>();
            foreach (var item in toUpdate)
            {
                item.Resource = GetRemoteResourceItem(item.Resource.Id).Resource;
                var task = m_ResourceLoader.CreateOrUpdateResource(item, token);
                updateTasks.Add((item, task));
            }

            return updateTasks;
        }

        List<(IResourceDeploymentItem, Task)> DeleteResources(List<IResourceDeploymentItem> toDelete, CancellationToken token)
        {
            List<(IResourceDeploymentItem, Task)> deleteTasks = new List<(IResourceDeploymentItem, Task)>();
            foreach (var resource in toDelete)
            {
                var task = m_ResourceLoader.DeleteResource(
                    resource,
                    token);
                deleteTasks.Add((resource, task));
            }

            return deleteTasks;
        }

        static async Task WaitForTasks(
            List<(IResourceDeploymentItem, Task)> tasks,
            string taskAction)
        {
            foreach (var (resource, task) in tasks)
            {
                try
                {
                    await task;
                    resource.Progress = 100f;
                    resource.Status = Statuses.GetFetched(taskAction);
                }
                catch (Exception e)
                {
                    resource.Status = Statuses.GetFailedToFetch(e.Message);
                }
            }
        }
    }
}
