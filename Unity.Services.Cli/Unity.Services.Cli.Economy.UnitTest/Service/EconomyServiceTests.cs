using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.Economy.Service;
using Unity.Services.Cli.Economy.UnitTest.Mock;
using Unity.Services.Cli.Economy.UnitTest.Utils;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Gateway.EconomyApiV2.Generated.Api;
using Unity.Services.Gateway.EconomyApiV2.Generated.Model;

namespace Unity.Services.Cli.Economy.UnitTest.Service;

[TestFixture]
public class EconomyServiceTests
{
    const string k_TestAccessToken = "test-token";
    const string k_InvalidProjectId = "invalidProject";
    const string k_InvalidEnvironmentId = "foo";

    readonly Mock<IConfigurationValidator> m_ValidatorObject = new();
    readonly Mock<IServiceAccountAuthenticationService> m_AuthenticationServiceObject = new();
    readonly EconomyApiV2AsyncMock m_EconomyApiV2AsyncMock = new();
    readonly Mock<IEconomyAdminApiAsync> m_DefaultApiAsyncObject = new();
    readonly Mock<ILogger>? m_MockLogger = new();

    List<GetResourcesResponseResultsInner>? m_ExpectedResources;

    EconomyService? m_EconomyService;

    [SetUp]
    public void SetUp()
    {
        m_DefaultApiAsyncObject.Reset();
        m_ValidatorObject.Reset();
        m_AuthenticationServiceObject.Reset();
        m_MockLogger.Reset();
        m_AuthenticationServiceObject.Setup(a => a.GetAccessTokenAsync(CancellationToken.None))
            .Returns(Task.FromResult(k_TestAccessToken));

        // Setup GetResources/GetPublished responses
        var currency = new CurrencyItemResponse(
            "id",
            "name",
            CurrencyItemResponse.TypeEnum.CURRENCY,
            0,
            100,
            "custom data",
            new ModifiedMetadata(DateTime.Now),
            new ModifiedMetadata(DateTime.Now)
        );
        GetResourcesResponseResultsInner response = new GetResourcesResponseResultsInner(currency);
        m_ExpectedResources = new List<GetResourcesResponseResultsInner> { response };
        m_EconomyApiV2AsyncMock.GetResourcesResponse.Results = m_ExpectedResources;
        m_EconomyApiV2AsyncMock.GetPublishedResponse.Results = m_ExpectedResources;

        m_EconomyApiV2AsyncMock.SetUp();

        m_EconomyService = new EconomyService(
            m_EconomyApiV2AsyncMock.DefaultApiAsyncObject.Object,
            m_ValidatorObject.Object,
            m_AuthenticationServiceObject.Object);
    }

    [Test]
    public async Task AuthorizeEconomyService()
    {
        await m_EconomyService!.AuthorizeService(CancellationToken.None);
        m_AuthenticationServiceObject.Verify(a => a.GetAccessTokenAsync(CancellationToken.None));
        Assert.AreEqual(
            k_TestAccessToken.ToHeaderValue(),
            m_EconomyApiV2AsyncMock.DefaultApiAsyncObject.Object.Configuration.DefaultHeaders[
                AccessTokenHelper.HeaderKey]);
    }

