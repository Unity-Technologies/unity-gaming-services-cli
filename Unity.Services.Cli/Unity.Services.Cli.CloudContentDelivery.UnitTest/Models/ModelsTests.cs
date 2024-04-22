using NUnit.Framework;
using Unity.Services.Cli.CloudContentDelivery.Model;
using Unity.Services.Cli.Common.Exceptions;

// ReSharper disable ObjectCreationAsStatement

namespace CloudContentDeliveryTest.Models;

[TestFixture]
public class ModelsTests
{
    [Test]
    public void BadgeResult_Constructor_HandleNullInput()
    {
        Assert.Throws<CliException>(() => new BadgeResult(null));
    }

    [Test]
    public void BucketResult_Constructor_HandleNullInput()
    {
        Assert.Throws<CliException>(() => new BucketResult(null));
    }

    [Test]
    public void EntryResult_Constructor_HandleNullInput()
    {
        Assert.Throws<CliException>(() => new EntryResult(null));
    }

    [Test]
    public void PromoteResult_Constructor_HandleNullInput()
    {
        Assert.Throws<CliException>(() => new PromoteResult(null));
    }

    [Test]
    public void PromotionResult_Constructor_HandleNullInput()
    {
        Assert.Throws<CliException>(() => new PromotionResult(null));
    }

    [Test]
    public void ReleaseResult_Constructor_HandleNullInput()
    {
        Assert.Throws<CliException>(() => new ReleaseResult(null));
    }

    [Test]
    public void PermissionResult_Constructor_HandlesNullInput()
    {
        Assert.Throws<CliException>(() => new PermissionResult(null));
    }
}
