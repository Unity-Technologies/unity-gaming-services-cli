using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Services.Cli.CloudCode.Model;
using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.UnitTest.Model;

[TestFixture]
public class OutputModuleTests
{
    [Test]
    public void ConstructOutputModule_WithNullOptionalFields()
    {
        var dt = DateTime.UtcNow;
        var response = new GetModuleResponse(
            name: "MyModule",
            language: "JS",
            tags: null,
            signedDownloadURL: null,
            dateModified: dt,
            dateCreated: dt);
        var output = new GetModuleResponseOutput(response);
        var yaml = output.ToString();

        Assert.That(yaml, Does.Contain("name: MyModule"));
        Assert.That(yaml, Does.Contain("language: JS"));
        Assert.That(yaml, Does.Contain("dateModified:"));
        Assert.That(yaml, Does.Contain("dateCreated:"));
        Assert.That(yaml, Does.Not.Contain("signedDownloadURL:"));
        Assert.That(yaml, Does.Not.Contain("tags:"));
    }

    [Test]
    public void ConstructOutputModule_WithSignedDownloadUrl()
    {
        var dt = DateTime.UtcNow;
        var response = new GetModuleResponse(
            name: "MyModule",
            language: "JS",
            tags: null,
            signedDownloadURL: "https://example.com/download",
            dateModified: dt,
            dateCreated: dt);
        var output = new GetModuleResponseOutput(response);
        var yaml = output.ToString();

        Assert.That(yaml, Does.Contain("name: MyModule"));
        Assert.That(yaml, Does.Contain("language: JS"));
        Assert.That(yaml, Does.Contain("dateModified:"));
        Assert.That(yaml, Does.Contain("dateCreated:"));
        Assert.That(yaml, Does.Contain("signedDownloadURL: https://example.com/download"));
    }

    [Test]
    public void ConstructOutputModule_WithTags()
    {
        var dt = DateTime.UtcNow;
        var tags = new Dictionary<string,string>
        {
            {"team", "core"},
            {"env", "prod"}
        };
        var response = new GetModuleResponse(
            name: "MyModule",
            language: "JS",
            tags: tags,
            signedDownloadURL: "https://example.com/download",
            dateModified: dt,
            dateCreated: dt);
        var output = new GetModuleResponseOutput(response);
        var yaml = output.ToString();

        Assert.That(yaml, Does.Contain("name: MyModule"));
        Assert.That(yaml, Does.Contain("language: JS"));
        Assert.That(yaml, Does.Contain("dateModified:"));
        Assert.That(yaml, Does.Contain("dateCreated:"));
        Assert.That(yaml, Does.Contain("tags:"));
        Assert.That(yaml, Does.Contain("team: core"));
        Assert.That(yaml, Does.Contain("env: prod"));
        Assert.That(yaml, Does.Contain("signedDownloadURL: https://example.com/download"));
    }

    [Test]
    public void ConstructOutputModule_HasErrorFalse_NoErrorMessage()
    {
        var dt = DateTime.UtcNow;
        var response = new GetModuleResponse(
            name: "MyModule",
            language: "JS",
            tags: null,
            signedDownloadURL: null,
            dateModified: dt,
            dateCreated: dt,
            hasError: false,
            errorMessage: null);

        var output = new GetModuleResponseOutput(response);
        var yaml = output.ToString();

        Assert.That(yaml, Does.Contain("hasError: false"));
        Assert.That(yaml, Does.Not.Contain("errorMessage:"));
    }

    [Test]
    public void ConstructOutputModule_HasErrorTrue_IncludesErrorMessage()
    {
        var dt = DateTime.UtcNow;
        var response = new GetModuleResponse(
            name: "MyModule",
            language: "JS",
            tags: null,
            signedDownloadURL: null,
            dateModified: dt,
            dateCreated: dt,
            hasError: true,
            errorMessage: "module endpoint generation failed");

        var output = new GetModuleResponseOutput(response);
        var yaml = output.ToString();

        Assert.That(yaml, Does.Contain("hasError: true"));
        Assert.That(yaml, Does.Contain("errorMessage: module endpoint generation failed"));
    }

    [Test]
    public void ConstructOutputModule_HasEndpoints()
    {
        var dt = DateTime.UtcNow;
        var response = new GetModuleResponse(
            name: "MyModule",
            language: "JS",
            tags: null,
            signedDownloadURL: null,
            dateModified: dt,
            dateCreated: dt,
            hasError: false,
            endpoints: new Dictionary<string, ModuleEndpoints1>
            {
                {
                    "SayHello",
                    new ModuleEndpoints1(
                        parameters: new Dictionary<string, string>
                        {
                            { "name", "System.String" }
                        },
                        returnType: "System.String"
                    )
                }
            }
        );

        var output = new GetModuleResponseOutput(response);
        var yaml = output.ToString();

        Assert.That(yaml, Does.Contain("endpoints:"));
        Assert.That(yaml, Does.Contain("SayHello:"));
        Assert.That(yaml, Does.Contain("parameters:"));
        Assert.That(yaml, Does.Contain("name: System.String"));
        Assert.That(yaml, Does.Contain("returnType: System.String"));
    }

}

