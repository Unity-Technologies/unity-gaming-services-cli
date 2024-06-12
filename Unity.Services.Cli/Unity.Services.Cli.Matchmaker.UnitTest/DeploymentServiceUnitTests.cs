using NUnit.Framework;
using Moq;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Matchmaker.Service;
using Unity.Services.Matchmaker.Authoring.Core.ConfigApi;
using Unity.Services.Matchmaker.Authoring.Core.Deploy;
using Unity.Services.Matchmaker.Authoring.Core.Model;
using Unity.Services.Cli.GameServerHosting.Services;
using Unity.Services.Cli.Matchmaker.UnitTest.SampleConfigs;

namespace Unity.Services.Cli.Matchmaker.UnitTest;

[TestFixture]
public class DeploymentServiceUnitTests
{
    [Test]
    public async Task Deploy_ShouldReturnDeploymentResult_WhenCalledWithValidParameters()
    {
        // Arrange
        var multiplaySampleConfig = new MultiplaySampleConfig();
        var resource = new MatchmakerConfigResource() { Name = "Test", Path = "TestPath.mmq" };
        var mockClient = new Mock<IConfigApiClient>();
        var mockGshConfigLoader = new Mock<IGameServerHostingConfigLoader>();
        mockGshConfigLoader.Setup(f => f.LoadAndValidateAsync(It.IsAny<List<string>>(), default)).Returns(Task.FromResult(multiplaySampleConfig.LocalConfigs));
        var mockDeploymentHandler = new Mock<IMatchmakerDeployHandler>();
        mockDeploymentHandler.Setup(m => m.DeployAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<MultiplayResources>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeployResult()
            {
                Created = { resource },
                Updated = { resource },
                Authored = { resource },
                Failed = { resource },
                Deleted = { resource }
            });
        var service = new MatchmakerDeploymentService(mockClient.Object, mockDeploymentHandler.Object, mockGshConfigLoader.Object);
        var deployInput = new DeployInput();
        var filePaths = new List<string> { "test.mmq" };
        var projectId = "testProjectId";
        var environmentId = "testEnvironmentId";
        var cancellationToken = new CancellationToken();

        // Act
        var result = await service.Deploy(deployInput, filePaths, projectId, environmentId, null, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.That(service.ServiceName, Is.EqualTo("matchmaker"));
        Assert.That(service.ServiceType, Is.EqualTo("Matchmaker"));
        Assert.That(service.FileExtensions, Is.EqualTo(new[] { ".mme", ".mmq" }));
    }

    [Test]
    public void Deploy_ShouldThrowMatchmakerException_WhenAbortMessageIsNotEmpty()
    {
        // Arrange
        var multiplaySampleConfig = new MultiplaySampleConfig();
        var mockClient = new Mock<IConfigApiClient>();
        var mockGshConfigLoader = new Mock<IGameServerHostingConfigLoader>();
        mockGshConfigLoader.Setup(f => f.LoadAndValidateAsync(It.IsAny<List<string>>(), default)).Returns(Task.FromResult(multiplaySampleConfig.LocalConfigs));

        var mockDeploymentHandler = new Mock<IMatchmakerDeployHandler>();
        mockDeploymentHandler.Setup(m => m.DeployAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<MultiplayResources>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeployResult { AbortMessage = "Abort" });
        var service = new MatchmakerDeploymentService(mockClient.Object, mockDeploymentHandler.Object, mockGshConfigLoader.Object);
        var deployInput = new DeployInput();
        var filePaths = new List<string> { "test.mmq" };
        var projectId = "testProjectId";
        var environmentId = "testEnvironmentId";
        var cancellationToken = new CancellationToken();

        // Act & Assert
        Assert.ThrowsAsync<MatchmakerException>(async () => await service.Deploy(deployInput, filePaths, projectId, environmentId, null, cancellationToken));
    }
}
