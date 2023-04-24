using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Authoring.Model;
using Unity.Services.Cli.CloudCode.Authoring;
using Unity.Services.Cli.CloudCode.Deploy;
using Unity.Services.Cli.CloudCode.Exceptions;
using Unity.Services.Cli.CloudCode.Parameters;
using Unity.Services.Cli.TestUtils;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.UnitTest;

[TestFixture]
class JavaScriptFetchHandlerTests
{
    const string k_RootDirectory = "foo/bar";
    const string k_UnpublishedErrorMessage = "This script doesn't have parameters and hasn't been published.";
    const string k_ParameterLessErrorMessage = "This script doesn't have parameters but has been published.";

    readonly Mock<IJavaScriptClient> m_Client = new();
    readonly Mock<ICloudCodeScriptParser> m_ScriptParser = new();
    readonly Mock<IFile> m_File = new();
    readonly Mock<IEqualityComparer<IScript>> m_Comparer = new();
    readonly Mock<ILogger> m_Logger = new();
    readonly JavaScriptFetchHandler m_Handler;

    TestableScriptList m_ToUpdate = null!;
    TestableScriptList m_ToCreate = null!;
    TestableScriptList m_ToDelete = null!;
    List<IScript> m_LocalScripts = null!;
    List<ScriptInfo> m_RemoteScripts = null!;
    IScript m_AnyUnpublishedScript = null!;
    CloudCodeScript m_AnyParameterLessScript = null!;
    CloudCodeScript m_AnyScriptWithParameters = null!;

    public JavaScriptFetchHandlerTests()
    {
        m_Handler = new JavaScriptFetchHandler(
            m_Client.Object,
            m_ScriptParser.Object,
            m_File.Object,
            m_Comparer.Object,
            m_Logger.Object);
    }

    [SetUp]
    public void SetUp()
    {
        m_Client.Reset();
        m_ScriptParser.Reset();
        m_File.Reset();
        m_Comparer.Reset();
        m_Logger.Reset();

        m_Comparer.Setup(x => x.Equals(It.IsAny<IScript>(), It.IsAny<IScript>()))
            .Returns<IScript, IScript>((left, right) => left.Name.Equals(right.Name));

        SetUpScripts();
    }

    void SetUpScripts()
    {
        m_ToUpdate = CreateScriptsFromPaths(
            new[]
            {
                $"{k_RootDirectory}/Updated.js",
            },
            new[]
            {
                $"{k_RootDirectory}/duplicateUpdate.js",
                $"{k_RootDirectory}/failed/duplicateUpdate.js",
            });
        m_ToCreate = CreateScriptsFromPaths(
            new[]
            {
                $"{k_RootDirectory}/Created.js",
            },
            new[]
            {
                $"{k_RootDirectory}/failedCreation.js",
            });
        m_ToDelete = CreateScriptsFromPaths(
            new[]
            {
                $"{k_RootDirectory}/Deleted.js",
            },
            new[]
            {
                $"{k_RootDirectory}/failedDeletion.js",
            });
        m_LocalScripts = m_ToDelete.All.Union(m_ToUpdate.All)
            .ToList();
        m_RemoteScripts = m_ToCreate.All.Union(m_ToUpdate.All)
            .Select(x => new ScriptInfo(x.Name))
            .ToList();

        foreach (var script in m_ToCreate.Failed)
        {
            m_AnyUnpublishedScript = SetUpUnpublished(script);
        }

        foreach (var script in m_ToCreate.Expected)
        {
            m_AnyParameterLessScript = SetUpParameterLessBody((CloudCodeScript)script);
        }

        foreach (var script in m_ToUpdate.Expected)
        {
            m_AnyScriptWithParameters = SetUpWithParameters((CloudCodeScript)script);
        }

        IScript SetUpUnpublished(IScript script)
        {
            script.Body = $"// {k_UnpublishedErrorMessage}";
            script.LastPublishedDate = "";
            SetUpParsingFailure(script.Body, k_UnpublishedErrorMessage);
            return script;
        }

        void SetUpParsingFailure(string expectedCode, string errorMessage)
        {
            m_ScriptParser.Setup(x => x.ParseScriptParametersAsync(expectedCode, CancellationToken.None))
                .ThrowsAsync(new ScriptEvaluationException(errorMessage));
        }

        CloudCodeScript SetUpParameterLessBody(CloudCodeScript script)
        {
            script.Body = $"// {k_ParameterLessErrorMessage}";
            script.LastPublishedDate = DateTime.MinValue.ToString(CultureInfo.InvariantCulture);
            script.Parameters = new List<CloudCodeParameter>
            {
                new()
                {
                    Name = "bar",
                    ParameterType = ParameterType.JSON,
                }
            };
            SetUpParsingFailure(script.Body, k_ParameterLessErrorMessage);
            return script;
        }

        CloudCodeScript SetUpWithParameters(CloudCodeScript script)
        {
            script.Body = "// This script has parameters.";
            script.Parameters = new List<CloudCodeParameter>
            {
                new()
                {
                    Name = "foo",
                    ParameterType = ParameterType.Any,
                }
            };
            m_ScriptParser.Setup(x => x.ParseScriptParametersAsync(script.Body, CancellationToken.None))
                .ReturnsAsync(
                    new[]
                    {
                        new ScriptParameter("foo")
                    });

            return script;
        }
    }

