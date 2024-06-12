using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Validator;
using Unity.Services.Cli.ServiceAccountAuthentication;
using Unity.Services.Cli.ServiceAccountAuthentication.Token;
using Unity.Services.Cli.CloudSave.Service;
using Unity.Services.Cli.CloudSave.UnitTest.Utils;
using Unity.Services.Cli.CloudSave.Utils;
using Unity.Services.Gateway.CloudSaveApiV1.Generated.Api;
using Unity.Services.Gateway.CloudSaveApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudSave.UnitTest.Service;

[TestFixture]
class CloudSaveServiceTests
{
    const string k_TestAccessToken = "test-token";

    const string k_InvalidProjectId = "invalidProject";
    const string k_InvalidEnvironmentId = "foo";

    readonly Mock<IConfigurationValidator> m_ValidatorObject = new();
    readonly Mock<IServiceAccountAuthenticationService> m_AuthenticationServiceObject = new();
    readonly Mock<IDataApiAsync> m_DataApiAsyncMock = new();

    CloudSaveDataService? m_CloudSaveDataService;

    readonly QueryIndexBody m_ValidQueryIndexBody = new QueryIndexBody(
        new List<FieldFilter>()
            { new FieldFilter ("fieldFilter_key","fieldFilter_value", FieldFilter.OpEnum.EQ, true)},
        new List<string> { "returnKey1", "returnKey2" },
        5,
        10);

    readonly List<QueryIndexResponseResultsInner> m_ValidQueryResponse = new List<QueryIndexResponseResultsInner>()
    {
        new QueryIndexResponseResultsInner("id",
            new List<Item>() {
                new Item("key1", "value", "writelock", new ModifiedMetadata(DateTime.Now), new ModifiedMetadata(DateTime.Today))
            }
        )
    };

    static readonly List<IndexField> k_ValidIndexFields = new List<IndexField>()
    {
        new IndexField("key1", true),
        new IndexField("key2", false)
    };

    readonly CreateIndexBody m_ValidCreateIndexBody = new CreateIndexBody(
        new CreateIndexBodyIndexConfig(k_ValidIndexFields));

    readonly CreateIndexResponse m_ValidCreateIndexResponse = new CreateIndexResponse("id", IndexStatus.READY);

    [SetUp]
    public void SetUp()
    {
        m_ValidatorObject.Reset();
        m_AuthenticationServiceObject.Reset();
        m_AuthenticationServiceObject.Setup(a => a.GetAccessTokenAsync(CancellationToken.None))
            .Returns(Task.FromResult(k_TestAccessToken));

        m_DataApiAsyncMock.Reset();
        m_DataApiAsyncMock.Setup(a => a.Configuration)
            .Returns(new Gateway.CloudSaveApiV1.Generated.Client.Configuration());

        m_CloudSaveDataService = new CloudSaveDataService(
            m_DataApiAsyncMock.Object,
            m_ValidatorObject.Object,
            m_AuthenticationServiceObject.Object);
    }

    [Test]
    public async Task AuthorizeCloudSaveService()
    {
        await m_CloudSaveDataService!.AuthorizeServiceAsync(CancellationToken.None);
        m_AuthenticationServiceObject.Verify(a => a.GetAccessTokenAsync(CancellationToken.None));
        Assert.That(
            m_DataApiAsyncMock.Object.Configuration.DefaultHeaders[
                AccessTokenHelper.HeaderKey], Is.EqualTo(k_TestAccessToken.ToHeaderValue()));
    }

