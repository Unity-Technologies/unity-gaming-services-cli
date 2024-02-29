using Moq;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Api;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Client;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Mocks;

class GameServerHostingBuildConfigurationsApiV1Mock
{
    readonly List<BuildConfigurationListItem> m_BuildConfigurationListItems = new()
    {
        new BuildConfigurationListItem(
            buildName: ValidBuildName,
            buildID: 1,
            fleetID: new Guid(ValidFleetId),
            fleetName: ValidFleetName,
            id: ValidBuildConfigurationId,
            name: ValidBuildConfigurationName,
            updatedAt: new DateTime(2022, 10, 11),
            createdAt: new DateTime(2022, 10, 11)
        )
    };

    readonly BuildConfiguration m_BuildConfiguration = new BuildConfiguration(
        binaryPath: "/path/to/simple-go-server",
        buildID: long.Parse(ValidBuildId),
        buildName: ValidBuildName,
        commandLine: "simple-go-server",
        configuration: new List<ConfigEntry>()
        {
            new ConfigEntry( 0, "key", "value"),
        },
        cores: 2L,
        createdAt: new DateTime(2022, 10, 11),
        fleetID: new Guid(ValidFleetId),
        fleetName: ValidFleetName,
        id: ValidBuildConfigurationId,
        memory: 800L,
        name: ValidBuildConfigurationName,
        queryType: "sqp",
        speed: 1200L,
        updatedAt: new DateTime(2022, 10, 11),
        version: 1L
    );

    public Mock<IBuildConfigurationsApi> DefaultBuildConfigurationsClient = new();

    public List<Guid>? ValidEnvironments;

    public List<Guid>? ValidProjects;

    public void SetUp()
    {
        DefaultBuildConfigurationsClient = new Mock<IBuildConfigurationsApi>();
        DefaultBuildConfigurationsClient.Setup(a => a.Configuration)
            .Returns(new Configuration());

        DefaultBuildConfigurationsClient.Setup(a =>
            a.GetBuildConfigurationAsync(
                It.IsAny<Guid>(), // projectId
                It.IsAny<Guid>(), // environmentId
                It.IsAny<long>(), // buildConfigurationId
                0,
                CancellationToken.None
            )).Returns((Guid projectId, Guid environmentId, long buildConfigurationId, int _, CancellationToken _) =>
        {
            var validated = ValidateProjectEnvironment(projectId, environmentId);
            if (!validated) throw new HttpRequestException();

            return Task.FromResult(m_BuildConfiguration);
        });

        DefaultBuildConfigurationsClient.Setup(a =>
            a.ListBuildConfigurationsAsync(
                It.IsAny<Guid>(), // projectId
                It.IsAny<Guid>(), // environmentId
                It.IsAny<Guid>(), // fleetId
                It.IsAny<string?>(),
                0,
                CancellationToken.None
            )).Returns((Guid projectId, Guid environmentId, Guid _, string? partialFilter, int _, CancellationToken _) =>
        {
            var validated = ValidateProjectEnvironment(projectId, environmentId);
            if (!validated) throw new HttpRequestException();
            var results = m_BuildConfigurationListItems.AsEnumerable();
            if (partialFilter != null)
            {
                results = results.Where(
                    a =>
                    {
                        var id = a.Id.ToString().Contains(partialFilter);
                        var name = a.Name.Contains(partialFilter);
                        return id || name;
                    }
                );
            }
            return Task.FromResult(results.ToList());
        });

        DefaultBuildConfigurationsClient.Setup(
            a =>
                a.CreateBuildConfigurationAsync(
                    It.IsAny<Guid>(), // projectId
                    It.IsAny<Guid>(), // environmentId
                    It.IsAny<BuildConfigurationCreateRequest>(),
                    0,
                    CancellationToken.None
            )).Returns((Guid projectId, Guid environmentId, BuildConfigurationCreateRequest createReq, int _, CancellationToken _) =>
        {
            var validated = ValidateProjectEnvironment(projectId, environmentId);
            if (!validated) throw new HttpRequestException();

            var buildConfig = new BuildConfiguration(
                binaryPath: ValidBuildConfigurationBinaryPath,
                buildID: long.Parse(ValidBuildId),
                buildName: ValidBuildName,
                commandLine: ValidBuildConfigurationCommandLine,
                configuration: new List<ConfigEntry>(),
                name: ValidBuildConfigurationName,
                queryType: ValidBuildConfigurationQueryType
            );
            return Task.FromResult(buildConfig);
        });

        DefaultBuildConfigurationsClient.Setup(a =>
              a.DeleteBuildConfigurationAsync(
                  It.IsAny<Guid>(), // projectId
                  It.IsAny<Guid>(), // environmentId
                  It.IsAny<long>(), // buildConfigurationId
                  null,
                  0,
                  CancellationToken.None
              )).Returns((Guid projectId, Guid environmentId, long buildConfigurationId, bool _, int _, CancellationToken _) =>
          {
              var validated = ValidateProjectEnvironment(projectId, environmentId);
              if (!validated) throw new HttpRequestException();

              return Task.CompletedTask;
          });

        DefaultBuildConfigurationsClient.Setup(
            a =>
                a.UpdateBuildConfigurationAsync(
                    It.IsAny<Guid>(), // projectId
                    It.IsAny<Guid>(), // environmentId
                    It.IsAny<long>(), // buildConfigurationId
                    It.IsAny<BuildConfigurationUpdateRequest>(),
                    0,
                    CancellationToken.None
                )).Returns((Guid projectId, Guid environmentId, long buildId, BuildConfigurationUpdateRequest _, int _, CancellationToken _) =>
        {
            var validated = ValidateProjectEnvironment(projectId, environmentId);
            if (!validated) throw new HttpRequestException();

            return Task.FromResult(m_BuildConfiguration);
        });
    }

    bool ValidateProjectEnvironment(Guid projectId, Guid environmentId)
    {
        if (ValidProjects != null && !ValidProjects.Contains(projectId)) return false;
        if (ValidEnvironments != null && !ValidEnvironments.Contains(environmentId)) return false;
        return true;
    }
}
