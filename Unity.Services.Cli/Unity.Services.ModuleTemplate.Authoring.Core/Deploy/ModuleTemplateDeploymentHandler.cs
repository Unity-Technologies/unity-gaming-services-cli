using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.ModuleTemplate.Authoring.Core.Model;
using Unity.Services.ModuleTemplate.Authoring.Core.Service;
using Unity.Services.ModuleTemplate.Authoring.Core.Validations;

namespace Unity.Services.ModuleTemplate.Authoring.Core.Deploy
{
    public class ModuleTemplateDeploymentHandler : IModuleTemplateDeploymentHandler
    {
        readonly IModuleTemplateClient m_Client;
        readonly object m_ResultLock = new();

        public ModuleTemplateDeploymentHandler(IModuleTemplateClient client)
        {
            m_Client = client;
        }

        public async Task<DeployResult> DeployAsync(
            IReadOnlyList<IResource> localResources,
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

            var toDelete = new List<IResource>();
            if (reconcile)
            {
                toDelete = remoteResources
                    .Except(localResources)
                    .ToList();
            }

            res.Created = toCreate;
            res.Deleted = toDelete;
            res.Updated = toUpdate;
            res.Deployed = new List<IResource>();
            res.Failed = new List<IResource>();

            UpdateDuplicateResourceStatus(res, duplicateGroups, dryRun);

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

        // TODO: Add support for CancellationToken in m_Client.Create, m_Client.Update, m_Client.Delete
        IEnumerable<Task> GetTasks(List<IResource> resources, Func<IResource, Task> func, DeployResult res)
            => resources.Select(i => DeployResource(func, i, res));

        protected virtual void UpdateStatus(
            IResource resource,
            DeploymentStatus status)
        {
            // clients can override this to provide user feedback on progress
            resource.Status = status;
        }

        protected virtual void UpdateProgress(
            IResource resource,
            float progress)
        {
            // clients can override this to provide user feedback on progress
            resource.Progress = progress;
        }

        void UpdateDuplicateResourceStatus(
            DeployResult result,
            IReadOnlyList<IGrouping<string, IResource>> duplicateGroups,
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

        async Task DeployResource(
            Func<IResource, Task> task,
            IResource resource,
            DeployResult res)
        {
            try
            {
                await task(resource);
                lock (m_ResultLock)
                    res.Deployed.Add(resource);
                UpdateStatus(resource, Statuses.Deployed);
                UpdateProgress(resource, 100);
            }
            catch (Exception e)
            {
                lock (m_ResultLock)
                    res.Failed.Add(resource);
                UpdateStatus(resource, Statuses.GetFailedToDeploy(e.Message));
            }
        }
    }
}
