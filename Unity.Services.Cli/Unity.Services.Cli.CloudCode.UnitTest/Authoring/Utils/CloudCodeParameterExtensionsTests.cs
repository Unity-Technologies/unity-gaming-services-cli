using System;
using NUnit.Framework;
using Unity.Services.Cli.CloudCode.UnitTest.Utils;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;

namespace Unity.Services.Cli.CloudCode.UnitTest;

[TestFixture]
class CloudCodeParameterExtensionsTests
{
    [Test]
    public void ToJavaScriptWithValidParametersBuildsExpectedString()
    {
        var stringifiedParameters = TestValues.ValidParameters.ToJavaScript();

        Assert.That(stringifiedParameters, Is.EqualTo(TestValues.ValidParametersToJavaScript));
    }

    [Test]
    public void ToJavaScriptWithEmptyParametersBuildsExpectedString()
    {
        var parameters = Array.Empty<CloudCodeParameter>();

        var stringifiedParameters = parameters.ToJavaScript();

        Assert.That(stringifiedParameters, Is.EqualTo("{}"));
    }
}