    static TestableScriptList CreateScriptsFromPaths(
        IEnumerable<string> expectedPaths, IEnumerable<string> failedPaths)
    {
        return new TestableScriptList(
            expectedPaths.Select(CreateScriptFromPath).ToList(),
            failedPaths.Select(CreateScriptFromPath).ToList());

        IScript CreateScriptFromPath(string path)
        {
            var fullPath = Path.GetFullPath(path);
            return new CloudCodeScript
            {
                Name = ScriptName.FromPath(fullPath),
                Path = fullPath,
            };
        }
    }

    [Test]
    public async Task FetchRemoteScriptsAsyncReturnsFilteredRemoteList()
    {
        SetUpFetchRemoteScripts();
        var expectedRemote = m_ToCreate.All.Union(m_ToUpdate.Expected)
            .ToList();

        var remoteScripts = await m_Handler.FetchRemoteScriptsAsync(m_ToUpdate.Failed);

        Assert.Multiple(AssertRemoteAreEquivalentToExpected);

        void AssertRemoteAreEquivalentToExpected()
        {
            Assert.That(remoteScripts.Count, Is.EqualTo(expectedRemote.Count));
            foreach (var remote in remoteScripts)
            {
                Assert.That(expectedRemote.Any(x => remote.Name.Equals(x.Name)), Is.True);
            }
        }
    }

    void SetUpFetchRemoteScripts()
    {
        m_Client.Setup(x => x.ListScripts())
            .ReturnsAsync(m_RemoteScripts);
    }

    [Test]
    public async Task GetRemoteScriptDetailsAsync()
    {
        var incompleteScripts = m_RemoteScripts
            .Select(
                x => new CloudCodeScript(x)
                {
                    Body = "",
                    Parameters = new List<CloudCodeParameter>(),
                })
            .ToList();
        SetUpGetRemoteScriptDetails();
        var expectedScripts = m_ToCreate.All.Union(m_ToUpdate.All)
            .ToList();

        await m_Handler.GetRemoteScriptDetailsAsync(incompleteScripts);

        Assert.Multiple(AssertIncompleteScriptsAreUpdated);

        void AssertIncompleteScriptsAreUpdated()
        {
            foreach (var script in incompleteScripts)
            {
                var matchingScriptWithDetails = expectedScripts.First(x => x.Name.Equals(script.Name));
                Assert.That(script.Body, Is.EqualTo(matchingScriptWithDetails.Body));
                Assert.That(script.Parameters, Is.EqualTo(matchingScriptWithDetails.Parameters));
                Assert.That(script.LastPublishedDate, Is.EqualTo(matchingScriptWithDetails.LastPublishedDate));
            }
        }
    }

    void SetUpGetRemoteScriptDetails()
    {
        foreach (var script in m_ToCreate.All.Union(m_ToUpdate.All))
        {
            m_Client.Setup(x => x.Get(script.Name))
                .ReturnsAsync(script);
        }
    }

    [Test]
    public void GetCreatedScriptsWithoutReconcileReturnsEmpty()
    {
        var createdScripts = m_Handler.GetCreatedScripts(
            It.IsAny<IEnumerable<IScript>>(), It.IsAny<IEnumerable<IScript>>(), false, It.IsAny<string>());

        Assert.That(createdScripts, Is.EquivalentTo(Array.Empty<IScript>()));
    }

    [Test]
    public void GetCreatedScriptsWithReconcileReturnsScriptsToCreateWithSetPaths()
    {
        foreach (var script in m_ToCreate.Expected)
        {
            ((CloudCodeScript)script).Path = "";
        }

        var createdScripts = m_Handler.GetCreatedScripts(
            m_ToCreate.Failed, m_ToCreate.All, true, k_RootDirectory);

        Assert.Multiple(AssertCreatedHaveUpdatedPaths);

        void AssertCreatedHaveUpdatedPaths()
        {
            Assert.That(createdScripts, Is.EquivalentTo(m_ToCreate.Expected));
            foreach (var script in createdScripts)
            {
                Assert.That(script.Path, Contains.Substring(k_RootDirectory));
            }
        }
    }

