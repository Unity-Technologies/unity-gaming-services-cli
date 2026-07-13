using NUnit.Framework;
using Unity.Services.Cli.Common.Networking;

namespace Unity.Services.Cli.Common.UnitTest;

[TestFixture]
class EndpointHelperTests
{
    [Test]
    public void InitializeNetworkTargetEndpointsRegistersEndpoints()
    {
        var endpoints = new NetworkTargetEndpoints[]
        {
            new UnityServicesGatewayEndpoints(),
        };

        EndpointHelper.InitializeNetworkTargetEndpoints(endpoints);

        Assert.AreEqual(1, EndpointHelper.NetworkTargetEndpoints.Count);
        Assert.IsInstanceOf<UnityServicesGatewayEndpoints>(
            EndpointHelper.NetworkTargetEndpoints[typeof(UnityServicesGatewayEndpoints)]);
    }

    [Test]
    public void InitializeNetworkTargetEndpointsClearsEndpointsMapAtEachCall()
    {
        EndpointHelper.InitializeNetworkTargetEndpoints(
            [new UnityServicesGatewayEndpoints()]);
        CollectionAssert.IsNotEmpty(EndpointHelper.NetworkTargetEndpoints);

        EndpointHelper.InitializeNetworkTargetEndpoints(
            Array.Empty<NetworkTargetEndpoints>());
        CollectionAssert.IsEmpty(EndpointHelper.NetworkTargetEndpoints);
    }
}
