using Unity.Services.Access.Authoring.Core.Model;
using Unity.Services.Access.Authoring.Core.Service;
using Unity.Services.Cli.Access.Service;
using Unity.Services.Gateway.AccessApiV1.Generated.Model;

namespace Unity.Services.Cli.Access.Deploy;

public class ProjectAccessClient : IProjectAccessClient
{
    public string ProjectId { get; private set; }
    public string EnvironmentId { get; private set; }
    public CancellationToken CancellationToken { get; private set; }

    readonly IAccessService m_Service;

    public void Initialize(string environmentId, string projectId, CancellationToken cancellationToken)
    {
        ProjectId = projectId;
        EnvironmentId = environmentId;
        CancellationToken = cancellationToken;
    }

    internal ProjectAccessClient(
        IAccessService service,
        string projectId,
        string environmentId,
        CancellationToken cancellationToken)
    {
        m_Service = service;
        ProjectId = projectId;
        EnvironmentId = environmentId;
        CancellationToken = cancellationToken;
    }

    public ProjectAccessClient(IAccessService service)
    {
        m_Service = service;
        ProjectId = string.Empty;
        EnvironmentId = string.Empty;
        CancellationToken = CancellationToken.None;
    }

    public async Task<List<AccessControlStatement>> GetAsync()
    {
        var policy = await m_Service.GetPolicyAsync(ProjectId, EnvironmentId, CancellationToken);

        return (policy.Statements == null || policy.Statements?.Count == 0)  ? new List<AccessControlStatement>() : GetAuthoringStatementsFromPolicy(policy);
    }

    public async Task UpsertAsync(IReadOnlyList<AccessControlStatement> authoringStatements)
    {
        var policy = GetPolicyFromAuthoringStatements(authoringStatements);
        await m_Service.UpsertProjectAccessCaCAsync(
            ProjectId,
            EnvironmentId,
            policy,
            CancellationToken);
    }

    public async Task DeleteAsync(IReadOnlyList<AccessControlStatement> authoringStatements)
    {
        var deleteOptions = GetDeleteOptionsFromAuthoringStatements(authoringStatements);
        await m_Service.DeleteProjectAccessCaCAsync(
            ProjectId,
            EnvironmentId,
            deleteOptions,
            CancellationToken);
    }

    static List<AccessControlStatement> GetAuthoringStatementsFromPolicy(Policy policy)
    {
        var authoringStatements = policy.Statements.Select(
                s => new AccessControlStatement()
                {
                    Name = s.Sid,
                    Path = "Remote",
                    Sid = s.Sid,
                    Action = s.Action,
                    Effect = s.Effect,
                    Principal = s.Principal,
                    Resource = s.Resource,
                    ExpiresAt = s.ExpiresAt,
                    Version = s._Version,
                })
            .ToList();

        return authoringStatements;
    }

    static Policy GetPolicyFromAuthoringStatements(IReadOnlyList<AccessControlStatement> authoringStatements)
    {
        var statements = authoringStatements.Select(
                s => new Statement(
                    sid: s.Sid,
                    action: s.Action,
                    effect: s.Effect,
                    principal: s.Principal,
                    resource: s.Resource,
                    expiresAt: s.ExpiresAt,
                    version: s.Version
                ))
            .ToList();

        var policy = new Policy(statements);
        return policy;
    }

    static DeleteOptions GetDeleteOptionsFromAuthoringStatements(IReadOnlyList<AccessControlStatement> authoringStatements)
    {
        var statementIDs = authoringStatements.Select(x => x.Sid).ToList();
        var deleteOptions = new DeleteOptions(statementIDs);
        return deleteOptions;
    }
}
