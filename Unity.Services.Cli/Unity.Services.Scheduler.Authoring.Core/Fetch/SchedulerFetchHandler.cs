using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Scheduler.Authoring.Core.IO;
using Unity.Services.Scheduler.Authoring.Core.Model;
using Unity.Services.Scheduler.Authoring.Core.Serialization;
using Unity.Services.Scheduler.Authoring.Core.Service;
using Unity.Services.Scheduler.Authoring.Core.Validations;

namespace Unity.Services.Scheduler.Authoring.Core.Fetch
{
    public class SchedulerFetchHandler : IScheduleFetchHandler
    {
        readonly ISchedulerClient m_Client;
        readonly IFileSystem m_FileSystem;
        readonly ISchedulesSerializer m_ScheduleSerializer;

        public SchedulerFetchHandler(
            ISchedulerClient client,
            IFileSystem fileSystem,
            ISchedulesSerializer scheduleSerializer)
        {
            m_Client = client;
            m_FileSystem = fileSystem;
            m_ScheduleSerializer = scheduleSerializer;
        }

        public async Task<FetchResult> FetchAsync(string rootDirectory,
            IReadOnlyList<IScheduleConfig> localResources,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default)
        {
            var res = new FetchResult();

            localResources = DuplicateResourceValidation.FilterDuplicateResources(
                localResources, out var duplicateGroups);

            var remoteResources = await m_Client.List();

            var toUpdate = localResources
                .Intersect(remoteResources, new ScheduleComparer())
                .ToList();

            var toDelete = localResources
                .Except(remoteResources, new ScheduleComparer())
                .ToList();

            var toCreate = new List<IScheduleConfig>();
            if (reconcile)
            {
                toCreate = remoteResources
                    .Except(localResources, new ScheduleComparer())
                    .ToList();
                toCreate.ForEach(r => r.Path = Path.Combine(rootDirectory, r.Name) + ".sched");
            }

            res.Created = toCreate;
            res.Deleted = toDelete;
            res.Updated = toUpdate;
            res.Fetched = new List<IScheduleConfig>();
            res.Failed = new List<IScheduleConfig>();

            UpdateDuplicateResourceStatus(res, duplicateGroups);

            if (dryRun)
            {
                return res;
            }

            var updateTasks = new List<(IScheduleConfig, Task)>();
            var deleteTasks = new List<(IScheduleConfig, Task)>();
            var createTasks = new List<(IScheduleConfig, Task)>();

            var updatedFiles = toUpdate.GroupBy(r => r.Path);
            foreach (var file in updatedFiles)
            {
                var task = m_FileSystem.WriteAllText(
                    file.Key,
                    m_ScheduleSerializer.Serialize(remoteResources.Intersect(file.ToList(), new ScheduleComparer()).ToList()),
                    token);
                file.ToList().ForEach(f => updateTasks.Add((f, task)));
            }

            var filesToDelete = toDelete.GroupBy(t => t.Path)
                    .ExceptBy(updatedFiles.Select(f => f.Key),
                        g => g.Key);
            foreach (var file in filesToDelete)
            {
                var task = m_FileSystem.Delete(
                    file.Key,
                    token);
                file.ToList().ForEach(f => deleteTasks.Add((f, task)));
            }

            if (reconcile)
            {
                foreach (var resource in toCreate)
                {
                    var task = m_FileSystem.WriteAllText(
                        resource.Path,
                        m_ScheduleSerializer.Serialize(new List<IScheduleConfig>() { resource }),
                        token);
                    createTasks.Add((resource, task));
                }
            }

            await UpdateResult(updateTasks, res, "Updated");
            await UpdateResult(deleteTasks, res, "Deleted");
            await UpdateResult(createTasks, res, "Created");

            return res;
        }

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
            FetchResult result,
            IReadOnlyList<IGrouping<string, IScheduleConfig>> duplicateGroups)
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
            List<(IScheduleConfig, Task)> tasks,
            FetchResult res,
            string detail)
        {
            foreach (var (resource, task) in tasks)
            {
                try
                {
                    await task;
                    res.Fetched.Add(resource);
                    UpdateStatus(resource, Statuses.GetFetched(detail));
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
