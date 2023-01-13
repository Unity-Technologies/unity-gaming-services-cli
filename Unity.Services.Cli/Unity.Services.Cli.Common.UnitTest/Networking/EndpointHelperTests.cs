using System.Reflection;
using NUnit.Framework;
using Unity.Services.Cli.Common.Networking;

namespace Unity.Services.Cli.Common.UnitTest;

[TestFixture]
class EndpointHelperTests
{
    [Test]
    public void InitializeNetworkTargetEndpointsFiltersInstantiableEndpointsTypesOnly()
    {
        var expectedEndpointsType = typeof(UnityServicesGatewayEndpoints);
        var types = new[]
        {
            typeof(EndpointHelper).GetTypeInfo(),
            typeof(int).GetTypeInfo(),
            typeof(NetworkTargetEndpoints).GetTypeInfo(),
            expectedEndpointsType.GetTypeInfo(),
        };

        EndpointHelper.InitializeNetworkTargetEndpoints(types);

        Assert.AreEqual(1, EndpointHelper.NetworkTargetEndpoints.Count);
        Assert.IsInstanceOf<UnityServicesGatewayEndpoints>(EndpointHelper.NetworkTargetEndpoints[expectedEndpointsType]);
    }

    [Test]
    public void InitializeNetworkTargetEndpointsClearsEndpointsMapAtEachCall()
    {
        var types = new List<TypeInfo>
        {
            typeof(UnityServicesGatewayEndpoints).GetTypeInfo(),
        };
        EndpointHelper.InitializeNetworkTargetEndpoints(types);
        CollectionAssert.IsNotEmpty(EndpointHelper.NetworkTargetEndpoints);

        types.Clear();
        EndpointHelper.InitializeNetworkTargetEndpoints(types);

        CollectionAssert.IsEmpty(EndpointHelper.NetworkTargetEndpoints);
    }
}
