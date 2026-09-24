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
    public class CompoundModuleTemplateDeploymentHandler : ModuleTemplateFetchDeployBase, ICompoundModuleTemplateDeploymentHandler
    {
        public CompoundModuleTemplateDeploymentHandler(IModuleTemplateClient client)
            : base(client) { }

        public async Task<DeployResult> DeployAsync(
            IReadOnlyList<CompoundResourceDeploymentItem> compoundLocalResources,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default)
        {
            var res = new DeployResult();

            var localResources = compoundLocalResources
                .SelectMany(c => c.Items)
                .ToList();

            var cmpdLocalResources = compoundLocalResources.ToList();

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

            res.Deployed = cmpdLocalResources.Cast<IDeploymentItem>().Concat(toDelete).ToList();

            if (dryRun)
            {
                UpdateDryRunResult(toUpdate, toDelete, toCreate, compoundLocalResources);
                return res;
            }

            cmpdLocalResources.ForEach(i => i.Progress = 50);
            filteredLocalResources.ForEach(l => l.Progress = 50);

            var createTasks = GetTasks(toCreate, Client.Create, Constants.Created, token);
            var updateTasks = GetTasks(toUpdate, Client.Update, Constants.Updated, token);
            var deleteTasks = reconcile
                ? GetTasks(toDelete, Client.Delete, Constants.Deleted, token)
                : Enumerable.Empty<Func<Task>>();

            var allTasks = createTasks.Concat(updateTasks).Concat(deleteTasks);

            await Batching.Batching.ExecuteInBatchesAsync(allTasks, token);

            UpdateCompoundItemStatus(compoundLocalResources);

            return res;
        }

        static IEnumerable<Func<Task>> GetTasks(
            IReadOnlyList<SimpleResourceDeploymentItem> resources,
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
            message = $"{message}\n{GetNestedDetails(failedItems)}";
            return Statuses.GetFailedToDeploy(message);
        }

        protected override SimpleResourceDeploymentItem CreateItem(string rootDirectory, SimpleResource resource)
        {
            return new NestedResourceDeploymentItem("Remote", resource);
        }
    }
}
