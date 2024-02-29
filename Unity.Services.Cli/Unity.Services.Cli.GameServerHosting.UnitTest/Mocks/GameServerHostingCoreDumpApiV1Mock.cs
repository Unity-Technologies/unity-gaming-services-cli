using Moq;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Api;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Client;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Mocks;

public class GameServerHostingCoreDumpApiV1Mock
{
    public Mock<ICoreDumpApi> DefaultCoreDumpClient = new();

    List<Guid> ValidProjects { get; } = new()
    {
        Guid.Parse(ValidProjectId)
    };

    List<Guid> ValidEnvironments { get; } = new()
    {
        Guid.Parse(ValidEnvironmentId)
    };

    List<Guid> ValidFleets { get; } = new()
    {
        Guid.Parse(CoreDumpMockFleetIdWithoutConfig),
        Guid.Parse(CoreDumpMockFleetIdWithDisabledConfig),
        Guid.Parse(CoreDumpMockFleetIdWithEnabledConfig),
    };

    public void SetUp()
    {
        DefaultCoreDumpClient = new Mock<ICoreDumpApi>();
        DefaultCoreDumpClient.Setup(a => a.Configuration)
            .Returns(new Configuration());

        SetUpGetMethod();
        SetUpDeleteMethod();
        SetUpCreateMethod();
        SetUpUpdateMethod();
    }