    [Test]
    public void GetUpdatedScriptsIntersectsLocalWithRemoteAndUpdatesPaths()
    {
        var remote = m_ToUpdate.Expected.Select(
                x => new CloudCodeScript(x)
                {
                    Path = "",
                })
            .ToList();

        var updatedScripts = m_Handler.GetUpdatedScripts(m_ToUpdate.All, remote);

        Assert.Multiple(AssertIntersectionAndPathAreSet);

        void AssertIntersectionAndPathAreSet()
        {
            Assert.That(updatedScripts, Is.EquivalentTo(remote));
            foreach (var script in updatedScripts)
            {
                Assert.That(script.Path, Is.Not.Empty);
            }
        }
    }

    [Test]
    public void GetDeletedScriptsFiltersOutRemoteScripts()
    {
        var local = m_ToDelete.Expected.Union(m_ToCreate.Expected).ToList();

        var deletedScripts = m_Handler.GetDeletedScripts(local, m_ToCreate.Expected);

        Assert.That(deletedScripts, Is.EquivalentTo(m_ToDelete.Expected));
    }

    [Test]
    public async Task EnforceScriptParametersInBodyAsyncInjectsParametersWhenMissingAndKnown()
    {
        var originalParameterLessBody = m_AnyParameterLessScript.Body;
        var originalBodyWithParameters = m_AnyScriptWithParameters.Body;
        var scripts = new List<IScript>
        {
            m_AnyUnpublishedScript,
            m_AnyParameterLessScript,
            m_AnyScriptWithParameters,
        };
        var failed = new List<IScript>();

        await m_Handler.EnforceScriptParametersInBodyAsync(scripts, failed, CancellationToken.None);

        Assert.Multiple(AssertKnownMissingParametersHaveBeenInjected);

        void AssertKnownMissingParametersHaveBeenInjected()
        {
            Assert.That(failed, Contains.Item(m_AnyUnpublishedScript));
            Assert.That(failed, Does.Not.Contain(m_AnyParameterLessScript));
            Assert.That(failed, Does.Not.Contain(m_AnyScriptWithParameters));
            Assert.That(m_AnyParameterLessScript.Body, Is.Not.EqualTo(originalParameterLessBody));
            Assert.That(m_AnyScriptWithParameters.Body, Is.EqualTo(originalBodyWithParameters));
            TestsHelper.VerifyLoggerWasCalled(m_Logger, LogLevel.Warning, message: k_UnpublishedErrorMessage);
            TestsHelper.VerifyLoggerWasCalled(m_Logger, LogLevel.Warning, message: k_ParameterLessErrorMessage);
            TestsHelper.VerifyLoggerWasCalled(
                m_Logger,
                LogLevel.Warning,
                message: $"\"{m_AnyUnpublishedScript.Path}\" parameters couldn't be determined.");
        }
    }

    [Test]
    public async Task ApplyFetchAsyncExecutesAllExpectedFileOperations()
    {
        var expectedFailures = m_ToUpdate.Failed.Union(m_ToCreate.Failed)
            .Union(m_ToDelete.Failed)
            .ToList();
        var failed = new List<IScript>();
        SetUpFileCreationsOrUpdates(m_ToUpdate);
        SetUpFileCreationsOrUpdates(m_ToCreate);
        SetUpFileDeletions();

        await m_Handler.ApplyFetchAsync(m_ToUpdate, m_ToCreate, m_ToDelete, failed, CancellationToken.None);

        Assert.Multiple(AssertExpectedFileOperationsOccuredAndFailedIsUpdated);

        void AssertExpectedFileOperationsOccuredAndFailedIsUpdated()
        {
            m_File.Verify();
            Assert.That(failed, Is.EquivalentTo(expectedFailures));
        }
    }

    void SetUpFileCreationsOrUpdates(TestableScriptList scripts)
    {
        foreach (var script in scripts.Expected)
        {
            m_File.Setup(x => x.WriteAllTextAsync(script.Path, script.Body, CancellationToken.None))
                .Returns(Task.CompletedTask)
                .Verifiable();
        }

        foreach (var script in scripts.Failed)
        {
            m_File.Setup(x => x.WriteAllTextAsync(script.Path, script.Body, CancellationToken.None))
                .ThrowsAsync(new InvalidOperationException())
                .Verifiable();
        }
    }

    void SetUpFileDeletions()
    {
        foreach (var script in m_ToDelete.Expected)
        {
            m_File.Setup(x => x.Delete(script.Path))
                .Verifiable();
        }

        foreach (var script in m_ToDelete.Failed)
        {
            m_File.Setup(x => x.Delete(script.Path))
                .Throws(new InvalidOperationException())
                .Verifiable();
        }
    }