    [Test]
    public void InvalidProjectIdThrowConfigValidationException()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, k_InvalidProjectId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.EnvironmentId, k_InvalidEnvironmentId, It.IsAny<string>()));
        Assert.Throws<ConfigValidationException>(
            () => m_CloudSaveDataService!.ValidateProjectIdAndEnvironmentId(
                k_InvalidProjectId, TestValues.ValidEnvironmentId));
    }

    [Test]
    public void InvalidEnvironmentIdThrowConfigValidationException()
    {
        m_ValidatorObject.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.EnvironmentId, k_InvalidEnvironmentId))
            .Throws(new ConfigValidationException(Keys.ConfigKeys.EnvironmentId, k_InvalidEnvironmentId, It.IsAny<string>()));
        Assert.Throws<ConfigValidationException>(
            () => m_CloudSaveDataService!.ValidateProjectIdAndEnvironmentId(
                TestValues.ValidProjectId, k_InvalidEnvironmentId));
    }

    [Test]
    public async Task ListIndexesAsync_Success()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        // Setup ListIndexes response
        var result = new List<LiveIndexConfigInner>
        {
            new LiveIndexConfigInner(
                "testIndex1",
                LiveIndexConfigInner.EntityTypeEnum.Player,
                AccessClass.Default,
                IndexStatus.READY,
                new List<IndexField>()
                {
                    new IndexField("testIndexKey1", true)
                }
            ),
            new LiveIndexConfigInner(
                "testIndex2",
                LiveIndexConfigInner.EntityTypeEnum.Custom,
                AccessClass.Private,
                IndexStatus.BUILDING,
                new List<IndexField>()
                {
                    new IndexField("testIndexKey2", false)
                }
            ),
        };
        m_DataApiAsyncMock.Setup(
            t => t.ListIndexesAsync(
                It.Is<Guid>(id => id.ToString() == TestValues.ValidProjectId),
                It.Is<Guid>(id => id.ToString() == TestValues.ValidEnvironmentId),
                It.IsAny<int>(),
                CancellationToken.None)).ReturnsAsync(new GetIndexIdsResponse(result));

        var actual = await m_CloudSaveDataService!.ListIndexesAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, CancellationToken.None);

        m_DataApiAsyncMock.VerifyAll();
        Assert.That(actual.Indexes, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task ListCustomIdsAsync_Success()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        // Setup ListCustomIds response
        var result = new GetCustomIdsResponse(
            new List<GetCustomIdsResponseResultsInner>
            {
                new GetCustomIdsResponseResultsInner(
                    "testId1",
                    new AccessClassesWithMetadata
                    {
                        Private = new AccessClassMetadata
                        {
                            NumKeys = 1,
                            TotalSize = 100
                        },
                        Protected = new AccessClassMetadata
                        {
                            NumKeys = 2,
                            TotalSize = 200
                        }
                    }
                ),
                new GetCustomIdsResponseResultsInner(
                    "testId2",
                    new AccessClassesWithMetadata
                    {
                        Default = new AccessClassMetadata
                        {
                            NumKeys = 3,
                            TotalSize = 300
                        }
                    }
                ),
            },
            new GetPlayersWithDataResponseLinks("someLink")
        );
        m_DataApiAsyncMock.Setup(
            t => t.ListCustomDataIDsAsync(
                It.Is<Guid>(id => id.ToString() == TestValues.ValidProjectId),
                It.Is<Guid>(id => id.ToString() == TestValues.ValidEnvironmentId),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                CancellationToken.None)).ReturnsAsync(result);

        var actual = await m_CloudSaveDataService!.ListCustomDataIdsAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, "someStart", 2, CancellationToken.None);

        m_DataApiAsyncMock.VerifyAll();
        Assert.Multiple(() =>
        {
            Assert.That(actual.Results, Has.Count.EqualTo(2));
            Assert.That(result.Links.Next, Is.EqualTo(actual.Links.Next));
        });
    }

    [Test]
    public async Task ListPlayerIdsAsync_Success()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        // Setup ListCustomIds response
        var result = new GetPlayersWithDataResponse(
            new List<GetPlayersWithDataResponseResultsInner>
            {
                new GetPlayersWithDataResponseResultsInner(
                    "testId1",
                    new AccessClassesWithMetadata
                    {
                        Private = new AccessClassMetadata
                        {
                            NumKeys = 1,
                            TotalSize = 100
                        },
                        Protected = new AccessClassMetadata
                        {
                            NumKeys = 2,
                            TotalSize = 200
                        }
                    }
                ),
                new GetPlayersWithDataResponseResultsInner(
                    "testId2",
                    new AccessClassesWithMetadata
                    {
                        Default = new AccessClassMetadata
                        {
                            NumKeys = 3,
                            TotalSize = 300
                        }
                    }
                ),
            },
            new GetPlayersWithDataResponseLinks("someLink")
        );
        m_DataApiAsyncMock.Setup(
            t => t.GetPlayersWithItemsAsync(
                It.Is<Guid>(id => id.ToString() == TestValues.ValidProjectId),
                It.Is<Guid>(id => id.ToString() == TestValues.ValidEnvironmentId),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                CancellationToken.None)).ReturnsAsync(result);

        var actual = await m_CloudSaveDataService!.ListPlayerDataIdsAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, "someStart", 2, CancellationToken.None);

        m_DataApiAsyncMock.VerifyAll();
        Assert.Multiple(() =>
        {
            Assert.That(actual.Results, Has.Count.EqualTo(2));
            Assert.That(result.Links.Next, Is.EqualTo(actual.Links.Next));
        });
    }

    [Test]
    public async Task QueryPlayerData_Default_Success()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        m_DataApiAsyncMock.Setup(
            t => t.QueryDefaultPlayerDataAsync(
                It.Is<Guid>(id => id.ToString() == TestValues.ValidProjectId),
                It.Is<Guid>(id => id.ToString() == TestValues.ValidEnvironmentId),
                It.IsAny<QueryIndexBody>(),
                It.IsAny<int>(),
                CancellationToken.None)).ReturnsAsync(new QueryIndexResponse(m_ValidQueryResponse));

        var body = JsonConvert.SerializeObject(m_ValidQueryIndexBody);
        var visibility = PlayerIndexVisibilityTypes.Default;

        var actual = await m_CloudSaveDataService!.QueryPlayerDataAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, visibility, body, CancellationToken.None);

        m_DataApiAsyncMock.VerifyAll();
        Assert.That(actual.Results, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task QueryPlayerData_Default_FailsWithInvalidBody()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        var body = "somebadjson";
        var visibility = PlayerIndexVisibilityTypes.Public;

        try
        {
            var actual = await m_CloudSaveDataService!.QueryPlayerDataAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                visibility,
                body,
                CancellationToken.None);
            Assert.Fail(); // Should not get this far
        }
        catch (CliException e)
        {
            Assert.That(e.Message, Does.Contain("Failed to deserialize object for Cloud Save request."));
        }
    }

    [Test]
    public async Task QueryPlayerData_Protected_Success()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        m_DataApiAsyncMock.Setup(
            t => t.QueryProtectedPlayerDataAsync(
                It.Is<Guid>(id => id.ToString() == TestValues.ValidProjectId),
                It.Is<Guid>(id => id.ToString() == TestValues.ValidEnvironmentId),
                It.IsAny<QueryIndexBody>(),
                It.IsAny<int>(),
                CancellationToken.None)).ReturnsAsync(new QueryIndexResponse(m_ValidQueryResponse));

        var body = JsonConvert.SerializeObject(m_ValidQueryIndexBody);
        var visibility = PlayerIndexVisibilityTypes.Protected;

        var actual = await m_CloudSaveDataService!.QueryPlayerDataAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, visibility, body, CancellationToken.None);

        m_DataApiAsyncMock.VerifyAll();
        Assert.That(actual.Results, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task QueryPlayerData_Protected_FailsWithInvalidBody()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        var body = "somebadjson";
        var visibility = PlayerIndexVisibilityTypes.Public;

        try
        {
            var actual = await m_CloudSaveDataService!.QueryPlayerDataAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                visibility,
                body,
                CancellationToken.None);
            Assert.Fail(); // Should not get this far
        }
        catch (CliException e)
        {
            Assert.That(e.Message, Does.Contain("Failed to deserialize object for Cloud Save request."));
        }
    }

    [Test]
    public async Task QueryPlayerData_Public_Success()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        m_DataApiAsyncMock.Setup(
            t => t.QueryPublicPlayerDataAsync(
                It.Is<Guid>(id => id.ToString() == TestValues.ValidProjectId),
                It.Is<Guid>(id => id.ToString() == TestValues.ValidEnvironmentId),
                It.IsAny<QueryIndexBody>(),
                It.IsAny<int>(),
                CancellationToken.None)).ReturnsAsync(new QueryIndexResponse(m_ValidQueryResponse));

        var body = JsonConvert.SerializeObject(m_ValidQueryIndexBody);
        var visibility = PlayerIndexVisibilityTypes.Public;

        var actual = await m_CloudSaveDataService!.QueryPlayerDataAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, visibility, body, CancellationToken.None);

        m_DataApiAsyncMock.VerifyAll();
        Assert.That(actual.Results, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task QueryPlayerData_Public_FailsWithInvalidBody()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        var body = "somebadjson";
        var visibility = PlayerIndexVisibilityTypes.Public;
        try
        {
            var actual = await m_CloudSaveDataService!.QueryPlayerDataAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                visibility,
                body,
                CancellationToken.None);
            Assert.Fail(); // Should not get this far
        }
        catch (CliException e)
        {
            Assert.That(e.Message, Does.Contain("Failed to deserialize object for Cloud Save request."));
        }
    }

    [Test]
    public async Task QueryCustomData_Default_Success()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        m_DataApiAsyncMock.Setup(
            t => t.QueryDefaultCustomDataAsync(
                It.Is<Guid>(id => id.ToString() == TestValues.ValidProjectId),
                It.Is<Guid>(id => id.ToString() == TestValues.ValidEnvironmentId),
                It.IsAny<QueryIndexBody>(),
                It.IsAny<int>(),
                CancellationToken.None)).ReturnsAsync(new QueryIndexResponse(m_ValidQueryResponse));

        var body = JsonConvert.SerializeObject(m_ValidQueryIndexBody);
        var visibility = CustomIndexVisibilityTypes.Default;

        var actual = await m_CloudSaveDataService!.QueryCustomDataAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, visibility, body, CancellationToken.None);

        m_DataApiAsyncMock.VerifyAll();
        Assert.That(actual.Results, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task QueryCustomData_Default_FailsWithInvalidBody()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        var body = "somebadjson";
        var visibility = CustomIndexVisibilityTypes.Default;

        try
        {
            var actual = await m_CloudSaveDataService!.QueryCustomDataAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                visibility,
                body,
                CancellationToken.None);
            Assert.Fail(); // Should not get this far
        }
        catch (CliException e)
        {
            Assert.That(e.Message, Does.Contain("Failed to deserialize object for Cloud Save request."));
        }
    }

    [Test]
    public async Task QueryCustomData_Private_Success()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        m_DataApiAsyncMock.Setup(
            t => t.QueryPrivateCustomDataAsync(
                It.Is<Guid>(id => id.ToString() == TestValues.ValidProjectId),
                It.Is<Guid>(id => id.ToString() == TestValues.ValidEnvironmentId),
                It.IsAny<QueryIndexBody>(),
                It.IsAny<int>(),
                CancellationToken.None)).ReturnsAsync(new QueryIndexResponse(m_ValidQueryResponse));

        var body = JsonConvert.SerializeObject(m_ValidQueryIndexBody);
        var visibility = CustomIndexVisibilityTypes.Private;

        var actual = await m_CloudSaveDataService!.QueryCustomDataAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, visibility, body, CancellationToken.None);

        m_DataApiAsyncMock.VerifyAll();
        Assert.That(actual.Results, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task QueryCustomData_Private_FailsWithInvalidBody()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        var body = "somebadjson";
        var visibility = CustomIndexVisibilityTypes.Private;

        try
        {
            var actual = await m_CloudSaveDataService!.QueryCustomDataAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                visibility,
                body,
                CancellationToken.None);
            Assert.Fail(); // Should not get this far
        }
        catch (CliException e)
        {
            Assert.That(e.Message, Does.Contain("Failed to deserialize object for Cloud Save request."));
        }
    }

    [Test]
    public async Task CreatePlayerIndex_Default_SerializedBody_Success()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        m_DataApiAsyncMock.Setup(
            t => t.CreateDefaultPlayerIndexAsync(
                It.Is<Guid>(id => id.ToString() == TestValues.ValidProjectId),
                It.Is<Guid>(id => id.ToString() == TestValues.ValidEnvironmentId),
                It.IsAny<CreateIndexBody>(),
                It.IsAny<int>(),
                CancellationToken.None)).ReturnsAsync(m_ValidCreateIndexResponse);

        var body = JsonConvert.SerializeObject(m_ValidCreateIndexBody);
        var visibility = PlayerIndexVisibilityTypes.Default;

        var actual = await m_CloudSaveDataService!.CreatePlayerIndexAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, null, visibility, body, CancellationToken.None);

        m_DataApiAsyncMock.VerifyAll();
        Assert.Multiple(() =>
        {
            Assert.That(actual.Id, Is.EqualTo(m_ValidCreateIndexResponse.Id));
            Assert.That(actual.Status, Is.EqualTo(m_ValidCreateIndexResponse.Status));
        });
    }

    [Test]
    public async Task CreatePlayerIndex_Default_SerializedFields_Success()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        m_DataApiAsyncMock.Setup(
            t => t.CreateDefaultPlayerIndexAsync(
                It.Is<Guid>(id => id.ToString() == TestValues.ValidProjectId),
                It.Is<Guid>(id => id.ToString() == TestValues.ValidEnvironmentId),
                It.IsAny<CreateIndexBody>(),
                It.IsAny<int>(),
                CancellationToken.None)).ReturnsAsync(m_ValidCreateIndexResponse);

        var fields = JsonConvert.SerializeObject(k_ValidIndexFields);
        var visibility = PlayerIndexVisibilityTypes.Default;

        var actual = await m_CloudSaveDataService!.CreatePlayerIndexAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, fields, visibility, null, CancellationToken.None);

        m_DataApiAsyncMock.VerifyAll();
        Assert.Multiple(() =>
        {
            Assert.That(actual.Id, Is.EqualTo(m_ValidCreateIndexResponse.Id));
            Assert.That(actual.Status, Is.EqualTo(m_ValidCreateIndexResponse.Status));
        });
    }

    [Test]
    public async Task CreatePlayerIndex_Public_SerializedBody_Success()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        m_DataApiAsyncMock.Setup(
            t => t.CreatePublicPlayerIndexAsync(
                It.Is<Guid>(id => id.ToString() == TestValues.ValidProjectId),
                It.Is<Guid>(id => id.ToString() == TestValues.ValidEnvironmentId),
                It.IsAny<CreateIndexBody>(),
                It.IsAny<int>(),
                CancellationToken.None)).ReturnsAsync(m_ValidCreateIndexResponse);

        var body = JsonConvert.SerializeObject(m_ValidCreateIndexBody);
        var visibility = PlayerIndexVisibilityTypes.Public;

        var actual = await m_CloudSaveDataService!.CreatePlayerIndexAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, null, visibility, body, CancellationToken.None);

        m_DataApiAsyncMock.VerifyAll();
        Assert.Multiple(() =>
        {
            Assert.That(actual.Id, Is.EqualTo(m_ValidCreateIndexResponse.Id));
            Assert.That(actual.Status, Is.EqualTo(m_ValidCreateIndexResponse.Status));
        });
    }

    [Test]
    public async Task CreatePlayerIndex_Public_SerializedFields_Success()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        m_DataApiAsyncMock.Setup(
            t => t.CreatePublicPlayerIndexAsync(
                It.Is<Guid>(id => id.ToString() == TestValues.ValidProjectId),
                It.Is<Guid>(id => id.ToString() == TestValues.ValidEnvironmentId),
                It.IsAny<CreateIndexBody>(),
                It.IsAny<int>(),
                CancellationToken.None)).ReturnsAsync(m_ValidCreateIndexResponse);

        var fields = JsonConvert.SerializeObject(k_ValidIndexFields);
        var visibility = PlayerIndexVisibilityTypes.Public;

        var actual = await m_CloudSaveDataService!.CreatePlayerIndexAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, fields, visibility, null, CancellationToken.None);

        m_DataApiAsyncMock.VerifyAll();
        Assert.Multiple(() =>
        {
            Assert.That(actual.Id, Is.EqualTo(m_ValidCreateIndexResponse.Id));
            Assert.That(actual.Status, Is.EqualTo(m_ValidCreateIndexResponse.Status));
        });
    }

    [Test]
    public async Task CreatePlayerIndex_Protected_SerializedBody_Success()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        m_DataApiAsyncMock.Setup(
            t => t.CreateProtectedPlayerIndexAsync(
                It.Is<Guid>(id => id.ToString() == TestValues.ValidProjectId),
                It.Is<Guid>(id => id.ToString() == TestValues.ValidEnvironmentId),
                It.IsAny<CreateIndexBody>(),
                It.IsAny<int>(),
                CancellationToken.None)).ReturnsAsync(m_ValidCreateIndexResponse);

        var body = JsonConvert.SerializeObject(m_ValidCreateIndexBody);
        var visibility = PlayerIndexVisibilityTypes.Protected;

        var actual = await m_CloudSaveDataService!.CreatePlayerIndexAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, null, visibility, body, CancellationToken.None);

        m_DataApiAsyncMock.VerifyAll();
        Assert.Multiple(() =>
        {
            Assert.That(actual.Id, Is.EqualTo(m_ValidCreateIndexResponse.Id));
            Assert.That(actual.Status, Is.EqualTo(m_ValidCreateIndexResponse.Status));
        });
    }

    [Test]
    public async Task CreatePlayerIndex_Protected_SerializedFields_Success()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        m_DataApiAsyncMock.Setup(
            t => t.CreateProtectedPlayerIndexAsync(
                It.Is<Guid>(id => id.ToString() == TestValues.ValidProjectId),
                It.Is<Guid>(id => id.ToString() == TestValues.ValidEnvironmentId),
                It.IsAny<CreateIndexBody>(),
                It.IsAny<int>(),
                CancellationToken.None)).ReturnsAsync(m_ValidCreateIndexResponse);

        var fields = JsonConvert.SerializeObject(k_ValidIndexFields);
        var visibility = PlayerIndexVisibilityTypes.Protected;

        var actual = await m_CloudSaveDataService!.CreatePlayerIndexAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, fields, visibility, null, CancellationToken.None);

        m_DataApiAsyncMock.VerifyAll();
        Assert.Multiple(() =>
        {
            Assert.That(actual.Id, Is.EqualTo(m_ValidCreateIndexResponse.Id));
            Assert.That(actual.Status, Is.EqualTo(m_ValidCreateIndexResponse.Status));
        });
    }

    [Test]
    public async Task CreatePlayerIndex_FailsWithInvalidBody()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        var body = "somebadjson";
        var visibility = PlayerIndexVisibilityTypes.Default;

        try
        {
            var actual = await m_CloudSaveDataService!.CreatePlayerIndexAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                null,
                visibility,
                body,
                CancellationToken.None);
            Assert.Fail(); // Should not get this far
        }
        catch (CliException e)
        {
            Assert.That(e.Message, Does.Contain("Failed to deserialize object for Cloud Save request."));
        }
    }

    [Test]
    public async Task CreatePlayerIndex_FailsWithBothBodyAndFieldsSpecified()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        var body = JsonConvert.SerializeObject(m_ValidCreateIndexBody);
        var fields = JsonConvert.SerializeObject(k_ValidIndexFields);
        var visibility = CustomIndexVisibilityTypes.Default;

        try
        {
            var actual = await m_CloudSaveDataService!.CreatePlayerIndexAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                fields,
                visibility,
                body,
                CancellationToken.None);
            Assert.Fail(); // Should not get this far
        }
        catch (CliException e)
        {
            Assert.That(e.Message, Does.Contain("Index body and fields cannot both be specified."));
        }
    }

    [Test]
    public async Task CreateCustomIndex_Default_SerializedBody_Success()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        m_DataApiAsyncMock.Setup(
            t => t.CreateDefaultCustomIndexAsync(
                It.Is<Guid>(id => id.ToString() == TestValues.ValidProjectId),
                It.Is<Guid>(id => id.ToString() == TestValues.ValidEnvironmentId),
                It.IsAny<CreateIndexBody>(),
                It.IsAny<int>(),
                CancellationToken.None)).ReturnsAsync(m_ValidCreateIndexResponse);

        var body = JsonConvert.SerializeObject(m_ValidCreateIndexBody);
        var visibility = CustomIndexVisibilityTypes.Default;
        var actual = await m_CloudSaveDataService!.CreateCustomIndexAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, null, visibility, body, CancellationToken.None);

        m_DataApiAsyncMock.VerifyAll();
        Assert.Multiple(() =>
        {
            Assert.That(actual.Id, Is.EqualTo(m_ValidCreateIndexResponse.Id));
            Assert.That(actual.Status, Is.EqualTo(m_ValidCreateIndexResponse.Status));
        });
    }

    [Test]
    public async Task CreateCustomIndex_Default_SerializedFields_Success()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        m_DataApiAsyncMock.Setup(
            t => t.CreateDefaultCustomIndexAsync(
                It.Is<Guid>(id => id.ToString() == TestValues.ValidProjectId),
                It.Is<Guid>(id => id.ToString() == TestValues.ValidEnvironmentId),
                It.IsAny<CreateIndexBody>(),
                It.IsAny<int>(),
                CancellationToken.None)).ReturnsAsync(m_ValidCreateIndexResponse);

        var fields = JsonConvert.SerializeObject(k_ValidIndexFields);
        var visibility = CustomIndexVisibilityTypes.Default;

        var actual = await m_CloudSaveDataService!.CreateCustomIndexAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, fields, visibility, null, CancellationToken.None);

        m_DataApiAsyncMock.VerifyAll();
        Assert.Multiple(() =>
        {
            Assert.That(actual.Id, Is.EqualTo(m_ValidCreateIndexResponse.Id));
            Assert.That(actual.Status, Is.EqualTo(m_ValidCreateIndexResponse.Status));
        });
    }

    [Test]
    public async Task CreateCustomIndex_FailsWithInvalidBody()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        var body = "somebadjson";
        var visibility = CustomIndexVisibilityTypes.Default;

        try
        {
            var actual = await m_CloudSaveDataService!.CreateCustomIndexAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                null,
                visibility,
                body,
                CancellationToken.None);
            Assert.Fail(); // Should not get this far
        }
        catch (CliException e)
        {
            Assert.That(e.Message, Does.Contain("Failed to deserialize object for Cloud Save request."));
        }
    }

    [Test]
    public async Task CreateCustomIndex_FailsWithBothBodyAndFieldsSpecified()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        var body = JsonConvert.SerializeObject(m_ValidCreateIndexBody);
        var fields = JsonConvert.SerializeObject(k_ValidIndexFields);
        var visibility = CustomIndexVisibilityTypes.Default;

        try
        {
            var actual = await m_CloudSaveDataService!.CreateCustomIndexAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                fields,
                visibility,
                body,
                CancellationToken.None);
            Assert.Fail(); // Should not get this far
        }
        catch (CliException e)
        {
            Assert.That(e.Message, Does.Contain("Index body and fields cannot both be specified."));
        }
    }

    [Test]
    public async Task CreateCustomIndex_Private_SerializedBody_Success()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        m_DataApiAsyncMock.Setup(
            t => t.CreatePrivateCustomIndexAsync(
                It.Is<Guid>(id => id.ToString() == TestValues.ValidProjectId),
                It.Is<Guid>(id => id.ToString() == TestValues.ValidEnvironmentId),
                It.IsAny<CreateIndexBody>(),
                It.IsAny<int>(),
                CancellationToken.None)).ReturnsAsync(m_ValidCreateIndexResponse);

        var body = JsonConvert.SerializeObject(m_ValidCreateIndexBody);
        var visibility = CustomIndexVisibilityTypes.Private;

        var actual = await m_CloudSaveDataService!.CreateCustomIndexAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, null, visibility, body, CancellationToken.None);

        m_DataApiAsyncMock.VerifyAll();
        Assert.Multiple(() =>
        {
            Assert.That(actual.Id, Is.EqualTo(m_ValidCreateIndexResponse.Id));
            Assert.That(actual.Status, Is.EqualTo(m_ValidCreateIndexResponse.Status));
        });
    }

    [Test]
    public async Task CreateCustomIndex_Private_SerializedFields_Success()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        m_DataApiAsyncMock.Setup(
            t => t.CreatePrivateCustomIndexAsync(
                It.Is<Guid>(id => id.ToString() == TestValues.ValidProjectId),
                It.Is<Guid>(id => id.ToString() == TestValues.ValidEnvironmentId),
                It.IsAny<CreateIndexBody>(),
                It.IsAny<int>(),
                CancellationToken.None)).ReturnsAsync(m_ValidCreateIndexResponse);

        var fields = JsonConvert.SerializeObject(k_ValidIndexFields);
        var visibility = CustomIndexVisibilityTypes.Private;

        var actual = await m_CloudSaveDataService!.CreateCustomIndexAsync(
            TestValues.ValidProjectId, TestValues.ValidEnvironmentId, fields, visibility, null, CancellationToken.None);

        m_DataApiAsyncMock.VerifyAll();
        Assert.Multiple(() =>
        {
            Assert.That(actual.Id, Is.EqualTo(m_ValidCreateIndexResponse.Id));
            Assert.That(actual.Status, Is.EqualTo(m_ValidCreateIndexResponse.Status));
        });
    }

    [Test]
    public async Task CreateCustomIndex_Private_FailsWithInvalidBody()
    {
        string mockErrorMsg;
        m_ValidatorObject.Setup(v => v.IsConfigValid(It.IsAny<string>(), It.IsAny<string>(), out mockErrorMsg))
            .Returns(true);

        var body = "somebadjson";
        var visibility = CustomIndexVisibilityTypes.Default;

        try
        {
            var actual = await m_CloudSaveDataService!.CreateCustomIndexAsync(
                TestValues.ValidProjectId,
                TestValues.ValidEnvironmentId,
                null,
                visibility,
                body,
                CancellationToken.None);
            Assert.Fail(); // Should not get this far
        }
        catch (CliException e)
        {
            Assert.That(e.Message, Does.Contain("Failed to deserialize object for Cloud Save request."));
        }
    }
}