    void SetUpGetMethod()
    {
        DefaultCoreDumpClient.Setup(
                a => a.GetCoreDumpConfigAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    0,
                    CancellationToken.None))
            .Returns(
                (
                    Guid projectId,
                    Guid environmentId,
                    Guid fleetId,
                    int timeout,
                    CancellationToken cancellationToken) =>
                {
                    if (!ValidateProjectEnvironmentFleet(projectId, environmentId, fleetId))
                    {
                        throw new ApiException(400, "Bad Request", $"{{\"detail\": \"validation error\"}}");
                    }

                    return fleetId.ToString() switch
                    {
                        CoreDumpMockFleetIdWithDisabledConfig => Task.FromResult(
                            new GetCoreDumpConfig200Response(
                                credentials: new CredentialsForTheBucket(storageBucket: "testBucket"),
                                fleetId: fleetId,
                                state: GetCoreDumpConfig200Response.StateEnum.NUMBER_0,
                                storageType: GetCoreDumpConfig200Response.StorageTypeEnum.Gcs,
                                updatedAt: DateTime.UtcNow)),
                        CoreDumpMockFleetIdWithEnabledConfig => Task.FromResult(
                            new GetCoreDumpConfig200Response(
                                credentials: new CredentialsForTheBucket(storageBucket: "testBucket"),
                                fleetId: fleetId,
                                state: GetCoreDumpConfig200Response.StateEnum.NUMBER_1,
                                storageType: GetCoreDumpConfig200Response.StorageTypeEnum.Gcs,
                                updatedAt: DateTime.UtcNow)),
                        CoreDumpMockFleetIdWithoutConfig => throw new ApiException(404, "Not found"),
                        _ => throw new ApiException(400, "Bad Request", $"{{\"detail\": \"something went wrong\"}}")
                    };
                });
    }

    void SetUpDeleteMethod()
    {
        DefaultCoreDumpClient.Setup(
                a => a.DeleteCoreDumpConfigAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    0,
                    CancellationToken.None))
            .Returns(
                (
                    Guid projectId,
                    Guid environmentId,
                    Guid fleetId,
                    int timeout,
                    CancellationToken cancellationToken) =>
                {
                    if (!ValidateProjectEnvironmentFleet(projectId, environmentId, fleetId))
                    {
                        throw new ApiException(400, "Bad Request", $"{{\"detail\": \"validation error\"}}");
                    }

                    return fleetId.ToString() switch
                    {
                        CoreDumpMockFleetIdWithDisabledConfig => Task.FromResult(""),
                        CoreDumpMockFleetIdWithEnabledConfig => Task.FromResult(""),
                        CoreDumpMockFleetIdWithoutConfig => throw new ApiException(404, "Not found"),
                        _ => throw new ApiException(400, "Bad Request", $"{{\"detail\": \"something went wrong\"}}")
                    };
                });
    }

    void SetUpCreateMethod()
    {
        DefaultCoreDumpClient.Setup(
                a => a.PostCoreDumpConfigAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CreateCoreDumpConfigRequest>(),
                    0,
                    CancellationToken.None))
            .Returns(
                (
                    Guid projectId,
                    Guid environmentId,
                    Guid fleetId,
                    CreateCoreDumpConfigRequest request,
                    int timeout,
                    CancellationToken cancellationToken) =>
                {
                    if (!ValidateProjectEnvironmentFleet(projectId, environmentId, fleetId))
                    {
                        throw new ApiException(400, "Bad Request");
                    }

                    if (fleetId.ToString() != CoreDumpMockFleetIdWithoutConfig)
                    {
                        throw new ApiException(
                            400,
                            "Bad request",
                            $"{{\"detail\": \"Core dump config already exists for fleet {fleetId}\"}}");
                    }

                    if (request.Credentials.StorageBucket != CoreDumpMockValidBucketName ||
                        request.Credentials.ServiceAccountAccessId != CoreDumpMockValidAccessId ||
                        request.Credentials.ServiceAccountPrivateKey != CoreDumpMockValidPrivateKey)
                    {
                        throw new ApiException(400, "Bad request", $"{{\"detail\": \"Invalid credentials\"}}");
                    }

                    return Task.FromResult(
                        new GetCoreDumpConfig200Response(
                            credentials: new CredentialsForTheBucket(storageBucket: request.Credentials.StorageBucket),
                            fleetId: fleetId,
                            state: (GetCoreDumpConfig200Response.StateEnum)request.State!,
                            storageType: (GetCoreDumpConfig200Response.StorageTypeEnum)request.StorageType,
                            updatedAt: DateTime.UtcNow));
                });
    }

    void SetUpUpdateMethod()
    {
        DefaultCoreDumpClient.Setup(
                a => a.PutCoreDumpConfigAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<UpdateCoreDumpConfigRequest>(),
                    0,
                    CancellationToken.None))
            .Returns(
                (
                    Guid projectId,
                    Guid environmentId,
                    Guid fleetId,
                    UpdateCoreDumpConfigRequest request,
                    int timeout,
                    CancellationToken cancellationToken) =>
                {
                    if (!ValidateProjectEnvironmentFleet(projectId, environmentId, fleetId))
                    {
                        throw new ApiException(400, "Bad Request");
                    }

                    if (fleetId.ToString() == CoreDumpMockFleetIdWithoutConfig)
                    {
                        throw new ApiException(
                            400,
                            "Bad request",
                            $"{{\"detail\": \"Core dump config does not exists for fleet {fleetId}\"}}");
                    }

                    if (request.Credentials.StorageBucket != CoreDumpMockValidBucketName ||
                        request.Credentials.ServiceAccountAccessId != CoreDumpMockValidAccessId ||
                        request.Credentials.ServiceAccountPrivateKey != CoreDumpMockValidPrivateKey)
                    {
                        throw new ApiException(400, "Bad request", $"{{\"detail\": \"Invalid credentials\"}}");
                    }

                    return Task.FromResult(
                        new GetCoreDumpConfig200Response(
                            credentials: new CredentialsForTheBucket(storageBucket: request.Credentials.StorageBucket),
                            fleetId: fleetId,
                            state: (GetCoreDumpConfig200Response.StateEnum)request.State!,
                            storageType: (GetCoreDumpConfig200Response.StorageTypeEnum)request.StorageType!,
                            updatedAt: DateTime.UtcNow));
                });
    }


    bool ValidateProjectEnvironmentFleet(Guid projectId, Guid environmentId, Guid fleetId)
    {
        return ValidProjects.Contains(projectId) &&
               ValidEnvironments.Contains(environmentId) &&
               ValidFleets.Contains(fleetId);
    }
}
