using NUnit.Framework;
using Unity.Services.Tooling.Editor.AccessControl.Authoring.Core.Model;

namespace Unity.Services.Cli.Access.UnitTest.Model;

[TestFixture]
public class ProjectAccessFileExtensionTests
{
    [Test]
    public Task ProjectAccessFileExtension_RemoveStatements()
    {
        var projectAccessFile = GetLocalProjectAccessFile();
        var statementsToRemove = GetStatementsToRemove();

        projectAccessFile.RemoveStatements(statementsToRemove);
        Assert.That(projectAccessFile.Statements, Has.Count.EqualTo(1));

        return Task.CompletedTask;
    }

    [Test]
    public Task ProjectAccessFileExtension_UpdateStatements()
    {
        var projectAccessFile = GetLocalProjectAccessFile();
        var statementsToUpdate = GetStatementsToUpdate();

        projectAccessFile.UpdateStatements(statementsToUpdate);
        Assert.That(projectAccessFile.Statements, Has.Count.EqualTo(2));
        Assert.That(projectAccessFile.Statements[1].Version, Is.EqualTo("3.0"));

        return Task.CompletedTask;
    }

    [Test]
    public Task ProjectAccessFileExtension_UpdateOrCreateStatementsShouldUpdate()
    {
        var projectAccessFile = GetLocalProjectAccessFile();
        var statementsToUpdate = GetStatementsToUpdate();

        projectAccessFile.UpdateOrCreateStatements(statementsToUpdate);
        Assert.That(projectAccessFile.Statements, Has.Count.EqualTo(2));
        Assert.That(projectAccessFile.Statements[1].Version, Is.EqualTo("3.0"));

        return Task.CompletedTask;
    }

    [Test]
    public Task ProjectAccessFileExtension_UpdateOrCreateStatementsShouldCreate()
    {
        var projectAccessFile = GetLocalProjectAccessFile();
        var statementsToCreate = GetStatementsToCreate();

        projectAccessFile.UpdateOrCreateStatements(statementsToCreate);
        Assert.That(projectAccessFile.Statements, Has.Count.EqualTo(3));
        Assert.That(projectAccessFile.Statements[2].Sid, Is.EqualTo("allow-access-to-economy"));

        return Task.CompletedTask;
    }

    static IProjectAccessFile GetLocalProjectAccessFile()
    {
        return new ProjectAccessFile()
        {
            Name = "file1",
            Path = "path1",
            Statements = new List<AccessControlStatement>()
            {
                new AccessControlStatement()
                {
                    Sid = "allow-access-to-cloud-save",
                    Action = new List<string>()
                    {
                        "*"
                    },
                    Effect = "Allow",
                    Principal = "Player",
                    Resource = "urn:ugs:cloud-save:*",
                    ExpiresAt = DateTime.MaxValue,
                    Version = "100.0"
                },
                new AccessControlStatement()
                {
                    Sid = "deny-access-to-lobby",
                    Action = new List<string>()
                    {
                        "*"
                    },
                    Effect = "Deny",
                    Principal = "Player",
                    Resource = "urn:ugs:lobby:*",
                    ExpiresAt = DateTime.MaxValue,
                    Version = "2.0"
                }
            }
        };
    }


    static IReadOnlyList<AccessControlStatement> GetStatementsToRemove()
    {
        return new List<AccessControlStatement>()
        {
            new AccessControlStatement()
            {
                Sid = "deny-access-to-lobby",
                Action = new List<string>()
                {
                    "*"
                },
                Effect = "Deny",
                Principal = "Player",
                Resource = "urn:ugs:lobby:*",
                ExpiresAt = DateTime.MaxValue,
                Version = "2.0"
            }
        };
    }

    static IReadOnlyList<AccessControlStatement> GetStatementsToUpdate()
    {
        return new List<AccessControlStatement>()
        {
            new AccessControlStatement()
            {
                Sid = "deny-access-to-lobby",
                Action = new List<string>()
                {
                    "*"
                },
                Effect = "Deny",
                Principal = "Player",
                Resource = "urn:ugs:lobby:*",
                ExpiresAt = DateTime.MaxValue,
                Version = "3.0"
            }
        };
    }

    static IReadOnlyList<AccessControlStatement> GetStatementsToCreate()
    {
        return new List<AccessControlStatement>()
        {
            new AccessControlStatement()
            {
                Sid = "allow-access-to-economy",
                Action = new List<string>()
                {
                    "*"
                },
                Effect = "Allow",
                Principal = "Player",
                Resource = "urn:ugs:economy:*",
                ExpiresAt = DateTime.MaxValue,
                Version = "1.0"
            }
        };
    }
}
