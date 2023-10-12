using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Access.Authoring.Core.ErrorHandling;
using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Access.Authoring.Core.Model;
using Unity.Services.Access.Authoring.Core.Results;
using Unity.Services.Access.Authoring.Core.Service;
using Unity.Services.Access.Authoring.Core.Validations;

namespace Unity.Services.Access.Authoring.Core.Deploy
{
    public class ProjectAccessDeploymentHandler : IProjectAccessDeploymentHandler
    {
        readonly IProjectAccessClient m_ProjectAccessClient;
        readonly IProjectAccessConfigValidator m_ProjectAccessConfigValidator;
        readonly IProjectAccessMerger m_ProjectAccessMerger;

        public ProjectAccessDeploymentHandler(
            IProjectAccessClient projectAccessClient,
            IProjectAccessConfigValidator projectAccessConfigValidator,
            IProjectAccessMerger projectAccessMerger)
        {
            m_ProjectAccessClient = projectAccessClient;
            m_ProjectAccessConfigValidator = projectAccessConfigValidator;
            m_ProjectAccessMerger = projectAccessMerger;
        }

        public async Task<DeployResult> DeployAsync(
            IReadOnlyList<IProjectAccessFile> files,
            bool dryRun = false,
            bool reconcile = false)
        {
            var res = new DeployResult();

            if (!dryRun)
            {
                SetStartDeployingStatus(files);
            }

            var deploymentExceptions = new List<ProjectAccessPolicyDeploymentException>();

            var validProjectAccessFiles = files
                .Where((t, i) => m_ProjectAccessConfigValidator.Validate(t, deploymentExceptions))
                .ToList();

            var validLocalStatements = m_ProjectAccessConfigValidator.FilterNonDuplicatedAuthoringStatements(
                validProjectAccessFiles,
                deploymentExceptions);

            var serverProjectAccessPolicyResult = await GetServerProjectAccessPolicies(files, dryRun);
            var remoteStatements = serverProjectAccessPolicyResult ?? new List<AccessControlStatement>();

            var localSet = validLocalStatements.Select(l => l.Sid).ToHashSet();
            var remoteSet = remoteStatements.Select(l => l.Sid).ToHashSet();

            var toUpdate = FindStatementsToUpdate(remoteSet, validLocalStatements);
            var toDelete = FindStatementsToDelete(remoteStatements, localSet, reconcile);
            var toCreate = FindStatementsToCreate(remoteSet, validLocalStatements);

            var toDeploy = m_ProjectAccessMerger.MergeStatementsToDeploy(
                toCreate,
                toUpdate,
                toDelete,
                remoteStatements);

            var failedFiles = FindFailedFiles(deploymentExceptions);
            var filesToDeploy = FindFilesToDeploy(files, toDeploy);
            var deployedFiles = filesToDeploy.Except(failedFiles).ToList();

            res.Created = toCreate;
            res.Deleted = toDelete;
            res.Updated = toUpdate;
            res.Deployed = deployedFiles;
            res.Failed = failedFiles;

            if (dryRun)
            {
                SetStatuses(res);
                return res;
            }

            try
            {
                if (reconcile && toDelete.Count != 0)
                {
                    await m_ProjectAccessClient.DeleteAsync(toDelete.ToList());
                }

                await m_ProjectAccessClient.UpsertAsync(toDeploy.ToList());

                SetSuccessfulDeployStatuses(filesToDeploy, res, remoteStatements);
            }
            catch (ProjectAccessPolicyDeploymentException e)
            {
                deploymentExceptions.Add(e);
                e.AffectedFiles.AddRange(filesToDeploy);
                res.Failed = FindFailedFiles(deploymentExceptions);
                res.Deployed = FindFilesToDeploy(files, toDeploy).Except(res.Failed).ToList();
            }
            catch (Exception e)
            {
                SetFailedStatus(filesToDeploy, DeploymentStatus.FailedToDeploy.Message, e.Message);
                res.Failed = filesToDeploy;
                res.Deployed = Array.Empty<IProjectAccessFile>();
            }

            HandleDeploymentException(deploymentExceptions);
            return res;
        }

        static void SetStatuses(DeployResult res)
        {
            SetStatus(
                res.Created,
                "Created",
                string.Empty,
                SeverityLevel.Info);
            SetStatus(
                res.Updated,
                "Updated",
                string.Empty,
                SeverityLevel.Info);
            SetStatus(
                res.Deleted,
                "Deleted",
                string.Empty,
                SeverityLevel.Info);
        }

