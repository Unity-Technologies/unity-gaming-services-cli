using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.ModuleTemplate.Authoring.Core.Deploy;
using Unity.Services.ModuleTemplate.Authoring.Core.IO;
using Unity.Services.ModuleTemplate.Authoring.Core.Model;
using Unity.Services.ModuleTemplate.Authoring.Core.Service;
using Unity.Services.ModuleTemplate.Authoring.Core.Validations;

namespace Unity.Services.ModuleTemplate.Authoring.Core.Fetch
{
    public class CompoundModuleTemplateFetchHandler : ModuleTemplateFetchDeployBase, ICompoundModuleTemplateFetchHandler
    {
        internal const string FetchResultName = "fetched_resources" + Constants.CompoundFileExtension;
        readonly IModuleTemplateCompoundResourceLoader m_ResourceLoader;

        public CompoundModuleTemplateFetchHandler(
            IModuleTemplateClient client,
            IModuleTemplateCompoundResourceLoader resourceLoader)
            : base(client)
        {
            m_ResourceLoader = resourceLoader;
        }

        public async Task<FetchResult> FetchAsync(
            string rootDirectory,
            IReadOnlyList<ICompoundResourceDeploymentItem> compoundLocalResources,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default)
        {
            compoundLocalResources.ToList().ForEach(l => l.Progress = 0f);

            var localResources = compoundLocalResources
                .SelectMany(c => c.Items)
                .ToList();

            var filteredLocalResources = DuplicateResourceValidation.FilterDuplicateResources(
                localResources, out var duplicateGroups);

            UpdateDuplicateResourceStatus(duplicateGroups);

            var remoteResources = (await GetRemoteItems(rootDirectory, token)).Cast<INestedResourceDeploymentItem>().ToList();

            SetupMaps(filteredLocalResources, remoteResources);

            var toUpdate = filteredLocalResources
                .Where(ExistsRemotely)
                .ToList();

            var toDelete = filteredLocalResources
                .Where(DoesNotExistRemotely)
                .ToList();

            var toCreate = new List<INestedResourceDeploymentItem>();
            if (reconcile)
            {
                toCreate = remoteResources
                    .Where(DoesNotExistLocally)
                    .ToList();
            }

            var (defaultFile, defaultFileCreated) = GetDefaultFile(
                rootDirectory,
                compoundLocalResources,
                toCreate);

            if (defaultFileCreated && toCreate.Count > 0)
            {
                compoundLocalResources = compoundLocalResources.Append(defaultFile).ToList();
            }

            var res = new FetchResult
            {
                Fetched = compoundLocalResources
            };

            if (dryRun)
            {
                UpdateCompoundDryRunResult(toUpdate, toDelete, toCreate, compoundLocalResources, defaultFileCreated);
                return res;
            }

            // Modify the state in-memory. if the file was just created, it has been populated.
            if (!defaultFileCreated)
                CreateLocal(toCreate);
            UpdateLocal(toUpdate);
            DeleteLocal(toDelete);

            var (toUpdateFiles, toDeleteFiles, toCreateFiles)
                = GetAffectedResources(compoundLocalResources, toCreate, toDelete);

            filteredLocalResources.ForEach(l => l.Progress = 50);

            var createTasks = UpdateOrCreateResources(toCreateFiles, token);
            var updateTasks = UpdateOrCreateResources(toUpdateFiles, token);
            var deleteTasks = DeleteResources(toDeleteFiles, token);

            await WaitForTasks(createTasks, Constants.Created);
            await WaitForTasks(updateTasks, Constants.Updated);
            await WaitForTasks(deleteTasks, Constants.Deleted);

            UpdateCompoundItemsStatus(compoundLocalResources, toCreate, toUpdate, toDelete, defaultFileCreated);
            return res;
        }

