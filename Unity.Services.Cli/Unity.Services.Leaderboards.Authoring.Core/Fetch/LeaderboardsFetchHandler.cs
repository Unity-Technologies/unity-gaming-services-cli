using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Leaderboards.Authoring.Core.Deploy;
using Unity.Services.Leaderboards.Authoring.Core.IO;
using Unity.Services.Leaderboards.Authoring.Core.Model;
using Unity.Services.Leaderboards.Authoring.Core.Serialization;
using Unity.Services.Leaderboards.Authoring.Core.Service;
using Unity.Services.Leaderboards.Authoring.Core.Validations;

namespace Unity.Services.Leaderboards.Authoring.Core.Fetch
{
    public class LeaderboardsFetchHandler : ILeaderboardsFetchHandler
    {
        readonly ILeaderboardsClient m_Client;
        readonly IFileSystem m_FileSystem;
        readonly ILeaderboardsSerializer m_LeaderboardsSerializer;

        public LeaderboardsFetchHandler(
            ILeaderboardsClient client,
            IFileSystem fileSystem,
            ILeaderboardsSerializer leaderboardsSerializer)
        {
            m_Client = client;
            m_FileSystem = fileSystem;
            m_LeaderboardsSerializer = leaderboardsSerializer;
        }

        public async Task<FetchResult> FetchAsync(
            string rootDirectory,
            IReadOnlyList<ILeaderboardConfig> localResources,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default)
        {
            var res = new FetchResult();

            localResources = DuplicateResourceValidation.FilterDuplicateResources(
                localResources, out var duplicateGroups);

            var remoteResources = await m_Client.List(token);

            var toUpdate = localResources
                .Intersect(remoteResources, new LeaderboardComparer())
                .ToList();

            var toDelete = localResources
                .Except(remoteResources, new LeaderboardComparer())
                .ToList();

            var toCreate = new List<ILeaderboardConfig>();
            if (reconcile)
            {
                toCreate = remoteResources
                    .Except(localResources, new LeaderboardComparer())
                    .ToList();
                toCreate.ForEach(r => ((LeaderboardConfig)r).Path = Path.Combine(rootDirectory, r.Id) + ".lb" );
            }

            res.Created = toCreate;
            res.Deleted = toDelete;
            res.Updated = toUpdate;
            res.Fetched = new List<ILeaderboardConfig>();
            res.Failed = new List<ILeaderboardConfig>();

            UpdateDuplicateResourceStatus(res, duplicateGroups);

            if (dryRun)
            {
                return res;
            }

            var updateTasks = new List<(ILeaderboardConfig, Task)>();
            var deleteTasks = new List<(ILeaderboardConfig, Task)>();
            var createTasks = new List<(ILeaderboardConfig, Task)>();

            foreach (var resource in toUpdate)
            {
                var task = m_FileSystem.WriteAllText(
                    resource.Path,
                    m_LeaderboardsSerializer.Serialize(resource),
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
                        m_LeaderboardsSerializer.Serialize(resource),
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
            FetchResult result,
            IReadOnlyList<IGrouping<string, ILeaderboardConfig>> duplicateGroups)
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
            List<(ILeaderboardConfig, Task)> tasks,
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