    [Test]
    public async Task FetchAsyncWithReconcileGivesExpectedResults()
    {
        SetUpFetchRemoteScripts();
        SetUpGetRemoteScriptDetails();
        SetUpFileCreationsOrUpdates(m_ToUpdate);
        SetUpFileCreationsOrUpdates(m_ToCreate);
        SetUpFileDeletions();
        var expectedCreated = m_ToCreate.Expected.Select(x => x.Name.ToString())
            .Distinct()
            .ToList();
        var expectedDeleted = m_ToDelete.Expected.Select(x => x.Name.ToString())
            .Distinct()
            .ToList();
        var expectedUpdated = m_ToUpdate.Expected.Select(x => x.Name.ToString())
            .Distinct()
            .ToList();
        var expectedFailures = m_ToCreate.Failed
            .Union(m_ToDelete.Failed)
            .Union(m_ToUpdate.Failed)
            .Select(x => x.Name.ToString())
            .Distinct()
            .ToList();

        var result = await m_Handler.FetchAsync(
            k_RootDirectory, m_LocalScripts, false, true, CancellationToken.None);

        Assert.Multiple(
            () => AssertResult(result, expectedCreated, expectedDeleted, expectedUpdated, expectedFailures));
    }

    static void AssertResult(
        FetchResult result,
        IReadOnlyCollection<string> expectedCreated,
        IReadOnlyCollection<string> expectedDeleted,
        IReadOnlyCollection<string> expectedUpdated,
        IEnumerable<string> expectedFailures)
    {
        var expectedFetched = expectedCreated
            .Union(expectedDeleted)
            .Union(expectedUpdated)
            .ToList();
        Assert.That(result.Created, Is.EquivalentTo(expectedCreated));
        Assert.That(result.Deleted, Is.EquivalentTo(expectedDeleted));
        Assert.That(result.Updated, Is.EquivalentTo(expectedUpdated));
        Assert.That(result.Failed, Is.EquivalentTo(expectedFailures));
        Assert.That(result.Fetched, Is.EquivalentTo(expectedFetched));
    }

    [Test]
    public async Task FetchAsyncWithoutReconcileGivesExpectedResults()
    {
        SetUpFetchRemoteScripts();
        SetUpGetRemoteScriptDetails();
        SetUpFileCreationsOrUpdates(m_ToUpdate);
        SetUpFileDeletions();
        var expectedDeleted = m_ToDelete.Expected.Select(x => x.Name.ToString())
            .Distinct()
            .ToList();
        var expectedUpdated = m_ToUpdate.Expected.Select(x => x.Name.ToString())
            .Distinct()
            .ToList();
        var expectedFailures = m_ToDelete.Failed
            .Union(m_ToUpdate.Failed)
            .Select(x => x.Name.ToString())
            .Distinct()
            .ToList();

        var result = await m_Handler.FetchAsync(
            k_RootDirectory, m_LocalScripts, false, false, CancellationToken.None);

        Assert.Multiple(
            () => AssertResult(result, Array.Empty<string>(), expectedDeleted, expectedUpdated, expectedFailures));
    }

    [Test]
    public async Task FetchAsyncDryRunWithReconcileGivesExpectedResults()
    {
        SetUpFetchRemoteScripts();
        SetUpGetRemoteScriptDetails();
        var expectedCreated = m_ToCreate.Expected.Select(x => x.Name.ToString())
            .Distinct()
            .ToList();
        var expectedDeleted = m_ToDelete.All.Select(x => x.Name.ToString())
            .Distinct()
            .ToList();
        var expectedUpdated = m_ToUpdate.Expected.Select(x => x.Name.ToString())
            .Distinct()
            .ToList();
        var expectedFailures = m_ToUpdate.Failed
            .Union(m_ToCreate.Failed)
            .Select(x => x.Name.ToString())
            .Distinct()
            .ToList();

        var result = await m_Handler.FetchAsync(
            k_RootDirectory, m_LocalScripts, true, true, CancellationToken.None);

        Assert.Multiple(
            () => AssertResult(result, expectedCreated, expectedDeleted, expectedUpdated, expectedFailures));
    }

    [Test]
    public async Task FetchAsyncDryRunWithoutReconcileGivesExpectedResults()
    {
        SetUpFetchRemoteScripts();
        SetUpGetRemoteScriptDetails();
        var expectedDeleted = m_ToDelete.All.Select(x => x.Name.ToString())
            .Distinct()
            .ToList();
        var expectedUpdated = m_ToUpdate.Expected.Select(x => x.Name.ToString())
            .Distinct()
            .ToList();
        var expectedFailures = m_ToUpdate.Failed
            .Select(x => x.Name.ToString())
            .Distinct()
            .ToList();

        var result = await m_Handler.FetchAsync(
            k_RootDirectory, m_LocalScripts, true, false, CancellationToken.None);

        Assert.Multiple(
            () => AssertResult(result, Array.Empty<string>(), expectedDeleted, expectedUpdated, expectedFailures));
    }
}
