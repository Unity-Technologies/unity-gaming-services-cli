using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Access.Authoring.Core.ErrorHandling;
using Unity.Services.Access.Authoring.Core.IO;
using Unity.Services.Access.Authoring.Core.Json;
using Unity.Services.Access.Authoring.Core.Model;
using Unity.Services.Access.Authoring.Core.Results;
using Unity.Services.Access.Authoring.Core.Service;
using Unity.Services.Access.Authoring.Core.Validations;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Access.Authoring.Core.Fetch
{
    public class ProjectAccessFetchHandler : IProjectAccessFetchHandler
    {
        internal const string FetchResultName = "project-statements.ac";
        readonly IProjectAccessClient m_Client;
        readonly IFileSystem m_FileSystem;
        readonly IJsonConverter m_JsonConverter;
        readonly IProjectAccessConfigValidator m_ProjectAccessValidator;

        public ProjectAccessFetchHandler(
            IProjectAccessClient client,
            IFileSystem fileSystem,
            IJsonConverter jsonConverter,
            IProjectAccessConfigValidator projectAccessValidator)
        {
            m_Client = client;
            m_FileSystem = fileSystem;
            m_JsonConverter = jsonConverter;
            m_ProjectAccessValidator = projectAccessValidator;
        }

        public async Task<FetchResult> FetchAsync(
            string rootDirectory,
            IReadOnlyList<IProjectAccessFile> files,
            bool dryRun = false,
            bool reconcile = false,
            CancellationToken token = default)
        {
            var remote = await m_Client.GetAsync();

            var fetchExceptions = new List<ProjectAccessPolicyDeploymentException>();

            var validLocal =
                m_ProjectAccessValidator.FilterNonDuplicatedAuthoringStatements(files, fetchExceptions);

            if (fetchExceptions.Count > 0)
            {
                await HandleFetchExceptions(fetchExceptions);
            }

            var localSet = validLocal.Select(l => l.Sid).ToHashSet();
            var remoteSet = remote.Select(l => l.Sid).ToHashSet();

            var toUpdate = FindEntriesToUpdate(remote, localSet);
            var toDelete = FindEntriesToDelete(remoteSet, validLocal);
            var toCreate = FindEntriesToCreate(remote, localSet, reconcile);
            var toFetch = files.ToList();

            if (dryRun)
            {
                var res = new FetchResult(
                    toCreate,
                    toUpdate,
                    toDelete,
                    toFetch);
                UpdateStatus(toFetch, toDelete, toUpdate, toCreate);
                return res;
            }

            UpdateLocal(files, toUpdate);
            DeleteLocal(files, toDelete);

            var defaultFile = GetDefaultFile(rootDirectory, toCreate, files);

            await WriteOrDeleteFiles(files);

            if (reconcile && toCreate.Count > 0)
            {
                await WriteOrDeleteFiles(
                    new[]
                    {
                        defaultFile
                    });

                if (!toFetch.Contains(defaultFile))
                    toFetch.Add(defaultFile);
            }

            UpdateStatus(toFetch, toDelete, toUpdate, toCreate);
            foreach (var file in toFetch)
            {
                file.Status = new DeploymentStatus("Deployed", string.Empty, SeverityLevel.Success);
            }
            return new FetchResult(
                toCreate,
                toUpdate,
                toDelete,
                toFetch);
        }

        static IProjectAccessFile GetDefaultFile(
            string rootDirectory,
            IReadOnlyList<AccessControlStatement> toCreate,
            IReadOnlyList<IProjectAccessFile> files)
        {
            var defaultFile = files.FirstOrDefault(f => f.Name == FetchResultName);

            if (defaultFile == null)
            {
                var filePath = Path.GetFullPath(Path.Combine(rootDirectory, FetchResultName));
                var file = new ProjectAccessFile()
                {
                    Name = Path.GetFileName(filePath),
                    Path = filePath,
                };

                foreach (var statement in toCreate)
                {
                    statement.Path = filePath;
                }
                file.Statements = (List<AccessControlStatement>)toCreate;

                defaultFile = file;
            }
            else
            {
                defaultFile.UpdateOrCreateStatements(toCreate);
            }

            return defaultFile;
        }

        static List<AccessControlStatement> FindEntriesToUpdate(
            IReadOnlyList<AccessControlStatement> remote,
            HashSet<string>  localSet)
        {
            var toUpdate = remote
                .Where(r => localSet.Contains(r.Sid))
                .ToList();

            return toUpdate;
        }

        static List<AccessControlStatement> FindEntriesToDelete(
            HashSet<string> remote,
            IReadOnlyList<AccessControlStatement> local)
        {
            var toDelete = local
                .Where(l => !remote.Contains(l.Sid))
                .ToList();

            return toDelete;
        }

        static List<AccessControlStatement> FindEntriesToCreate(
            IReadOnlyList<AccessControlStatement> remote,
            HashSet<string> localSet,
            bool reconcile)
        {
            if (!reconcile)
                return new List<AccessControlStatement>();

            return remote
                .Where(k => !localSet.Contains(k.Sid))
                .ToList();
        }

        static void UpdateLocal(
            IReadOnlyList<IProjectAccessFile> files,
            IReadOnlyList<AccessControlStatement> remote)
        {
            foreach (var file in files)
            {
                file.UpdateStatements(remote);
            }
        }

        static void DeleteLocal(
            IReadOnlyList<IProjectAccessFile> files,
            IReadOnlyList<AccessControlStatement> toDelete)
        {
            foreach (var file in files.Where(file => file.Statements.Any()))
            {
                file.RemoveStatements(toDelete);
            }
        }

        async Task WriteOrDeleteFiles(
            IReadOnlyList<IProjectAccessFile> files)
        {
            var tasks = new List<Task>(files.Count);

            foreach (var file in files)
            {
                if (file.Statements.Any())
                {
                    var content = new ProjectAccessFileContent(file.Statements);

                    var text = m_JsonConverter.SerializeObject(content);

                    tasks.Add(m_FileSystem.WriteAllText(file.Path, text));
                }
                else
                {
                    tasks.Add(m_FileSystem.Delete(file.Path));
                }
            }

            await Task.WhenAll(tasks);
        }

        static Task HandleFetchExceptions(List<ProjectAccessPolicyDeploymentException> fetchExceptions)
        {
            var exceptions = fetchExceptions
                .SelectMany(exception => exception.AffectedFiles.SelectMany(file => file.Statements),
                    (exception, s) => new DuplicateAuthoringStatementsException(s.Sid, exception.AffectedFiles))
                .ToList();

            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }

            return Task.CompletedTask;
        }

        static void UpdateStatus(
            IReadOnlyList<IProjectAccessFile> files,
            IReadOnlyList<AccessControlStatement> toDelete,
            IReadOnlyList<AccessControlStatement> toUpdate,
            IReadOnlyList<AccessControlStatement> toCreate)
        {
            var allStatements = files.SelectMany(s => s.Statements).ToList();
            var updateIds = toUpdate.Select(s => s.Sid).ToHashSet();
            var createIds = toCreate.Select(s => s.Sid).ToHashSet();
            var deleteIds = toDelete.Select(s => s.Sid).ToHashSet();
            //Must update the local references, not the remote ones
            SetStatus(allStatements.Where(s => updateIds.Contains(s.Sid)).ToList(), "Updated");
            SetStatus(allStatements.Where(s => createIds.Contains(s.Sid)).ToList(), "Created");
            SetStatus(allStatements.Where(s => deleteIds.Contains(s.Sid)).ToList(), "Deleted");
        }

        static void SetStatus(List<AccessControlStatement> statements, string action)
        {
            statements.ForEach(s => s.Status = new DeploymentStatus("Fetched", action, SeverityLevel.Success));
        }

        static void SetStatus(IReadOnlyList<AccessControlStatement> statements, string message, string detail, SeverityLevel level)
        {
            foreach (var statement in statements)
                statement.Status = new DeploymentStatus(message, detail, level);
        }
    }
}
