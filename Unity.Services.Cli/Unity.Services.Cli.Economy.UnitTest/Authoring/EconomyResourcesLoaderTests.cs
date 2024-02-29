using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Services.Cli.Economy.Authoring;
using Unity.Services.Cli.Economy.Authoring.IO;
using Unity.Services.Cli.Economy.Model;
using Unity.Services.Cli.Economy.Templates;
using Unity.Services.Economy.Editor.Authoring.Core.IO;
using Unity.Services.Economy.Editor.Authoring.Core.Model;
using Unity.Services.Gateway.EconomyApiV2.Generated.Model;
using Statuses = Unity.Services.Cli.Authoring.Model.Statuses;

namespace Unity.Services.Cli.Economy.UnitTest.Authoring;

public class EconomyResourcesLoaderTests
{
    Mock<IFileSystem>? m_MockFileSystem;
    Mock<IEconomyJsonConverter>? m_MockEconomyJsonConverter;
    EconomyResourcesLoader? m_EconomyResourcesLoader;

    CurrencyItemResponse m_TestCurrencyItemResponse = new(
        "TEST_ID",
        "TEST_NAME",
        CurrencyItemResponse.TypeEnum.CURRENCY,
        0,
        100,
        "custom data",
        new ModifiedMetadata(DateTime.Now),
        new ModifiedMetadata(DateTime.Now)
    );

    IEnumerable<Resource>? m_ConfigValidationTestCases;
    IEnumerable<IEconomyResource>? m_EmptyResourceTestCases;
    IEnumerable<(EconomyResourceFile file, string path)>? m_LoadFileTestCases;

