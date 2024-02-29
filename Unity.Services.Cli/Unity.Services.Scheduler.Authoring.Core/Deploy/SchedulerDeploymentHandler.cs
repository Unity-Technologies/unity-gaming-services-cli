using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Scheduler.Authoring.Core.Model;
using Unity.Services.Scheduler.Authoring.Core.Service;
using Unity.Services.Scheduler.Authoring.Core.Validations;

namespace Unity.Services.Scheduler.Authoring.Core.Deploy
{
    public class SchedulerDeploymentHandler : IScheduleDeploymentHandler
    {
        readonly ISchedulerClient m_Client;
        readonly object m_ResultLock = new();

        public SchedulerDeploymentHandler(ISchedulerClient client)
        {
            m_Client = client;
        }

        public async Task<DeployResult> DeployAsync(
            IReadOnlyList<IScheduleConfig> localResources,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default)
        {
            var res = new DeployResult();

            localResources = DuplicateResourceValidation.FilterDuplicateResources(
                localResources, out var duplicateGroups);

            var remoteResources = await m_Client.List();

            var toCreate = localResources
                .Except(remoteResources, new ScheduleComparer())
                .ToList();

            var toUpdate = localResources
                .Except(toCreate, new ScheduleComparer())
                .ToList();

            var toDelete = new List<IScheduleConfig>();
            if (reconcile)
            {
                toDelete = remoteResources
                    .Except(localResources, new ScheduleComparer())
                    .ToList();
            }

            res.Created = toCreate;
            res.Deleted = toDelete;
            res.Updated = toUpdate;
            res.Deployed = new List<IScheduleConfig>();
            res.Failed = new List<IScheduleConfig>();

            UpdateDuplicateResourceStatus(res, duplicateGroups);
            FindIdsForSchedules(toUpdate, remoteResources, toDelete);

            if (dryRun)
            {
                return res;
            }

            var createTasks = GetTasks(toCreate, m_Client.Create, res, "Created");
            var updateTasks = GetTasks(toUpdate, m_Client.Update, res, "Updated");
            var deleteTasks = reconcile
                ? GetTasks(toDelete, m_Client.Delete, res, "Deleted")
                : new List<Task>();

            var allTasks = createTasks.Concat(updateTasks).Concat(deleteTasks);

            await Batching.Batching.ExecuteInBatchesAsync(allTasks, token);

            return res;
        }

        static void FindIdsForSchedules(
            List<IScheduleConfig> toUpdate,
            IReadOnlyList<IScheduleConfig> remoteResources,
            List<IScheduleConfig> toDelete)
        {
            foreach (var trigger in toUpdate)
            {
                trigger.Id = remoteResources.First(t => t.Name == trigger.Name).Id;
            }

            foreach (var trigger in toDelete)
            {
                trigger.Id = remoteResources.First(t => t.Name == trigger.Name).Id;
            }
        }

        // TODO: Add support for CancellationToken in m_Client.Create, m_Client.Update, m_Client.Delete
        IEnumerable<Task> GetTasks(List<IScheduleConfig> resources, Func<IScheduleConfig, Task> func, DeployResult res, string detail)
            => resources.Select(i => DeployResource(func, i, res, detail));

        protected virtual void UpdateStatus(
            IScheduleConfig triggerConfig,
            DeploymentStatus status)
        {
            // clients can override this to provide user feedback on progress
            triggerConfig.Status = status;
        }

        protected virtual void UpdateProgress(
            IScheduleConfig triggerConfig,
            float progress)
        {
            // clients can override this to provide user feedback on progress
            triggerConfig.Progress = progress;
        }

        void UpdateDuplicateResourceStatus(
            DeployResult result,
            IReadOnlyList<IGrouping<string, IScheduleConfig>> duplicateGroups)
        {
            foreach (var group in duplicateGroups)
            {
                foreach (var res in group)
                {
                    result.Failed.Add(res);
                    result.Created.Remove(res);
                    result.Updated.Remove(res);
                    var (message, shortMessage) = DuplicateResourceValidation.GetDuplicateResourceErrorMessages(res, group.ToList());
                    UpdateStatus(res, Statuses.GetFailedToDeploy(shortMessage));
                }
            }
        }

        async Task DeployResource(
            Func<IScheduleConfig, Task> task,
            IScheduleConfig triggerConfig,
            DeployResult res,
            string detail)
        {
            try
            {
                await task(triggerConfig);
                lock (m_ResultLock)
                    res.Deployed.Add(triggerConfig);
                UpdateStatus(triggerConfig, Statuses.GetDeployed(detail));
                UpdateProgress(triggerConfig, 100);
            }
            catch (Exception e)
            {
                lock (m_ResultLock)
                    res.Failed.Add(triggerConfig);
                UpdateStatus(triggerConfig, Statuses.GetFailedToDeploy(e.Message));
            }
        }
    }
}
