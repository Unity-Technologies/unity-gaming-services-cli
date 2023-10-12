using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Triggers.Authoring.Core.Model;
using Unity.Services.Triggers.Authoring.Core.Service;
using Unity.Services.Triggers.Authoring.Core.Validations;

namespace Unity.Services.Triggers.Authoring.Core.Deploy
{
    public class TriggersDeploymentHandler : ITriggersDeploymentHandler
    {
        readonly ITriggersClient m_Client;
        readonly object m_ResultLock = new();

        public TriggersDeploymentHandler(ITriggersClient client)
        {
            m_Client = client;
        }

        public async Task<DeployResult> DeployAsync(
            IReadOnlyList<ITriggerConfig> localResources,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default)
        {
            var res = new DeployResult();

            localResources = DuplicateResourceValidation.FilterDuplicateResources(
                localResources, out var duplicateGroups);

            var remoteResources = await m_Client.List();

            var toCreate = localResources
                .Except(remoteResources, new TriggerComparer())
                .ToList();

            var toUpdate = localResources
                .Except(toCreate, new TriggerComparer())
                .ToList();

            var toDelete = new List<ITriggerConfig>();
            if (reconcile)
            {
                toDelete = remoteResources
                    .Except(localResources, new TriggerComparer())
                    .ToList();
            }

            res.Created = toCreate;
            res.Deleted = toDelete;
            res.Updated = toUpdate;
            res.Deployed = new List<ITriggerConfig>();
            res.Failed = new List<ITriggerConfig>();

            UpdateDuplicateResourceStatus(res, duplicateGroups);
            FindIdsForTriggers(toUpdate, remoteResources, toDelete);
            
            if (dryRun)
            {
                return res;
            }

            var createTasks = GetTasks(toCreate, m_Client.Create, res);
            var updateTasks = GetTasks(toUpdate, m_Client.Update, res);
            var deleteTasks = reconcile
                ? GetTasks(toDelete, m_Client.Delete, res)
                : new List<Task>();

            var allTasks = createTasks.Concat(updateTasks).Concat(deleteTasks);

            await Batching.Batching.ExecuteInBatchesAsync(allTasks, token);

            return res;
        }

        static void FindIdsForTriggers(List<ITriggerConfig> toUpdate, IReadOnlyList<ITriggerConfig> remoteResources, List<ITriggerConfig> toDelete)
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
        IEnumerable<Task> GetTasks(List<ITriggerConfig> resources, Func<ITriggerConfig, Task> func, DeployResult res)
            => resources.Select(i => DeployResource(func, i, res));

        protected virtual void UpdateStatus(
            ITriggerConfig triggerConfig,
            DeploymentStatus status)
        {
            // clients can override this to provide user feedback on progress
            triggerConfig.Status = status;
        }

        protected virtual void UpdateProgress(
            ITriggerConfig triggerConfig,
            float progress)
        {
            // clients can override this to provide user feedback on progress
            triggerConfig.Progress = progress;
        }

        void UpdateDuplicateResourceStatus(
            DeployResult result,
            IReadOnlyList<IGrouping<string, ITriggerConfig>> duplicateGroups)
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
            Func<ITriggerConfig, Task> task,
            ITriggerConfig triggerConfig,
            DeployResult res)
        {
            try
            {
                await task(triggerConfig);
                lock (m_ResultLock)
                    res.Deployed.Add(triggerConfig);
                UpdateStatus(triggerConfig, Statuses.Deployed);
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
