using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.ModuleTemplate.Authoring.Core.Model;
using Unity.Services.ModuleTemplate.Authoring.Core.Service;

namespace Unity.Services.ModuleTemplate.Authoring.Core.Deploy
{
    public class ModuleTemplateDeploymentHandler : ModuleTemplateFetchDeployBase, IModuleTemplateDeploymentHandler
    {
        public ModuleTemplateDeploymentHandler(IModuleTemplateClient client)
            : base(client) { }

        public async Task<DeployResult> DeployAsync(
            IReadOnlyList<SimpleResourceDeploymentItem> localResources,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default)
        {
            var res = new DeployResult();

            var filteredLocalResources = FilterInvalidItems(localResources);

            var remoteResources = await GetRemoteItems(cancellationToken: token);

            SetupMaps(filteredLocalResources, remoteResources);

            var toCreate = filteredLocalResources
                .Where(DoesNotExistRemotely)
                .ToList();

            var toUpdate = filteredLocalResources
                .Where(ExistsRemotely)
                .ToList();

            var toDelete = new List<SimpleResourceDeploymentItem>();
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
                : Enumerable.Empty<Func<Task>>();

            var allTasks = createTasks.Concat(updateTasks).Concat(deleteTasks);

            await Batching.Batching.ExecuteInBatchesAsync(allTasks, token);

            return res;
        }

        static IEnumerable<Func<Task>> GetTasks(
            List<SimpleResourceDeploymentItem> resources,
            Func<SimpleResource, CancellationToken, Task> func,
            string taskAction,
            CancellationToken token)
        {
            return resources.Select(i => (Func<Task>)(() => DeployResource(func, i, taskAction, token)));
        }

        protected override DeploymentStatus GetSuccessStatus(string message)
        {
            return Statuses.GetDeployed(message);
        }

        protected override DeploymentStatus GetFailedStatus(string message, IReadOnlyList<IDeploymentItem> failedItems = null)
        {
            return Statuses.GetFailedToDeploy(message);
        }
    }
}
