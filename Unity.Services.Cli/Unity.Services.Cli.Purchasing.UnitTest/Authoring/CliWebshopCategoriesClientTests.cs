using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Services.Cli.Purchasing.Authoring;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;

namespace Unity.Services.Cli.Purchasing.UnitTest.Authoring;

[TestFixture]
class CliWebshopCategoriesClientTests
{
    const string k_EnvironmentId = "env-123";
    const string k_ProjectId = "proj-456";
    const string k_SchemaBase = "https://services.api.unity.com/schema-registry";
    const string k_ExpectedSchema = k_SchemaBase + "/v1/schemas/UnityWebshopCategories/versions/1.1.0";

    Mock<ILiveContentApiTransport> m_MockTransport = null!;
    CliWebshopCategoriesClient m_Client = null!;

    [SetUp]
    public void SetUp()
    {
        m_MockTransport = new Mock<ILiveContentApiTransport>();
        m_MockTransport
            .Setup(t => t.InitializeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        m_Client = new CliWebshopCategoriesClient(m_MockTransport.Object, k_SchemaBase);
    }

    [Test]
    public async Task Initialize_DelegatesToTransport()
    {
        await m_Client.Initialize(k_EnvironmentId, k_ProjectId, CancellationToken.None);

        m_MockTransport.Verify(
            t => t.InitializeAsync(k_EnvironmentId, k_ProjectId, CancellationToken.None),
            Times.Once());
    }

    [Test]
    public async Task Get_Missing_ReturnsNull()
    {
        SetupGet(new TransportResult<LiveContentConfigBody>(404, error: "not found"));

        var result = await m_Client.Get(CancellationToken.None);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task Get_Existing_ReturnsParsedDoc()
    {
        SetupGet(new TransportResult<LiveContentConfigBody>(
            200, BuildContent(("starter", "Starter Pack"), ("vip", "VIP"))));

        var result = await m_Client.Get(CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Categories, Has.Count.EqualTo(2));
        Assert.That(result.Categories[0].Id, Is.EqualTo("starter"));
        Assert.That(result.Categories[0].Name["en"], Is.EqualTo("Starter Pack"));
        VerifyGetCalledWithCorrectPath();
    }

    [Test]
    public async Task Get_Empty_ReturnsEmptyDoc()
    {
        SetupGet(new TransportResult<LiveContentConfigBody>(200));

        var result = await m_Client.Get(CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Categories, Is.Null);
    }

    [Test]
    public void Get_NonSuccess_ThrowsClientException()
    {
        SetupGet(new TransportResult<LiveContentConfigBody>(403, error: "forbidden"));

        var ex = Assert.ThrowsAsync<ClientException>(() => m_Client.Get(CancellationToken.None));

        Assert.That(ex!.Message, Does.Contain("403"));
        Assert.That(ex.Message, Does.Contain("forbidden"));
    }

    [Test]
    public void Upsert_NullDoc_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => m_Client.Upsert(null!, CancellationToken.None));
    }

