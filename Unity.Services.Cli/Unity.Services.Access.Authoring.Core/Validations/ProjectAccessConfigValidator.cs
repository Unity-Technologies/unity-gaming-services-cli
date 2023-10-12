using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Services.Access.Authoring.Core.ErrorHandling;
using Unity.Services.Access.Authoring.Core.Model;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Access.Authoring.Core.Validations
{
    public class ProjectAccessConfigValidator : IProjectAccessConfigValidator
    {
        public List<AccessControlStatement> FilterNonDuplicatedAuthoringStatements(
            IReadOnlyList<IProjectAccessFile> files,
            ICollection<ProjectAccessPolicyDeploymentException> deploymentExceptions)
        {
            var nonDuplicatedStatements = new List<AccessControlStatement>();

            foreach (var f in files)
            {
                ProjectAccessFile file = (ProjectAccessFile)f;

                foreach (var statement in file.Statements)
                {

                    var containingFiles = files
                        .Where(f => f.Statements.Exists(fs => fs.Sid == statement.Sid))
                        .ToList();

                    if (containingFiles.Count > 1)
                    {
                        deploymentExceptions.Add(new DuplicateAuthoringStatementsException(statement.Sid, containingFiles));
                        file.Status = new DeploymentStatus("Validation Error", $"Multiple resources with the same identifier '{statement.Sid}' were found. ", SeverityLevel.Error);
                        continue;
                    }

                    var duplicatedStatementsInASameFileCount = file.Statements
                        .GroupBy(s => s.Sid).Count(t => t.Count() > 1);

                    if (duplicatedStatementsInASameFileCount > 0)
                    {
                        containingFiles.Add(file);
                        deploymentExceptions.Add(new DuplicateAuthoringStatementsException(statement.Sid, containingFiles));
                        file.Status = new DeploymentStatus("Validation Error", $"Multiple resources with the same identifier '{statement.Sid}' were found. ", SeverityLevel.Error);
                        continue;
                    }

                    nonDuplicatedStatements.Add(statement);
                }

            }

            return nonDuplicatedStatements;
        }

        public bool Validate(
            IProjectAccessFile file,
            ICollection<ProjectAccessPolicyDeploymentException> deploymentExceptions)
        {
            bool validated = true;

            foreach (var authoringStatement in file.Statements)
            {
                var isSidValidated = ValidateSid(authoringStatement.Sid, (ProjectAccessFile)file, deploymentExceptions);
                var isResourceValidated = ValidateResource(authoringStatement.Resource, (ProjectAccessFile)file, deploymentExceptions);
                var isActionValidated = ValidateAction(authoringStatement.Action, (ProjectAccessFile)file, deploymentExceptions);
                var isPrincipalValidated = ValidatePrincipal(authoringStatement.Principal, (ProjectAccessFile)file, deploymentExceptions);
                var isEffectValidated = ValidateEffect(authoringStatement.Effect, (ProjectAccessFile)file, deploymentExceptions);

                if (!isSidValidated || !isResourceValidated || !isActionValidated || !isPrincipalValidated || !isEffectValidated)
                {
                    validated = false;
                }
            }

            return validated;
        }

        static bool ValidateSid(string sid, ProjectAccessFile projectAccessFile, ICollection<ProjectAccessPolicyDeploymentException> deploymentExceptions)
        {
            Regex regex = new Regex("^[A-Za-z0-9][A-Za-z0-9_-]{5,59}$", RegexOptions.CultureInvariant, matchTimeout: TimeSpan.FromSeconds(2));
            if (regex.Match(sid).Success) return true;

            deploymentExceptions.Add(new InvalidDataException(projectAccessFile, "Invalid value for Sid, must match a pattern of " + regex));
            projectAccessFile.Status = new DeploymentStatus("Validation Error", "Invalid value for Sid, must match a pattern of " + regex, SeverityLevel.Error);
            return false;

        }

        static bool ValidateResource(string resource, ProjectAccessFile projectAccessFile, ICollection<ProjectAccessPolicyDeploymentException> deploymentExceptions)
        {
            Regex regex = new Regex("^urn:ugs:(([a-z-]*:){1}[*/]*[/a-z0-9-*]*|\\*{1})", RegexOptions.CultureInvariant, matchTimeout: TimeSpan.FromSeconds(2));
            if (regex.Match(resource).Success) return true;

            deploymentExceptions.Add(new InvalidDataException(projectAccessFile, "Invalid value for Resource, must match a pattern of " + regex));
            projectAccessFile.Status = new DeploymentStatus("Validation Error", "Invalid value for Resource, must match a pattern of " + regex, SeverityLevel.Error);
            return false;
        }

        static bool ValidateAction(List<string> action, ProjectAccessFile projectAccessFile, ICollection<ProjectAccessPolicyDeploymentException> deploymentExceptions)
        {
            var validActions = new List<string>{ "*", "Read", "Write", "Vivox:JoinMuted", "Vivox:JoinAllMuted" };
            var invalidActions = action.Where(v => !validActions.Contains(v));
            if (!invalidActions.Any()) return true;

            deploymentExceptions.Add(new InvalidDataException(projectAccessFile, "Invalid Value for Action, must be '*', 'Read', 'Write', 'Vivox:JoinMuted' or 'Vivox:JoinAllMuted'"));
            projectAccessFile.Status = new DeploymentStatus("Validation Error", "Invalid Value for Action, must be '*', 'Read', 'Write', 'Vivox:JoinMuted' or 'Vivox:JoinAllMuted'", SeverityLevel.Error);
            return false;
        }

        static bool ValidatePrincipal(string principal, ProjectAccessFile projectAccessFile, ICollection<ProjectAccessPolicyDeploymentException> deploymentExceptions)
        {
            var validPrincipals = new List<string>{ "Player" };
            if (validPrincipals.Contains(principal)) return true;

            deploymentExceptions.Add(new InvalidDataException(projectAccessFile, "Invalid Value for Principal, must be 'Player'"));
            projectAccessFile.Status = new DeploymentStatus("Validation Error", "Invalid Value for Principal, must be 'Player'", SeverityLevel.Error);
            return false;
        }

        static bool ValidateEffect(string effect, ProjectAccessFile projectAccessFile, ICollection<ProjectAccessPolicyDeploymentException> deploymentExceptions)
        {
            var validEffects = new List<string>{ "Allow", "Deny" };
            if (validEffects.Contains(effect)) return true;

            deploymentExceptions.Add(new InvalidDataException(projectAccessFile, "Invalid Value for Effect, must be 'Allow' or 'Deny"));
            projectAccessFile.Status = new DeploymentStatus("Validation Error", "Invalid Value for Effect, must be 'Allow' or 'Deny", SeverityLevel.Error);
            return false;
        }


    }
}
