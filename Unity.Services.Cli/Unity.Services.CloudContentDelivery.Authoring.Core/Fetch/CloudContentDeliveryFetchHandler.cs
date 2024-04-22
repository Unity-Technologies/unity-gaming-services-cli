using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.CloudContentDelivery.Authoring.Core.IO;
using Unity.Services.CloudContentDelivery.Authoring.Core.Model;
using Unity.Services.CloudContentDelivery.Authoring.Core.Service;
using Unity.Services.CloudContentDelivery.Authoring.Core.Validations;

namespace Unity.Services.CloudContentDelivery.Authoring.Core.Fetch
{
    public class CloudContentDeliveryFetchHandler : ICloudContentDeliveryFetchHandler
    {
        readonly ICloudContentDeliveryClient m_Client;
        readonly IFileSystem m_FileSystem;

        public CloudContentDeliveryFetchHandler(
            ICloudContentDeliveryClient client,
            IFileSystem fileSystem)
        {
            m_Client = client;
            m_FileSystem = fileSystem;
        }

        public async Task<FetchResult> FetchAsync(string rootDirectory,
            IReadOnlyList<IBucket> localResources,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default)
        {
            var res = new FetchResult();

            localResources = DuplicateResourceValidation.FilterDuplicateResources(
                localResources, out var duplicateGroups);

            var remoteResources = await m_Client.List();

            var toUpdate = remoteResources
                .Intersect(localResources)
                .ToList();

            var toDelete = localResources
                .Except(remoteResources)
                .ToList();

            var toCreate = new List<IBucket>();
            if (reconcile)
            {
                toCreate = remoteResources
                    .Except(localResources)
                    .ToList();
            }

            res.Created = toCreate;
            res.Deleted = toDelete;
            res.Updated = toUpdate;
            res.Fetched = new List<IBucket>();
            res.Failed = new List<IBucket>();

            UpdateDuplicateResourceStatus(res, duplicateGroups, dryRun);

            if (dryRun)
            {
                return res;
            }

            var updateTasks = new List<(IBucket, Task)>();
            var deleteTasks = new List<(IBucket, Task)>();
            var createTasks = new List<(IBucket, Task)>();

            foreach (var resource in toUpdate)
            {
                var task = m_FileSystem.WriteAllText(
                    resource.Path,
                    res.ToString(),
                    token);
                updateTasks.Add((resource, task));
            }

            foreach (var resource in toDelete)
            {
                var task = m_FileSystem.Delete(
                    resource.Path,
                    token);
                deleteTasks.Add((resource, task));
            }

            if (reconcile)
            {
                foreach (var resource in toCreate)
                {
                    var task = m_FileSystem.WriteAllText(
                        resource.Path,
                        resource.Name,
                        token);
                    createTasks.Add((resource, task));
                }
            }

            await UpdateResult(updateTasks, res);
            await UpdateResult(deleteTasks, res);
            await UpdateResult(createTasks, res);

            return res;
        }

        protected virtual void UpdateStatus(
            IBucket bucket,
            DeploymentStatus status)
        {
            // clients can override this to provide user feedback on progress
            bucket.Status = status;
        }

        protected virtual void UpdateProgress(
            IBucket bucket,
            float progress)
        {
            // clients can override this to provide user feedback on progress
            bucket.Progress = progress;
        }

        void UpdateDuplicateResourceStatus(
            FetchResult result,
            IReadOnlyList<IGrouping<string, IBucket>> duplicateGroups,
            bool dryRun)
        {
            foreach (var group in duplicateGroups)
            {
                foreach (var res in group)
                {
                    result.Failed.Add(res);
                    var (message, shortMessage) = DuplicateResourceValidation.GetDuplicateResourceErrorMessages(res, group.ToList());
                    UpdateStatus(res, Statuses.GetFailedToFetch(shortMessage));
                }
            }
        }

        async Task UpdateResult(
            List<(IBucket, Task)> tasks,
            FetchResult res)
        {
            foreach (var (resource, task) in tasks)
            {
                try
                {
                    await task;
                    res.Fetched.Add(resource);
                    UpdateStatus(resource, Statuses.Fetched);
                    UpdateProgress(resource, 100);
                }
                catch (Exception e)
                {
                    res.Failed.Add(resource);
                    UpdateStatus(resource, Statuses.GetFailedToFetch(e.Message));
                }
            }
        }
    }
}
