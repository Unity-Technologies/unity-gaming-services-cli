using System;
using System.Collections.Generic;
using System.Threading;
using Moq;
using Unity.Services.Gateway.EconomyApiV2.Generated.Api;
using Unity.Services.Gateway.EconomyApiV2.Generated.Model;

namespace Unity.Services.Cli.Economy.UnitTest.Mock;

class EconomyApiV2AsyncMock
{
    public Mock<IEconomyAdminApiAsync> DefaultApiAsyncObject = new();

    public GetResourcesResponse GetResourcesResponse { get; } = new(
        new List<GetResourcesResponseResultsInner>());

    public GetPublishedResourcesResponse GetPublishedResponse { get; } = new(
        new List<GetResourcesResponseResultsInner>());

    public void SetUp()
    {
        DefaultApiAsyncObject = new Mock<IEconomyAdminApiAsync>();

        DefaultApiAsyncObject.Setup(
                a => a.GetResourcesAsync(
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(GetResourcesResponse);

        DefaultApiAsyncObject.Setup(
                a => a.GetPublishedResourcesAsync(
                    It.IsAny<string>(),
                    It.IsAny<Guid>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int>(),
                    CancellationToken.None))
            .ReturnsAsync(GetPublishedResponse);

        DefaultApiAsyncObject.Setup(a => a.Configuration)
            .Returns(new Gateway.EconomyApiV2.Generated.Client.Configuration());
    }
}
