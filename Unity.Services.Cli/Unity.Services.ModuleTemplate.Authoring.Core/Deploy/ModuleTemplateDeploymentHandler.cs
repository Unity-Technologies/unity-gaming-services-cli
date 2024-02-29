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
    public class ModuleTemplateDeploymentHandler : ModuleTemplateFetchDeployBase, IModuleTemplateDeploymentHandler
    {
        public ModuleTemplateDeploymentHandler(IModuleTemplateClient client)
            : base(client) { }

        public async Task<DeployResult> DeployAsync(
            IReadOnlyList<IResourceDeploymentItem> localResources,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default)
        {
            var res = new DeployResult();

            var filteredLocalResources = DuplicateResourceValidation.FilterDuplicateResources(
                localResources, out var duplicateGroups);

            UpdateDuplicateResourceStatus(duplicateGroups);

            var remoteResources = await GetRemoteItems(cancellationToken: token);

            SetupMaps(filteredLocalResources, remoteResources);

            var toCreate = filteredLocalResources
                .Where(DoesNotExistRemotely)
                .ToList();

            var toUpdate = filteredLocalResources
                .Where(ExistsRemotely)
                .ToList();

            var toDelete = new List<IResourceDeploymentItem>();
            if (reconcile)
            {
                toDelete = remoteResources
                    .Where(DoesNotExistLocally)
                    .ToList();
            }

            res.Deployed = localResources.Concat(toDelete).ToList();

            if (dryRun)
            {
                UpdateDryRunResult(toUpdate, toDelete, toCreate);
                return res;
            }

            filteredLocalResources.ForEach(l => l.Progress = 50);

            var createTasks = GetTasks(toCreate, Client.Create, Constants.Created, token);
            var updateTasks = GetTasks(toUpdate, Client.Update, Constants.Updated, token);
            var deleteTasks = reconcile
                ? GetTasks(toDelete, Client.Delete, Constants.Deleted, token)
                : new List<Task>();

            var allTasks = createTasks.Concat(updateTasks).Concat(deleteTasks);

            await Batching.Batching.ExecuteInBatchesAsync(allTasks, token);

            return res;
        }

        static IEnumerable<Task> GetTasks(
            List<IResourceDeploymentItem> resources,
            Func<IResource, CancellationToken, Task> func,
            string taskAction,
            CancellationToken token)
        {
            return resources.Select(i => DeployResource(func, i, taskAction, token));
        }

        static async Task DeployResource(
            Func<IResource, CancellationToken, Task> task,
            IResourceDeploymentItem resource,
            string taskAction,
            CancellationToken token)
        {
            try
            {
                resource.Status = Statuses.GetDeploying();
                await task(resource.Resource, token);
                resource.Status = Statuses.GetDeployed(taskAction);
                resource.Progress = 100f;
            }
            catch (Exception e)
            {
                resource.Status = Statuses.GetFailedToDeploy(e.Message);
            }
        }

        protected override DeploymentStatus GetSuccessStatus(string message)
        {
            return Statuses.GetDeployed(message);
        }

        protected override DeploymentStatus GetFailedStatus(string message)
        {
            return Statuses.GetFailedToDeploy(message);
        }
    }
}
