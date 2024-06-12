using NUnit.Framework;
using Moq;
using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Matchmaker.Service;
using Unity.Services.Matchmaker.Authoring.Core.ConfigApi;
using Unity.Services.Matchmaker.Authoring.Core.Fetch;
using Unity.Services.Matchmaker.Authoring.Core.Model;
using FetchResult = Unity.Services.Matchmaker.Authoring.Core.Fetch.FetchResult;

namespace Unity.Services.Cli.Matchmaker.UnitTest;

[TestFixture]
public class MatchmakerFetchServiceUnitTests
{
    [Test]
    public async Task FetchAsync_ShouldReturnFetchResult_WhenCalledWithValidParameters()
    {
        var resource = new MatchmakerConfigResource() { Name = "Test", Path = "TestPath" };
        var mockClient = new Mock<IConfigApiClient>();
        var mockFetchHandler = new Mock<IMatchmakerFetchHandler>();
        mockFetchHandler.Setup(m => m.FetchAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FetchResult()
            {
                Created = { resource },
                Updated = { resource },
                Authored = { resource },
                Failed = { resource },
                Deleted = { resource }
            });
        var service = new MatchmakerFetchService(mockClient.Object, mockFetchHandler.Object);
        var fetchInput = new FetchInput();
        var filePaths = new List<string> { "test.mme" };
        var projectId = "testProjectId";
        var environmentId = "testEnvironmentId";
        var cancellationToken = new CancellationToken();

        var result = await service.FetchAsync(fetchInput, filePaths, projectId, environmentId, null, cancellationToken);

        Assert.NotNull(result);
        Assert.That(service.ServiceName, Is.EqualTo("matchmaker"));
        Assert.That(service.ServiceType, Is.EqualTo("Matchmaker"));
        Assert.That(service.FileExtensions, Is.EqualTo(new[] { ".mme", ".mmq" }));
    }

    [Test]
    public void FetchAsync_ShouldThrowMatchmakerException_WhenAbortMessageIsNotEmpty()
    {
        var mockClient = new Mock<IConfigApiClient>();
        var mockFetchHandler = new Mock<IMatchmakerFetchHandler>();
        mockFetchHandler.Setup(m => m.FetchAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FetchResult { AbortMessage = "Abort" });
        var service = new MatchmakerFetchService(mockClient.Object, mockFetchHandler.Object);
        var fetchInput = new FetchInput();
        var filePaths = new List<string> { "test.mm" };
        var projectId = "testProjectId";
        var environmentId = "testEnvironmentId";
        var cancellationToken = new CancellationToken();

        Assert.ThrowsAsync<MatchmakerException>(async () => await service.FetchAsync(fetchInput, filePaths, projectId, environmentId, null, cancellationToken));
    }
}
