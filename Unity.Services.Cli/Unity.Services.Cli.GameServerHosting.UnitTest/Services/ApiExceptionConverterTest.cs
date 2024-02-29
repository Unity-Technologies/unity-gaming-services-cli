using Newtonsoft.Json;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.GameServerHosting.Services;
using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Client;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Services;

[TestFixture]
[TestOf(typeof(ApiExceptionConverter))]
public class ApiExceptionConverterTest
{
    [TestCase("test", "Error parsing", typeof(JsonReaderException))]
    [TestCase("{\"detail\":\"test\"}", "test", typeof(CliException))]
    [TestCase("{\"detail\":\"\"}", "", typeof(CliException))]
    [TestCase(null, "", typeof(ApiException))]
    public void Convert(string payload, string message, Type exceptionType)
    {
        try
        {
            var e = new ApiException(400, "", payload);
            ApiExceptionConverter.Convert(e);
        }
        catch (Exception e)
        {
            Assert.That(e, Is.TypeOf(exceptionType));
            Assert.That(e.Message, Does.Contain(message));
        }
    }
}