        static void SetSuccessfulDeployStatuses(List<IProjectAccessFile> filesToDeploy, DeployResult res, List<AccessControlStatement> remoteStatements)
        {
            foreach (var f in filesToDeploy)
                f.Status = new DeploymentStatus("Deployed", "Deployed Successfully");

            SetStatus(
                res.Created,
                "Created",
                string.Empty,
                SeverityLevel.Info);
            SetStatus(
                res.Updated.Where(s => s.HasStatementChanged(remoteStatements)).ToList(),
                "Updated",
                string.Empty,
                SeverityLevel.Info);
            SetStatus(
                res.Updated.Where(s => !s.HasStatementChanged(remoteStatements)).ToList(),
                "Updated",
                "Statement was unchanged",
                SeverityLevel.Info);
            SetStatus(
                res.Deleted,
                "Deleted",
                string.Empty,
                SeverityLevel.Info);
        }

        static List<AccessControlStatement> FindStatementsToUpdate(
            HashSet<string> remoteSet,
            IReadOnlyList<AccessControlStatement> local)
        {
            var toUpdate = local
                .Where(k => remoteSet.Contains(k.Sid))
                .ToList();

            return toUpdate;
        }

        static List<AccessControlStatement> FindStatementsToCreate(
            HashSet<string> remoteSet,
            IReadOnlyList<AccessControlStatement> local)
        {
            return local
                .Where(k => !remoteSet.Contains(k.Sid))
                .ToList();
        }

        static List<AccessControlStatement> FindStatementsToDelete(
            IReadOnlyList<AccessControlStatement> remote,
            HashSet<string> localSet,
            bool reconcile)
        {
            if (!reconcile)
            {
                return new List<AccessControlStatement>();
            }

            var toDelete = remote
                .Where(r => !localSet.Contains(r.Sid))
                .ToList();

            return toDelete;
        }

        static List<IProjectAccessFile> FindFilesToDeploy(
            IReadOnlyList<IProjectAccessFile> files,
            IReadOnlyList<AccessControlStatement> toDeploy)
        {
            return files
                .Where(file => file.Statements.Any(toDeploy.Contains))
                .ToList();
        }

        static List<IProjectAccessFile> FindFailedFiles(
            List<ProjectAccessPolicyDeploymentException> deploymentExceptions)
        {
            var failed = new List<IProjectAccessFile>();

            foreach (var exception in deploymentExceptions)
            {
                failed.AddRange(exception.AffectedFiles);
            }

            return failed.Distinct().ToList();
        }

        async Task<List<AccessControlStatement>> GetServerProjectAccessPolicies(
            IReadOnlyList<IProjectAccessFile> configFiles,
            bool dryRun)
        {
            try
            {
                return await m_ProjectAccessClient.GetAsync();
            }
            catch (Exception e)
            {
                if (!dryRun)
                    SetFailedStatus(configFiles, detail: e.Message);
                throw;
            }
        }

        void HandleDeploymentException(ICollection<ProjectAccessPolicyDeploymentException> deploymentExceptions)
        {
            if (!deploymentExceptions.Any())
            {
                return;
            }

            foreach (var deploymentException in deploymentExceptions)
            {
                SetFailedStatus(
                    deploymentException.AffectedFiles,
                    deploymentException.StatusDescription,
                    deploymentException.StatusDetail);
            }
        }

        void SetFailedStatus(IReadOnlyList<IProjectAccessFile> files, string status = null, string detail = null)
        {
            foreach (var projectAccessFile in files)
            {
                projectAccessFile.Status = new DeploymentStatus(status, detail, SeverityLevel.Error);
            }

            SetStatusAndProgress(
                files,
                status ?? "Failed to deploy",
                detail ?? " Unknown Error",
                SeverityLevel.Error,
                0f);
        }

        void SetStatusAndProgress(
            IReadOnlyList<IProjectAccessFile> files,
            string status,
            string detail,
            SeverityLevel severityLevel,
            float progress)
        {
            foreach (var file in files)
            {
                UpdateStatus(
                    file,
                    status,
                    detail,
                    severityLevel);
                UpdateProgress(file, progress);
            }
        }

        protected virtual void UpdateStatus(
            IProjectAccessFile projectAccessFile,
            string status,
            string detail,
            SeverityLevel severityLevel)
        {
            projectAccessFile
                .Statements
                .ForEach(s => s.Status = new DeploymentStatus(status, detail, severityLevel));
        }

        protected virtual void UpdateProgress(
            IProjectAccessFile projectAccessFile,
            float progress)
        {
            projectAccessFile
                .Statements
                .ForEach(s => s.Progress = progress);
        }


        void SetStartDeployingStatus(IReadOnlyList<IProjectAccessFile> files)
        {
            SetStatusAndProgress(
                files,
                string.Empty,
                string.Empty,
                SeverityLevel.None,
                0f);
        }

        static void SetStatus(
            IReadOnlyList<IDeploymentItem> items,
            string status,
            string detail,
            SeverityLevel severityLevel)
        {
            foreach (var file in items)
            {
                file.Status = new DeploymentStatus(status, detail, severityLevel);
            }
        }
    }
}
