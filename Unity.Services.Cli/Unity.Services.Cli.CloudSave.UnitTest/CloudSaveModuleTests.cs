using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.CloudSave.Input;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.CloudSave.Service;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Gateway.CloudSaveApiV1.Generated.Api;

namespace Unity.Services.Cli.CloudSave.UnitTest;

[TestFixture]
class CloudSaveModuleTests
{
    [Test]
    public void ListIndexesCommandWithInput()
    {
        CloudSaveModule module = new();

        Assert.That(module.ListIndexesCommand.Options, Does.Contain(CommonInput.CloudProjectIdOption));
        Assert.That(module.ListIndexesCommand.Options, Does.Contain(CommonInput.EnvironmentNameOption));
    }

    [Test]
    public void ListCustomIdsCommandWithInput()
    {
        CloudSaveModule module = new();

        Assert.That(module.ListCustomDataIdsCommand.Options, Does.Contain(CommonInput.CloudProjectIdOption));
        Assert.That(module.ListCustomDataIdsCommand.Options, Does.Contain(CommonInput.EnvironmentNameOption));
        Assert.That(module.ListCustomDataIdsCommand.Options, Does.Contain(ListDataIdsInput.LimitOption));
        Assert.That(module.ListCustomDataIdsCommand.Options, Does.Contain(ListDataIdsInput.StartOption));
    }

    public void ListPlayerIdsCommandWithInput()
    {
        CloudSaveModule module = new();

        Assert.That(module.ListPlayerDataIdsCommand.Options, Does.Contain(CommonInput.CloudProjectIdOption));
        Assert.That(module.ListPlayerDataIdsCommand.Options, Does.Contain(CommonInput.EnvironmentNameOption));
        Assert.That(module.ListPlayerDataIdsCommand.Options, Does.Contain(ListDataIdsInput.LimitOption));
        Assert.That(module.ListPlayerDataIdsCommand.Options, Does.Contain(ListDataIdsInput.StartOption));
    }

    [Test]
    public void QueryPlayerDataCommandWithInput()
    {
        CloudSaveModule module = new();

        Assert.That(module.QueryPlayerDataCommand.Options, Does.Contain(CommonInput.CloudProjectIdOption));
        Assert.That(module.QueryPlayerDataCommand.Options, Does.Contain(CommonInput.EnvironmentNameOption));
        Assert.That(module.QueryPlayerDataCommand.Options, Does.Contain(QueryDataInput.JsonFileOrBodyOption));
        Assert.That(module.QueryPlayerDataCommand.Options, Does.Contain(QueryDataInput.VisibilityOption));
    }

    [Test]
    public void QueryCustomDataCommandWithInput()
    {
        CloudSaveModule module = new();

        Assert.That(module.QueryCustomDataCommand.Options, Does.Contain(CommonInput.CloudProjectIdOption));
        Assert.That(module.QueryCustomDataCommand.Options, Does.Contain(CommonInput.EnvironmentNameOption));
        Assert.That(module.QueryCustomDataCommand.Options, Does.Contain(QueryDataInput.JsonFileOrBodyOption));
        Assert.That(module.QueryCustomDataCommand.Options, Does.Contain(QueryDataInput.VisibilityOption));
    }

    [Test]
    public void CreatePlayerIndexCommandWithInput()
    {
        CloudSaveModule module = new();

        Assert.That(module.CreatePlayerIndexCommand.Options, Does.Contain(CommonInput.CloudProjectIdOption));
        Assert.That(module.CreatePlayerIndexCommand.Options, Does.Contain(CommonInput.EnvironmentNameOption));
        Assert.That(module.CreatePlayerIndexCommand.Options, Does.Contain(CreateIndexInput.FieldsOption));
        Assert.That(module.CreatePlayerIndexCommand.Options, Does.Contain(CreateIndexInput.JsonFileOrBodyOption));
        Assert.That(module.CreatePlayerIndexCommand.Options, Does.Contain(CreateIndexInput.VisibilityOption));
    }

    [Test]
    public void CreateCustomIndexCommandWithInput()
    {
        CloudSaveModule module = new();

        Assert.That(module.CreateCustomIndexCommand.Options, Does.Contain(CommonInput.CloudProjectIdOption));
        Assert.That(module.CreateCustomIndexCommand.Options, Does.Contain(CommonInput.EnvironmentNameOption));
        Assert.That(module.CreateCustomIndexCommand.Options, Does.Contain(CreateIndexInput.FieldsOption));
        Assert.That(module.CreateCustomIndexCommand.Options, Does.Contain(CreateIndexInput.JsonFileOrBodyOption));
        Assert.That(module.CreateCustomIndexCommand.Options, Does.Contain(CreateIndexInput.VisibilityOption));
    }

    [TestCase(typeof(ICloudSaveDataService))]
    public void ConfigureCloudSaveRegistersExpectedServices(Type serviceType)
    {
        EndpointHelper.InitializeNetworkTargetEndpoints(new[]
        {
            typeof(CloudSaveEndpoints).GetTypeInfo()
        });

        var collection = new ServiceCollection();
        collection.AddSingleton(ServiceDescriptor.Singleton(new Mock<IDataApiAsync>().Object));
        collection.AddSingleton(ServiceDescriptor.Singleton(new Mock<IServiceAccountAuthenticationService>().Object));
        CloudSaveModule.RegisterServices(collection);
        Assert.That(collection.FirstOrDefault(c => c.ServiceType == serviceType), Is.Not.Null);
    }

    [Test]

    public void RetryAfterSleepDuration()
    {
        var response = new RestSharp.RestResponse();
        response.Headers = new List<RestSharp.HeaderParameter>()
        {
            new ("Retry-After", "1")
        };
        Polly.DelegateResult<RestSharp.RestResponse> res = new Polly.DelegateResult<RestSharp.RestResponse>(response);
        Assert.That(CloudSaveModule.RetryAfterSleepDuration(2, res, new Polly.Context()), Is.EqualTo(TimeSpan.FromSeconds(2)));
    }
}