    [SetUp]
    public void SetUp()
    {
        m_MockFileSystem = new Mock<IFileSystem>();
        m_MockEconomyJsonConverter = new Mock<IEconomyJsonConverter>();

        m_MockFileSystem!
            .Setup(x =>
                x.ReadAllText(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(m_TestCurrencyItemResponse.ToJson);

        m_EconomyResourcesLoader = new(m_MockEconomyJsonConverter.Object, m_MockFileSystem.Object);

        m_ConfigValidationTestCases = new[]
        {
            new Resource("", "", EconomyResourceTypes.InventoryItem, ""),
            new Resource("", "", EconomyResourceTypes.Currency, ""),
            new Resource("", "", EconomyResourceTypes.VirtualPurchase, ""),
            new Resource("", "", EconomyResourceTypes.MoneyPurchase, "")
        };

        m_EmptyResourceTestCases = new IEconomyResource[]
        {
            new EconomyCurrency("Currency"),
            new EconomyInventoryItem("Inventory Item"),
            new EconomyVirtualPurchase("Virtual Purchase")
            {
                Costs = new []{new Cost()},
                Rewards = new []{new Reward()}
            },
            new EconomyRealMoneyPurchase("Real Money Purchase")
            {
                Rewards = new []{new RealMoneyReward()},
                StoreIdentifiers = new ()
            }
        };

        m_LoadFileTestCases = new (EconomyResourceFile file, string path)[]
        {
            (
                new EconomyCurrencyFile
                {
                    Id = "Currency",
                    Initial = 0,
                    Max = 100,
                    Name = "Currency"
                },
                EconomyResourcesExtensions.Currency
            ),
            (
                new EconomyInventoryItemFile
                {
                    Id = "Inventory_Item",
                    Name = "Inventory Item"
                },
                EconomyResourcesExtensions.InventoryItem
            ),
            (
                new EconomyVirtualPurchaseFile()
                {
                    Id = "Virtual_Purchase",
                    Name = "Virtual Purchase",
                    Costs = new []
                    {
                        new Cost
                        {
                            ResourceId = "Currency",
                            Amount = 1
                        }
                    },
                    Rewards = new [] {
                        new Reward
                        {
                            ResourceId = "Inventory_Item",
                            Amount = 1
                        }
                    }
                },
                EconomyResourcesExtensions.VirtualPurchase
            ),
            (
                new EconomyRealMoneyPurchaseFile()
                {
                    Id = "Real_Money_Purchase",
                    Name = "Real Money Purchase",
                    Rewards = new []
                    {
                        new RealMoneyReward
                        {
                            ResourceId = "Inventory_Item",
                            Amount = 1
                        }
                    }
                },
                EconomyResourcesExtensions.MoneyPurchase
            )
        };
    }

    [Test]
    public async Task LoadResourcesAsync_FailsToReadFile()
    {
        m_MockFileSystem!
            .Setup(x => x.ReadAllText(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException());

        var deployContents = await m_EconomyResourcesLoader!.LoadResourceAsync(
            "",
            CancellationToken.None);

        Assert.That(deployContents.Status.Message, Is.SameAs(Statuses.FailedToRead));
    }

    [Test]
    public async Task LoadResourcesAsync_FailsToDeserialize()
    {
        m_MockEconomyJsonConverter!
            .Setup(x => x.DeserializeObject<Resource>(It.IsAny<string>()))
            .Throws(new JsonReaderException());

        var deployContents = await m_EconomyResourcesLoader!.LoadResourceAsync(
            "",
            CancellationToken.None);

        Assert.That(deployContents.Status.Message, Is.SameAs(Statuses.FailedToRead));
    }

    [Test]
    public async Task LoadResourcesAsync_ReturnsCorrectResource()
    {
        string filepath = "test.ecc";

        var file = new EconomyCurrencyFile()
        {
            Id = "TEST_ID",
            Name = "TEST_NAME",
            Type = EconomyResourceTypes.Currency,
            CustomData = m_TestCurrencyItemResponse.ToJson(),
            Initial = 0,
            Max = 100,
        };

        m_MockEconomyJsonConverter!
            .Setup(x => x.DeserializeObject<EconomyCurrencyFile>(It.IsAny<string>()))
            .Returns(JsonConvert.DeserializeObject<EconomyCurrencyFile>(JsonConvert.SerializeObject(file)));


        var resource = await m_EconomyResourcesLoader!.LoadResourceAsync(
            filepath,
            CancellationToken.None);

        Assert.That(resource.Status.Message, Is.SameAs(Statuses.Loaded));
        Assert.Multiple(() =>
        {
            Assert.That(resource.Id, Is.EqualTo(file.Id));
            Assert.That(((EconomyResource)resource).EconomyType, Is.EqualTo(file.Type));
            Assert.That(resource.Name, Is.EqualTo(file.Name));
            Assert.That(resource.Path, Is.EqualTo(filepath));
        });
    }

    [Test]
    public void ConstructResourceFile_ReturnsCorrectResourceFile()
    {
        foreach (var resource in m_EmptyResourceTestCases!)
        {
            m_MockEconomyJsonConverter!.Setup(
                    x => x.SerializeObject(
                        It.IsAny<IEconomyResourceFile>()
                    ))
                .Returns("");

            Assert.DoesNotThrow(
                () => m_EconomyResourcesLoader!.CreateAndSerialize(
                    resource));

            m_MockEconomyJsonConverter.Verify(
                x => x.SerializeObject(It.IsAny<IEconomyResourceFile>()),
                Times.Once);

            m_MockEconomyJsonConverter.Reset();
        }
    }

    public static IEnumerable<TestCaseData> LoadFileTestCases
    {
        get
        {
            yield return new TestCaseData(
                new EconomyCurrencyFile
                {
                    Id = "Currency",
                    Initial = 0,
                    Max = 100,
                    Name = "Currency"
                },
                EconomyResourcesExtensions.Currency
            );
            yield return new TestCaseData(
                new EconomyInventoryItemFile
                {
                    Id = "Inventory_Item",
                    Name = "Inventory Item"
                },
                EconomyResourcesExtensions.InventoryItem
            );
            yield return new TestCaseData(
                new EconomyVirtualPurchaseFile
                {
                    Id = "Virtual_Purchase",
                    Name = "Virtual Purchase",
                    Costs = new[]
                    {
                        new Cost
                        {
                            ResourceId = "Currency",
                            Amount = 1
                        }
                    },
                    Rewards = new[]
                    {
                        new Reward
                        {
                            ResourceId = "Inventory_Item",
                            Amount = 1,
                            DefaultInstanceData = null
                        }
                    }
                },
                EconomyResourcesExtensions.VirtualPurchase
            );
            yield return new TestCaseData(
                new EconomyRealMoneyPurchaseFile
                {
                    Id = "Real_Money_Purchase",
                    Name = "Real Money Purchase",
                    Rewards = new[]
                    {
                        new RealMoneyReward
                        {
                            ResourceId = "Inventory_Item",
                            Amount = 1
                        }
                    }
                },
                EconomyResourcesExtensions.MoneyPurchase
            );
        }
    }

    [Test]
    public async Task LoadResourceAsync_ReturnsCorrectResourceType()
    {
        foreach (var (file, path) in m_LoadFileTestCases!)
        {

            if (path == EconomyResourcesExtensions.Currency)
            {
                m_MockEconomyJsonConverter!
                    .Setup(x => x.DeserializeObject<EconomyCurrencyFile>(It.IsAny<string>()))
                    .Returns(JsonConvert.DeserializeObject<EconomyCurrencyFile>(JsonConvert.SerializeObject(file)));
            }

            if (path == EconomyResourcesExtensions.InventoryItem)
            {
                m_MockEconomyJsonConverter!
                    .Setup(x => x.DeserializeObject<EconomyInventoryItemFile>(It.IsAny<string>()))
                    .Returns(JsonConvert.DeserializeObject<EconomyInventoryItemFile>(JsonConvert.SerializeObject(file)));
            }

            if (path == EconomyResourcesExtensions.VirtualPurchase)
            {
                m_MockEconomyJsonConverter!
                    .Setup(x => x.DeserializeObject<EconomyVirtualPurchaseFile>(It.IsAny<string>()))
                    .Returns(JsonConvert.DeserializeObject<EconomyVirtualPurchaseFile>(JsonConvert.SerializeObject(file)));
            }

            if (path == EconomyResourcesExtensions.MoneyPurchase)
            {
                m_MockEconomyJsonConverter!
                    .Setup(x => x.DeserializeObject<EconomyRealMoneyPurchaseFile>(It.IsAny<string>()))
                    .Returns(JsonConvert.DeserializeObject<EconomyRealMoneyPurchaseFile>(JsonConvert.SerializeObject(file)));
            }

            var resource = await m_EconomyResourcesLoader!.LoadResourceAsync(
                path,
                CancellationToken.None);

            Assert.That(resource.Status.Message, Is.SameAs(Statuses.Loaded));
            Assert.Multiple(
                async () =>
                {
                    m_MockEconomyJsonConverter!
                        .Setup(x => x.DeserializeObject<JObject>(It.IsAny<string>()))
                        .Returns(JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(file)));

                    var resource = await m_EconomyResourcesLoader!.LoadResourceAsync(
                        path,
                        CancellationToken.None);

                    Assert.That(resource.Status.Message, Is.SameAs(Statuses.Loaded));
                    Assert.Multiple(
                        () =>
                        {
                            Assert.That(resource.Id, Is.EqualTo(file.Id));
                            Assert.That(((EconomyResource)resource).EconomyType, Is.EqualTo(file.Type));
                            Assert.That(resource.Name, Is.EqualTo(file.Name));
                            Assert.That(resource.Path, Is.EqualTo(path));

                            if (resource is EconomyVirtualPurchase economyVirtualPurchase &&
                                file is EconomyVirtualPurchaseFile economyVirtualPurchaseFile)
                            {
                                Assert.That(
                                    economyVirtualPurchase.Costs.Length,
                                    Is.EqualTo(economyVirtualPurchaseFile.Costs.Length));

                                for (int i = 0; i < economyVirtualPurchase.Costs.Length; i++)
                                {
                                    Assert.That(
                                        economyVirtualPurchase.Costs[i].ResourceId,
                                        Is.EqualTo(economyVirtualPurchaseFile.Costs[i].ResourceId));
                                    Assert.That(
                                        economyVirtualPurchase.Costs[i].Amount,
                                        Is.EqualTo(economyVirtualPurchaseFile.Costs[i].Amount));
                                }

                                Assert.That(
                                    economyVirtualPurchase.Rewards.Length,
                                    Is.EqualTo(economyVirtualPurchaseFile.Rewards.Length));

                                for (int i = 0; i < economyVirtualPurchase.Rewards.Length; i++)
                                {
                                    Assert.That(
                                        economyVirtualPurchase.Rewards[i].ResourceId,
                                        Is.EqualTo(economyVirtualPurchaseFile.Rewards[i].ResourceId));
                                    Assert.That(
                                        economyVirtualPurchase.Rewards[i].Amount,
                                        Is.EqualTo(economyVirtualPurchaseFile.Rewards[i].Amount));
                                }
                            }

                            if (resource is EconomyRealMoneyPurchase economyRealMoneyPurchase &&
                                file is EconomyRealMoneyPurchaseFile economyRealMoneyPurchaseFile)
                            {
                                Assert.That(
                                    economyRealMoneyPurchase.Rewards.Length,
                                    Is.EqualTo(economyRealMoneyPurchaseFile.Rewards.Length));

                                for (int i = 0; i < economyRealMoneyPurchase.Rewards.Length; i++)
                                {
                                    Assert.That(
                                        economyRealMoneyPurchase.Rewards[i].ResourceId,
                                        Is.EqualTo(economyRealMoneyPurchaseFile.Rewards[i].ResourceId));
                                    Assert.That(
                                        economyRealMoneyPurchase.Rewards[i].Amount,
                                        Is.EqualTo(economyRealMoneyPurchaseFile.Rewards[i].Amount));
                                }
                            }
                        });
                });
        }
    }

    [Test]
    public async Task LoadResourcesAsync_FailsConfigValidation()
    {
        m_MockEconomyJsonConverter!
            .Setup(x => x.DeserializeObject<Resource>(It.IsAny<string>()))
            .Returns((Resource)null!);

        var deployContents = await m_EconomyResourcesLoader!.LoadResourceAsync(
            "",
            CancellationToken.None);
        Assert.That(deployContents.Status.Message, Is.SameAs(Statuses.FailedToRead));
    }

    [Test]
    public async Task LoadResourcesAsync_FailsCurrencyConfigValidation()
    {
        foreach (var res in m_ConfigValidationTestCases!)
        {
            m_MockEconomyJsonConverter!
                .Setup(x => x.DeserializeObject<Resource>(It.IsAny<string>()))
                .Returns(res);

            ConfigValidationMockHelper(res);

            var deployContents = await m_EconomyResourcesLoader!.LoadResourceAsync(
                "",
                CancellationToken.None);

            Assert.That(deployContents.Status.Message, Is.SameAs(Statuses.FailedToRead));
        }
    }

    void ConfigValidationMockHelper(Resource res)
    {
        switch (res.Type)
        {
            case EconomyResourceTypes.InventoryItem:
                m_MockEconomyJsonConverter!
                    .Setup(x => x.DeserializeObject<EconomyInventoryItemFile>(It.IsAny<string>()))
                    .Returns((EconomyInventoryItemFile)null!);
                break;
            case EconomyResourceTypes.Currency:
                m_MockEconomyJsonConverter!
                    .Setup(x => x.DeserializeObject<EconomyCurrencyFile>(It.IsAny<string>()))
                    .Returns((EconomyCurrencyFile)null!);
                break;
            case EconomyResourceTypes.VirtualPurchase:
                m_MockEconomyJsonConverter!
                    .Setup(x =>
                        x.DeserializeObject<EconomyVirtualPurchaseFile>(It.IsAny<string>()))
                    .Returns((EconomyVirtualPurchaseFile)null!);
                break;
            case EconomyResourceTypes.MoneyPurchase:
                m_MockEconomyJsonConverter!
                    .Setup(x =>
                        x.DeserializeObject<EconomyRealMoneyPurchaseFile>(It.IsAny<string>()))
                    .Returns((EconomyRealMoneyPurchaseFile)null!);
                break;
        }
    }
}
