using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.CloudContentDelivery.Authoring.Core.Model;
using Unity.Services.CloudContentDelivery.Authoring.Core.Service;
using Unity.Services.CloudContentDelivery.Authoring.Core.Validations;

namespace Unity.Services.CloudContentDelivery.Authoring.Core.Deploy
{
    public class CloudContentDeliveryDeploymentHandler : ICloudContentDeliveryDeploymentHandler
    {
        readonly ICloudContentDeliveryClient m_Client;

        public CloudContentDeliveryDeploymentHandler(ICloudContentDeliveryClient client)
        {
            m_Client = client;
        }

        public async Task<DeployResult> DeployAsync(
            IReadOnlyList<IBucket> localResources,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default)
        {
            var res = new DeployResult();

            localResources = DuplicateResourceValidation.FilterDuplicateResources(
                localResources, out var duplicateGroups);

            var remoteResources = await m_Client.List();

            var toCreate = localResources
                .Except(remoteResources)
                .ToList();

            var toUpdate = localResources
                .Except(toCreate)
                .ToList();

            var toDelete = new List<IBucket>();
            if (reconcile)
            {
                toDelete = remoteResources
                    .Except(localResources)
                    .ToList();
            }

            res.Created = toCreate;
            res.Deleted = toDelete;
            res.Updated = toUpdate;
            res.Deployed = new List<IBucket>();
            res.Failed = new List<IBucket>();

            UpdateDuplicateResourceStatus(res, duplicateGroups, dryRun);

            if (dryRun)
            {
                return res;
            }

            var createTasks = new List<(IBucket, Task)>();
            var updateTasks = new List<(IBucket, Task)>();
            var deleteTasks = new List<(IBucket, Task)>();

            foreach (var localResource in toCreate)
            {
                createTasks.Add((localResource, m_Client.Create(localResource)));
            }

            foreach (var resource in toUpdate)
            {
                updateTasks.Add((resource, m_Client.Update(resource)));
            }

            if (reconcile)
            {
                foreach (var resource in toDelete)
                {
                    deleteTasks.Add((resource, m_Client.Delete(resource)));
                }

            }

            await UpdateResult(createTasks, res);
            await UpdateResult(updateTasks, res);
            await UpdateResult(deleteTasks, res);

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
            DeployResult result,
            IReadOnlyList<IGrouping<string, IBucket>> duplicateGroups,
            bool dryRun)
        {
            foreach (var group in duplicateGroups)
            {
                foreach (var res in group)
                {
                    result.Failed.Add(res);
                    var (message, shortMessage) = DuplicateResourceValidation.GetDuplicateResourceErrorMessages(res, group.ToList());
                    UpdateStatus(res, Statuses.GetFailedToDeploy(shortMessage));
                }
            }
        }

        async Task UpdateResult(
            List<(IBucket, Task)> tasks,
            DeployResult res)
        {
            foreach (var (resource, task) in tasks)
            {
                try
                {
                    await task;
                    res.Deployed.Add(resource);
                    UpdateStatus(resource, Statuses.Deployed);
                    UpdateProgress(resource, 100);
                }
                catch (Exception e)
                {
                    res.Failed.Add(resource);

                    UpdateStatus(resource, Statuses.GetFailedToDeploy(e.Message));
                }
            }
        }
    }
}
