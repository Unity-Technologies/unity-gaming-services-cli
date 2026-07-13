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

    [Test]
    public void SetPlayerDataItemCommandWithInput()
    {
        CloudSaveModule module = new();

        Assert.That(module.SetPlayerDataItemCommand.Options, Does.Contain(CommonInput.CloudProjectIdOption));
        Assert.That(module.SetPlayerDataItemCommand.Options, Does.Contain(CommonInput.EnvironmentNameOption));
        Assert.That(module.SetPlayerDataItemCommand.Options, Does.Contain(SetPlayerItemInput.PlayerIdValue));
        Assert.That(module.SetPlayerDataItemCommand.Options, Does.Contain(SetPlayerItemInput.KeyValue));
        Assert.That(module.SetPlayerDataItemCommand.Options, Does.Contain(SetPlayerItemInput.ValueValue));
        Assert.That(module.SetPlayerDataItemCommand.Options, Does.Contain(SetPlayerItemInput.WriteLockValue));
        Assert.That(module.SetPlayerDataItemCommand.Options, Does.Contain(SetPlayerItemInput.VisibilityOption));
    }

    [Test]
    public void SetCustomDataItemCommandWithInput()
    {
        CloudSaveModule module = new();

        Assert.That(module.SetCustomDataItemCommand.Options, Does.Contain(CommonInput.CloudProjectIdOption));
        Assert.That(module.SetCustomDataItemCommand.Options, Does.Contain(CommonInput.EnvironmentNameOption));
        Assert.That(module.SetCustomDataItemCommand.Options, Does.Contain(SetCustomItemInput.CustomIdValue));
        Assert.That(module.SetCustomDataItemCommand.Options, Does.Contain(SetCustomItemInput.KeyValue));
        Assert.That(module.SetCustomDataItemCommand.Options, Does.Contain(SetCustomItemInput.ValueValue));
        Assert.That(module.SetCustomDataItemCommand.Options, Does.Contain(SetCustomItemInput.WriteLockValue));
        Assert.That(module.SetCustomDataItemCommand.Options, Does.Contain(SetCustomItemInput.VisibilityOption));
    }

    [Test]
    public void GetPlayerDataItemsCommandWithInput()
    {
        CloudSaveModule module = new();

        Assert.That(module.GetPlayerDataItemsCommand.Options, Does.Contain(CommonInput.CloudProjectIdOption));
        Assert.That(module.GetPlayerDataItemsCommand.Options, Does.Contain(CommonInput.EnvironmentNameOption));
        Assert.That(module.GetPlayerDataItemsCommand.Options, Does.Contain(GetPlayerItemsInput.PlayerIdValue));
        Assert.That(module.GetPlayerDataItemsCommand.Options, Does.Contain(GetPlayerItemsInput.KeysValue));
        Assert.That(module.GetPlayerDataItemsCommand.Options, Does.Contain(GetPlayerItemsInput.AfterValue));
        Assert.That(module.GetPlayerDataItemsCommand.Options, Does.Contain(GetPlayerItemsInput.VisibilityOption));
    }

    [TestCase(typeof(ICloudSaveDataService))]
    public void ConfigureCloudSaveRegistersExpectedServices(Type serviceType)
    {
        EndpointHelper.InitializeNetworkTargetEndpoints(
        [
            new CloudSaveEndpoints()
        ]);

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