    [Test]
    public async Task Upsert_ExistingFile_UsesUpdate_WithCorrectBody()
    {
        SetupGetForUpsert();
        string? capturedBody = null;
        SetupUpdate(new TransportResult<LiveContentConfig>(200), body => capturedBody = body);

        await m_Client.Upsert(NewDoc(("starter", "Starter"), ("vip", "VIP")), CancellationToken.None);

        AssertBody(capturedBody, k_ExpectedSchema, "starter", "vip");
        m_MockTransport.Verify(
            t => t.UpdateConfigAsync(CliWebshopCategoriesClient.Path, It.IsAny<string>(), CancellationToken.None),
            Times.Once());
        m_MockTransport.Verify(
            t => t.CreateConfigAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Test]
    public async Task Upsert_PreservesExistingCustomMetadata()
    {
        var existingMetadata = new JObject
        {
            ["managedBy"] = "In App Purchase",
            ["customFilter"] = "storefront-promo"
        };
        SetupGetForUpsert(existingMetadata);
        string? capturedBody = null;
        SetupUpdate(new TransportResult<LiveContentConfig>(200), body => capturedBody = body);

        await m_Client.Upsert(NewDoc(("starter", "Starter")), CancellationToken.None);

        Assert.That(capturedBody, Is.Not.Null);
        var metadata = JObject.Parse(capturedBody!)["$metadata"]!;
        Assert.That((string?)metadata["managedBy"], Is.EqualTo("In App Purchase"));
        Assert.That((string?)metadata["customFilter"], Is.EqualTo("storefront-promo"));
    }

    [Test]
    public async Task Upsert_MissingFile_FallsBackToCreate()
    {
        SetupGetForUpsert();
        SetupUpdate(new TransportResult<LiveContentConfig>(404, error: "not found"));
        string? capturedBody = null;
        SetupCreate(new TransportResult<LiveContentConfig>(200), body => capturedBody = body);

        await m_Client.Upsert(NewDoc(("starter", "Starter")), CancellationToken.None);

        AssertBody(capturedBody, k_ExpectedSchema, "starter");
        m_MockTransport.Verify(
            t => t.UpdateConfigAsync(CliWebshopCategoriesClient.Path, It.IsAny<string>(), CancellationToken.None),
            Times.Once());
        m_MockTransport.Verify(
            t => t.CreateConfigAsync(CliWebshopCategoriesClient.Path, It.IsAny<string>(), CancellationToken.None),
            Times.Once());
    }

    [Test]
    public void Upsert_ServerRejects_ThrowsClientException()
    {
        SetupGetForUpsert();
        SetupUpdate(new TransportResult<LiveContentConfig>(400, error: "invalid body"));

        var ex = Assert.ThrowsAsync<ClientException>(
            () => m_Client.Upsert(NewDoc(("starter", "Starter")), CancellationToken.None));

        Assert.That(ex!.Message, Does.Contain("400"));
        Assert.That(ex.Message, Does.Contain("invalid body"));
    }

    [Test]
    public void Get_Cancelled_ThrowsOperationCanceledException()
    {
        m_MockTransport
            .Setup(t => t.GetConfigContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        Assert.ThrowsAsync<OperationCanceledException>(() => m_Client.Get(CancellationToken.None));
    }

    [Test]
    public void Upsert_Cancelled_ThrowsOperationCanceledException()
    {
        SetupGetForUpsert();
        m_MockTransport
            .Setup(t => t.UpdateConfigAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        Assert.ThrowsAsync<OperationCanceledException>(
            () => m_Client.Upsert(NewDoc(("starter", "Starter")), CancellationToken.None));
    }

    [Test]
    public void Upsert_FallbackToCreate_CreateFails_ThrowsClientException()
    {
        SetupGetForUpsert();
        SetupUpdate(new TransportResult<LiveContentConfig>(404, error: "not found"));
        SetupCreate(new TransportResult<LiveContentConfig>(500, error: "internal error"), _ => { });

        var ex = Assert.ThrowsAsync<ClientException>(
            () => m_Client.Upsert(NewDoc(("starter", "Starter")), CancellationToken.None));

        Assert.That(ex!.Message, Does.Contain("500"));
        Assert.That(ex.Message, Does.Contain("internal error"));
    }

    static WebshopCategories NewDoc(params (string id, string enName)[] entries)
    {
        var doc = new WebshopCategories { Categories = new List<WebshopCategory>() };
        foreach (var (id, enName) in entries)
        {
            doc.Categories.Add(new WebshopCategory
            {
                Id = id,
                Name = new Dictionary<string, string> { ["en"] = enName },
            });
        }
        return doc;
    }

    static LiveContentConfigBody BuildContent(params (string id, string enName)[] entries)
    {
        var categories = new JArray();
        foreach (var (id, enName) in entries)
        {
            categories.Add(new JObject
            {
                ["id"] = id,
                ["name"] = new JObject { ["en"] = enName },
            });
        }
        return new LiveContentConfigBody(new JObject { ["categories"] = categories });
    }

    void SetupGetForUpsert(JObject? metadata = null)
    {
        var content = new JObject();
        if (metadata != null)
            content["$metadata"] = metadata;
        SetupGet(new TransportResult<LiveContentConfigBody>(200, new LiveContentConfigBody(content)));
    }

    void SetupGet(TransportResult<LiveContentConfigBody> result)
    {
        m_MockTransport
            .Setup(t => t.GetConfigContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(result));
    }

    void SetupUpdate(TransportResult<LiveContentConfig> result, Action<string>? onCall = null)
    {
        m_MockTransport
            .Setup(t => t.UpdateConfigAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, body, _) => onCall?.Invoke(body))
            .Returns(Task.FromResult(result));
    }

    void SetupCreate(TransportResult<LiveContentConfig> result, Action<string> onCall)
    {
        m_MockTransport
            .Setup(t => t.CreateConfigAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, body, _) => onCall(body))
            .Returns(Task.FromResult(result));
    }

    void VerifyGetCalledWithCorrectPath()
    {
        m_MockTransport.Verify(
            t => t.GetConfigContentAsync(CliWebshopCategoriesClient.Path, CancellationToken.None),
            Times.Once());
    }

    static void AssertBody(string? body, string schema, params string[] categoryIds)
    {
        Assert.That(body, Is.Not.Null);
        var json = JObject.Parse(body!);
        CollectionAssert.AreEqual(new[] { schema }, json["$schema"]!.Values<string>());
        Assert.That((string?)json["$metadata"]!["managedBy"], Is.EqualTo("In App Purchase"));
        CollectionAssert.AreEqual(categoryIds, json["categories"]!.Select(c => (string?)c["id"]));
    }
}
