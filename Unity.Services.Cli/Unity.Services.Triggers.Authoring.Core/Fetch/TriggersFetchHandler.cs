using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Triggers.Authoring.Core.IO;
using Unity.Services.Triggers.Authoring.Core.Model;
using Unity.Services.Triggers.Authoring.Core.Serialization;
using Unity.Services.Triggers.Authoring.Core.Service;
using Unity.Services.Triggers.Authoring.Core.Validations;

namespace Unity.Services.Triggers.Authoring.Core.Fetch
{
    public class TriggersFetchHandler : ITriggersFetchHandler
    {
        readonly ITriggersClient m_Client;
        readonly IFileSystem m_FileSystem;
        readonly ITriggersSerializer m_TriggersSerializer;

        public TriggersFetchHandler(
            ITriggersClient client,
            IFileSystem fileSystem,
            ITriggersSerializer triggersSerializer)
        {
            m_Client = client;
            m_FileSystem = fileSystem;
            m_TriggersSerializer = triggersSerializer;
        }

        public async Task<FetchResult> FetchAsync(string rootDirectory,
            IReadOnlyList<ITriggerConfig> localResources,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default)
        {
            var res = new FetchResult();

            localResources = DuplicateResourceValidation.FilterDuplicateResources(
                localResources, out var duplicateGroups);

            var remoteResources = await m_Client.List();

            var toUpdate = localResources
                .Intersect(remoteResources, new TriggerComparer())
                .ToList();

            var toDelete = localResources
                .Except(remoteResources, new TriggerComparer())
                .ToList();

            var toCreate = new List<ITriggerConfig>();
            if (reconcile)
            {
                toCreate = remoteResources
                    .Except(localResources, new TriggerComparer())
                    .ToList();
                toCreate.ForEach(r => ((TriggerConfig)r).Path = Path.Combine(rootDirectory, r.Name) + ".tr");
            }

            res.Created = toCreate;
            res.Deleted = toDelete;
            res.Updated = toUpdate;
            res.Fetched = new List<ITriggerConfig>();
            res.Failed = new List<ITriggerConfig>();

            UpdateDuplicateResourceStatus(res, duplicateGroups);

            if (dryRun)
            {
                return res;
            }

            var updateTasks = new List<(ITriggerConfig, Task)>();
            var deleteTasks = new List<(ITriggerConfig, Task)>();
            var createTasks = new List<(ITriggerConfig, Task)>();

            var updatedFiles = toUpdate.GroupBy(r => r.Path).ToList();
            foreach (var file in updatedFiles)
            {
                var task = m_FileSystem.WriteAllText(
                    file.Key,
                    m_TriggersSerializer.Serialize(remoteResources.Intersect(file.ToList(), new TriggerComparer()).ToList()),
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
                        m_TriggersSerializer.Serialize(new List<ITriggerConfig>(){ resource }),
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
            FetchResult result,
            IReadOnlyList<IGrouping<string, ITriggerConfig>> duplicateGroups)
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
            List<(ITriggerConfig, Task)> tasks,
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