        static (ICompoundResourceDeploymentItem, bool) GetDefaultFile(
            string rootDir,
            IReadOnlyList<ICompoundResourceDeploymentItem> compoundLocalResources,
            IReadOnlyList<INestedResourceDeploymentItem> toCreate)
        {
            var defaultFile = compoundLocalResources
                .FirstOrDefault(f => f.Name == FetchResultName);
            var created = defaultFile == null;
            if (created)
            {
                // If it does not exist, there are no updates/deletes to be done
                defaultFile = new CompoundResourceDeploymentItem(Path.Combine(rootDir, FetchResultName))
                {
                    Items = toCreate.ToList()
                };
            }

            foreach (var nestedResourceDeploymentItem in toCreate)
            {
                nestedResourceDeploymentItem.Parent = defaultFile;
            }

            return (defaultFile, created);
        }

        static (List<ICompoundResourceDeploymentItem>, List<ICompoundResourceDeploymentItem>, List<ICompoundResourceDeploymentItem>)
            GetAffectedResources(
                IReadOnlyList<ICompoundResourceDeploymentItem> files,
                IReadOnlyList<INestedResourceDeploymentItem> toCreate,
                IReadOnlyList<INestedResourceDeploymentItem> toDelete)
        {
            var toCreateFiles = toCreate.Select(i => i.Parent).Distinct().ToList();
            var otherFiles = files.Except(toCreateFiles).ToList();
            var toUpdateFiles = new List<ICompoundResourceDeploymentItem>();
            var toDeleteFiles = new List<ICompoundResourceDeploymentItem>();
            foreach (var file in otherFiles)
            {
                //Avoid re-writting files where all resources failed to be written
                var allFailed = file.Items.All(r => r.Status.MessageSeverity == SeverityLevel.Error);
                if (allFailed)
                    continue;

                var deletedResourceCount = file.Items.Count(toDelete.Contains);
                if (deletedResourceCount == file.Items.Count)
                    toDeleteFiles.Add(file);
                else
                    toUpdateFiles.Add(file);
            }

            return (toUpdateFiles, toDeleteFiles, toCreateFiles);
        }

        void CreateLocal(
            IReadOnlyList<INestedResourceDeploymentItem> toCreate)
        {
            foreach (var entry in toCreate)
            {
                entry.Parent.Items.Add(
                    (INestedResourceDeploymentItem) GetRemoteResourceItem(entry.Resource.Id));
            }
        }

        void UpdateLocal(
            IReadOnlyList<INestedResourceDeploymentItem> toUpdate)
        {
            foreach (var entry in toUpdate)
            {
                var entryIx = entry.Parent.Items.IndexOf(entry);
                entry.Parent.Items[entryIx].Resource =
                    GetRemoteResourceItem(entry.Resource.Id).Resource;
            }
        }

        static void DeleteLocal(
            IReadOnlyList<INestedResourceDeploymentItem> toDelete)
        {
            foreach (var entry in toDelete)
            {
                var entryIx = entry.Parent.Items.IndexOf(entry);
                //Marking null is marking for deletion.
                //This allows the output to still be comprehensive (tell the user the item was deleted)
                entry.Parent.Items[entryIx].Resource = null;
            }
        }

        List<(ICompoundResourceDeploymentItem, Task)> UpdateOrCreateResources(
            List<ICompoundResourceDeploymentItem> filesToWrite,
            CancellationToken token)
        {
            List<(ICompoundResourceDeploymentItem, Task)> updateTasks = new List<(ICompoundResourceDeploymentItem, Task)>();
            foreach (var item in filesToWrite)
            {
                var task = m_ResourceLoader.CreateOrUpdateResource(item, token);
                updateTasks.Add((item, task));
            }

            return updateTasks;
        }

        List<(ICompoundResourceDeploymentItem, Task)> DeleteResources(List<ICompoundResourceDeploymentItem> toDelete, CancellationToken token)
        {
            List<(ICompoundResourceDeploymentItem, Task)> deleteTasks = new List<(ICompoundResourceDeploymentItem, Task)>();
            foreach (var resource in toDelete)
            {
                var task = m_ResourceLoader.DeleteResource(
                    resource,
                    token);
                deleteTasks.Add((resource, task));
            }

            return deleteTasks;
        }