    // Get resources tests -----------------------------------------------------
    [Test]
    public async Task GetResourcesAsync_EmptyConfigSuccess()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);
        m_ExpectedResources!.Clear();

        var actualResources = await m_EconomyService!.GetResourcesAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, CancellationToken.None);

        Assert.AreEqual(0, actualResources.Count);
    }

    [Test]
    public async Task GetResourcesAsync_WithValidParams_GetsExpectedResources()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        var actualResources = await m_EconomyService!.GetResourcesAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, CancellationToken.None);

        CollectionAssert.AreEqual(m_ExpectedResources, actualResources);
        m_EconomyApiV2AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetResourcesAsync(
                TestValues.ValidProjectId,
                Guid.Parse(TestValues.ValidEnvironmentId),
                null,
                null,
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public void GetResourcesAsync_InvalidProjectId_ThrowsConfigValidationException()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, k_InvalidProjectId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.ProjectId, k_InvalidProjectId, It.IsAny<string>()));

        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_EconomyService!.GetResourcesAsync(
                k_InvalidProjectId, TestValues.ValidEnvironmentId, CancellationToken.None));

        m_EconomyApiV2AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetResourcesAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void GetResourcesAsync_InvalidEnvironmentId_ThrowsConfigValidationException()
    {
        m_ValidatorObject.Setup(
                v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, k_InvalidEnvironmentId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.EnvironmentId, k_InvalidEnvironmentId, It.IsAny<string>()));

        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_EconomyService!.GetResourcesAsync(
                TestValues.ValidProjectId, k_InvalidEnvironmentId, CancellationToken.None));

        m_EconomyApiV2AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetResourcesAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Get published resources tests -----------------------------------------------------

    [Test]
    public async Task GetPublishedResourcesAsync_EmptyConfigSuccess()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);
        m_ExpectedResources!.Clear();

        var actualResources = await m_EconomyService!.GetPublishedAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, CancellationToken.None);

        Assert.AreEqual(0, actualResources.Count);
    }

    [Test]
    public async Task GetPublishedAsync_WithValidParams_GetsExpectedResources()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        var actualResources = await m_EconomyService!.GetPublishedAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, CancellationToken.None);

        CollectionAssert.AreEqual(m_ExpectedResources, actualResources);
        m_EconomyApiV2AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetPublishedResourcesAsync(
                TestValues.ValidProjectId,
                Guid.Parse(TestValues.ValidEnvironmentId),
                null,
                null,
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public void GetPublishedResourcesAsync_InvalidProjectId_ThrowsConfigValidationException()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, k_InvalidProjectId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.ProjectId, k_InvalidProjectId, It.IsAny<string>()));

        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_EconomyService!.GetPublishedAsync(
                k_InvalidProjectId, TestValues.ValidEnvironmentId, CancellationToken.None));

        m_EconomyApiV2AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetPublishedResourcesAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void GetPublishedResourcesAsync_InvalidEnvironmentId_ThrowsConfigValidationException()
    {
        m_ValidatorObject.Setup(
                v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, k_InvalidEnvironmentId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.EnvironmentId, k_InvalidEnvironmentId, It.IsAny<string>()));

        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_EconomyService!.GetPublishedAsync(
                TestValues.ValidProjectId, k_InvalidEnvironmentId, CancellationToken.None));

        m_EconomyApiV2AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.GetPublishedResourcesAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Publish tests -----------------------------------------------------

    [Test]
    public async Task PublishAsync_PublishesConfiguration()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        await m_EconomyService!.PublishAsync(TestValues.ValidProjectId, TestValues.ValidEnvironmentId, CancellationToken.None);

        m_EconomyApiV2AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.PublishEconomyAsync(
                TestValues.ValidProjectId,
                Guid.Parse(TestValues.ValidEnvironmentId),
                new PublishBody(true),
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public void PublishAsync_InvalidProjectId_ThrowsConfigValidationException()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, k_InvalidProjectId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.ProjectId, k_InvalidProjectId, It.IsAny<string>()));

        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_EconomyService!.PublishAsync(
                k_InvalidProjectId, TestValues.ValidEnvironmentId, CancellationToken.None));

        m_EconomyApiV2AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.PublishEconomyAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                new PublishBody(true),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void PublishAsync_InvalidEnvironmentId_ThrowsConfigValidationException()
    {
        m_ValidatorObject.Setup(
                v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, k_InvalidEnvironmentId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.EnvironmentId, k_InvalidEnvironmentId, It.IsAny<string>()));

        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_EconomyService!.PublishAsync(
                TestValues.ValidProjectId, k_InvalidEnvironmentId, CancellationToken.None));

        m_EconomyApiV2AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.PublishEconomyAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                new PublishBody(true),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Delete resource tests -----------------------------------------------------
    [Test]
    public async Task DeleteResourceAsync_WithValidId_DeletesResource()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        await m_EconomyService!.DeleteAsync("resource_id", TestValues.ValidProjectId, TestValues.ValidEnvironmentId, CancellationToken.None);

        m_EconomyApiV2AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.DeleteConfigResourceAsync(
                TestValues.ValidProjectId,
                Guid.Parse(TestValues.ValidEnvironmentId),
                "resource_id",
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public void DeleteResourceAsync_InvalidProjectId_ThrowsConfigValidationException()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, k_InvalidProjectId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.ProjectId, k_InvalidProjectId, It.IsAny<string>()));

        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_EconomyService!.DeleteAsync(
                "resource_id", k_InvalidProjectId, TestValues.ValidEnvironmentId, CancellationToken.None));

        m_EconomyApiV2AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.DeleteConfigResourceAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void DeleteResourceAsync_InvalidEnvironmentId_ThrowsConfigValidationException()
    {
        m_ValidatorObject.Setup(
                v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, k_InvalidEnvironmentId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.EnvironmentId, k_InvalidEnvironmentId, It.IsAny<string>()));

        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_EconomyService!.DeleteAsync(
                "resource_id", TestValues.ValidProjectId, k_InvalidEnvironmentId, CancellationToken.None));

        m_EconomyApiV2AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.DeleteConfigResourceAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Add resource tests -----------------------------------------------------

    [Test]
    public async Task AddResourceAsync_WithValidInput_AddsResource()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        CurrencyItemRequest currencyRequest = new CurrencyItemRequest("id", "name", CurrencyItemRequest.TypeEnum.CURRENCY);
        AddConfigResourceRequest addRequest = new AddConfigResourceRequest(currencyRequest);
        await m_EconomyService!.AddAsync(addRequest, TestValues.ValidProjectId, TestValues.ValidEnvironmentId, CancellationToken.None);

        m_EconomyApiV2AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.AddConfigResourceAsync(
                TestValues.ValidProjectId,
                Guid.Parse(TestValues.ValidEnvironmentId),
                addRequest,
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public void AddResourceAsync_InvalidProjectId_ThrowsConfigValidationException()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, k_InvalidProjectId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.ProjectId, k_InvalidProjectId, It.IsAny<string>()));

        CurrencyItemRequest currencyRequest = new CurrencyItemRequest("id", "name", CurrencyItemRequest.TypeEnum.CURRENCY);
        AddConfigResourceRequest addRequest = new AddConfigResourceRequest(currencyRequest);
        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_EconomyService!.AddAsync(
                addRequest, k_InvalidProjectId, TestValues.ValidEnvironmentId, CancellationToken.None));

        m_EconomyApiV2AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.AddConfigResourceAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<AddConfigResourceRequest>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void AddResourceAsync_InvalidEnvironmentId_ThrowsConfigValidationException()
    {
        m_ValidatorObject.Setup(
                v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, k_InvalidEnvironmentId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.EnvironmentId, k_InvalidEnvironmentId, It.IsAny<string>()));

        CurrencyItemRequest currencyRequest = new CurrencyItemRequest("id", "name", CurrencyItemRequest.TypeEnum.CURRENCY);
        AddConfigResourceRequest addRequest = new AddConfigResourceRequest(currencyRequest);
        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_EconomyService!.AddAsync(
                addRequest, TestValues.ValidProjectId, k_InvalidEnvironmentId, CancellationToken.None));

        m_EconomyApiV2AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.AddConfigResourceAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<AddConfigResourceRequest>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Edit resource tests -----------------------------------------------------

    [Test]
    public async Task EditResourceAsync_WithValidInput_AddsResource()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        CurrencyItemRequest currencyRequest = new CurrencyItemRequest("id", "name", CurrencyItemRequest.TypeEnum.CURRENCY);
        AddConfigResourceRequest addRequest = new AddConfigResourceRequest(currencyRequest);
        await m_EconomyService!.EditAsync("id", addRequest, TestValues.ValidProjectId, TestValues.ValidEnvironmentId, CancellationToken.None);

        m_EconomyApiV2AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.EditConfigResourceAsync(
                TestValues.ValidProjectId,
                Guid.Parse(TestValues.ValidEnvironmentId),
                "id",
                addRequest,
                0,
                CancellationToken.None),
            Times.Once);
    }

    [Test]
    public void EditResourceAsync_InvalidProjectId_ThrowsConfigValidationException()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, k_InvalidProjectId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.ProjectId, k_InvalidProjectId, It.IsAny<string>()));

        CurrencyItemRequest currencyRequest = new CurrencyItemRequest("id", "name", CurrencyItemRequest.TypeEnum.CURRENCY);
        AddConfigResourceRequest addRequest = new AddConfigResourceRequest(currencyRequest);
        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_EconomyService!.EditAsync(
                "id", addRequest, k_InvalidProjectId, TestValues.ValidEnvironmentId, CancellationToken.None));

        m_EconomyApiV2AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.EditConfigResourceAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<AddConfigResourceRequest>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void EditResourceAsync_InvalidEnvironmentId_ThrowsConfigValidationException()
    {
        m_ValidatorObject.Setup(
                v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, k_InvalidEnvironmentId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.EnvironmentId, k_InvalidEnvironmentId, It.IsAny<string>()));

        CurrencyItemRequest currencyRequest = new CurrencyItemRequest("id", "name", CurrencyItemRequest.TypeEnum.CURRENCY);
        AddConfigResourceRequest addRequest = new AddConfigResourceRequest(currencyRequest);
        Assert.ThrowsAsync<ConfigValidationException>(
            () => m_EconomyService!.EditAsync(
                "id", addRequest, TestValues.ValidProjectId, k_InvalidEnvironmentId, CancellationToken.None));

        m_EconomyApiV2AsyncMock.DefaultApiAsyncObject.Verify(
            a => a.EditConfigResourceAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<AddConfigResourceRequest>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
