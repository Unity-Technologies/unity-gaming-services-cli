using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Cli.MockServer.ServiceMocks;

namespace Unity.Services.Cli.IntegrationTest.PurchasingTests;

public class PurchasingBaseFixture : UgsCliFixture
{
    protected static readonly string TestDirectory =
        Path.Combine(UgsCliBuilder.RootDirectory, ".tmp", "PurchasingDir");

    protected static readonly string TestItemPath =
        Path.Combine(TestDirectory, "test-item.ucat");

    protected static readonly string TestCsvPath =
        Path.Combine(TestDirectory, "test-catalog.catalog.csv");

    protected const string k_TestCsvContent =
        "Sku,Title,Description,Language,ProductType,CurrencyCode,Amount\n" +
        "test-item,Title,Description,en-US,Consumable,USD,4.99\n";

    protected const string k_TestItemJson = """
        {
            "$schema": "https://ugs-config-schemas.unity3d.com/v1/purchasing/ucat.schema.json",
            "uSKU": "test-item",
            "type": "Consumable",
            "productDetails": [
                {
                    "title": "Title",
                    "description": "Description",
                    "language": "en-US"
                }
            ],
            "pricing": [
                {
                    "currencyCode": "USD",
                    "amount": 4.99
                }
            ]
        }
        """;

    [SetUp]
    public async Task SetUp()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();
        SetupProjectAndEnvironment();

        if (Directory.Exists(TestDirectory))
        {
            Directory.Delete(TestDirectory, true);
        }

        Directory.CreateDirectory(TestDirectory);

        MockApi.Server?.ResetMappings();
        await MockApi.MockServiceAsync(new IdentityV1Mock());
        await MockApi.MockServiceAsync(new PurchasingApiMock());
    }

    [TearDown]
    public void TearDown()
    {
        DeleteLocalConfig();
        DeleteLocalCredentials();

        if (Directory.Exists(TestDirectory))
        {
            Directory.Delete(TestDirectory, true);
        }
    }
}