        protected async Task WaitForTasks(
            List<(ICompoundResourceDeploymentItem, Task)> tasks,
            string taskAction)
        {
            foreach (var (resource, task) in tasks)
            {
                try
                {
                    await task;
                    resource.Progress = 100f;
                    resource.Status = GetSuccessStatus(taskAction);
                }
                catch (Exception e)
                {
                    resource.Status = GetFailedStatus(e.Message);
                }
            }
        }

        protected void UpdateCompoundDryRunResult(
            IReadOnlyList<INestedResourceDeploymentItem> toUpdate,
            IReadOnlyList<INestedResourceDeploymentItem> toDelete,
            IReadOnlyList<INestedResourceDeploymentItem> toCreate,
            IReadOnlyList<ICompoundResourceDeploymentItem> localCompoundItems,
            bool defaultFileCreated)
        {
            base.UpdateDryRunResult(
                toUpdate,
                toDelete,
                toCreate,
                null);

            //Make a copy of the resource list to not modify the internal state during a "dry-run"
            var fileToList = localCompoundItems.ToDictionary(
                k => k,
                k => k.Items.ToList());

            foreach (var deletedItem in toDelete)
                fileToList[deletedItem.Parent].Remove(deletedItem);
            foreach (var createdItem in toCreate)
                fileToList[createdItem.Parent].Add(createdItem);

            var createdFiles = defaultFileCreated
                    ? toCreate
                        .Select(i => i.Parent)
                        .Distinct()
                        .ToList()
                    : new List<ICompoundResourceDeploymentItem>();

            // Need to update the default file for added items, there's no better alternative
            if (!defaultFileCreated)
            {
                CreateLocal(toCreate);
            }

            foreach (var item in localCompoundItems)
            {
                var fileFutureItems = fileToList[item];
                var itemCount = fileFutureItems.Count;
                UpdateCompoundItemStatus(item, itemCount, createdFiles);
            }
        }

        protected void UpdateCompoundItemsStatus(
            IReadOnlyList<ICompoundResourceDeploymentItem> localCompoundItems,
            IReadOnlyList<INestedResourceDeploymentItem> toCreate,
            IReadOnlyList<INestedResourceDeploymentItem> toUpdate,
            IReadOnlyList<INestedResourceDeploymentItem> toDelete,
            bool defaultFileWasCreated)
        {
            base.UpdateDryRunResult(toUpdate, toDelete, toCreate);
            var createdFiles = defaultFileWasCreated
                ? toCreate
                    .Select(i => i.Parent)
                    .Distinct()
                    .ToList()
                : new List<ICompoundResourceDeploymentItem>();

            foreach (var item in localCompoundItems)
            {
                var itemCount = item.Items.Except(toDelete).Count();
                UpdateCompoundItemStatus(item, itemCount, createdFiles);
            }
        }

        void UpdateCompoundItemStatus(
            ICompoundResourceDeploymentItem item,
            int itemCount,
            List<ICompoundResourceDeploymentItem> createdFiles)
        {
            var message = createdFiles.Contains(item) ? Constants.Created : Constants.Updated;
            var failedItemCount =
                item.Items.Count(nested => nested.Status.MessageSeverity != SeverityLevel.Success);

            if (itemCount == 0)
                item.Status = GetSuccessStatus(
                    Constants.Deleted + "; All items were deleted, as they no longer exist remotely.");
            else if (failedItemCount == 0)
                item.Status = GetSuccessStatus(message + "; All items were successfully fetched");
            else if (failedItemCount != item.Items.Count)
                item.Status = GetPartialStatus();
            else //futureItemCount > 0 && failedItem == futureItemCount
                item.Status = GetFailedStatus("No items were fetched");
        }

        protected override IResourceDeploymentItem CreateItem(string rootDirectory, IResource resource)
        {
            return new NestedResourceDeploymentItem(Path.Combine(rootDirectory,FetchResultName), resource);
        }

        protected override DeploymentStatus GetSuccessStatus(string message)
        {
            return Statuses.GetFetched(message);
        }

        protected override DeploymentStatus GetFailedStatus(string message)
        {
            return Statuses.GetFailedToFetch(message);
        }

        protected override DeploymentStatus GetPartialStatus(string message = null)
        {
            return Statuses.GetPartialFetch(message);
        }
    }
}
