using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Leaderboards.Authoring.Core.Model;
using Unity.Services.Leaderboards.Authoring.Core.Service;
using Unity.Services.Leaderboards.Authoring.Core.Validations;

namespace Unity.Services.Leaderboards.Authoring.Core.Deploy
{
    public class LeaderboardsDeploymentHandler : ILeaderboardsDeploymentHandler
    {
        readonly ILeaderboardsClient m_Client;
        readonly object m_ResultLock = new();

        public LeaderboardsDeploymentHandler(ILeaderboardsClient client)
        {
            m_Client = client;
        }

        public async Task<DeployResult> DeployAsync(
            IReadOnlyList<ILeaderboardConfig> localResources,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default)
        {
            var res = new DeployResult();

            localResources = DuplicateResourceValidation.FilterDuplicateResources(
                localResources, out var duplicateGroups);

            var remoteResources = await m_Client.List(token);

            var toCreate = localResources
                .Except(remoteResources, new LeaderboardComparer())
                .ToList();

            var toUpdate = localResources
                .Except(toCreate, new LeaderboardComparer())
                .ToList();

            var toDelete = new List<ILeaderboardConfig>();
            if (reconcile)
            {
                toDelete = remoteResources
                    .Except(localResources, new LeaderboardComparer())
                    .ToList();
            }

            res.Created = toCreate;
            res.Deleted = toDelete;
            res.Updated = toUpdate;
            res.Deployed = new List<ILeaderboardConfig>();
            res.Failed = new List<ILeaderboardConfig>();

            UpdateDuplicateResourceStatus(res, duplicateGroups);

            if (dryRun)
            {
                return res;
            }

            var createTasks = GetTasks(toCreate, m_Client.Create, res, token);
            var updateTasks = GetTasks(toUpdate, m_Client.Update, res, token);
            var deleteTasks = reconcile
                ? GetTasks(toDelete, m_Client.Delete, res, token)
                : new List<Task>();

            var allTasks = createTasks.Concat(updateTasks).Concat(deleteTasks);

            await Batching.Batching.ExecuteInBatchesAsync(allTasks, token);

            return res;
        }

        IEnumerable<Task> GetTasks(
            List<ILeaderboardConfig> resources,
            Func<ILeaderboardConfig, CancellationToken, Task> func,
            DeployResult res,
            CancellationToken token)
        {
            return resources.Select(i => DeployResource(func, i, res, token));
        }

        protected virtual void UpdateStatus(
            ILeaderboardConfig leaderboardConfig,
            DeploymentStatus status)
        {
            // clients can override this to provide user feedback on progress
            leaderboardConfig.Status = status;
        }

        protected virtual void UpdateProgress(
            ILeaderboardConfig leaderboardConfig,
            float progress)
        {
            // clients can override this to provide user feedback on progress
            leaderboardConfig.Progress = progress;
        }

        void UpdateDuplicateResourceStatus(
            DeployResult result,
            IReadOnlyList<IGrouping<string, ILeaderboardConfig>> duplicateGroups)
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

        async Task DeployResource(
            Func<ILeaderboardConfig, CancellationToken, Task> task,
            ILeaderboardConfig leaderboardConfig,
            DeployResult res,
            CancellationToken token)
        {
            try
            {
                await task(leaderboardConfig, token);
                lock (m_ResultLock)
                    res.Deployed.Add(leaderboardConfig);
                UpdateStatus(leaderboardConfig, Statuses.Deployed);
                UpdateProgress(leaderboardConfig, 100);
            }
            catch (Exception e)
            {
                lock (m_ResultLock)
                    res.Failed.Add(leaderboardConfig);
                UpdateStatus(leaderboardConfig, Statuses.GetFailedToDeploy(e.Message));
            }
        }
    }
}
