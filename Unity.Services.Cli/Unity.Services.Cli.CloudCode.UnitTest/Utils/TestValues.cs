using System.Collections.Generic;
using Unity.Services.CloudCode.Authoring.Editor.Core.Model;

namespace Unity.Services.Cli.CloudCode.UnitTest.Utils;

static class TestValues
{
    public const string ValidProjectId = "a912b1fd-541d-42e1-89f2-85436f27aabd";

    public const string ValidEnvironmentId = "00000000-0000-0000-0000-000000000000";

    public const string ValidCode = "module.exports = () => {}; module.exports.params = { sides: \"NUMERIC\" };";

    public const string ValidScriptName = "foo.js";

    public const string ValidFilepath = @".\createhandlertemp.txt";

    public static readonly IReadOnlyList<CloudCodeParameter> ValidParameters = new CloudCodeParameter[]
    {
        new()
        {
            Name = "foo",
            Required = true,
            ParameterType = ParameterType.JSON,
        },
        new()
        {
            Name = "bar",
            ParameterType = ParameterType.Any,
        },
    };

    public static readonly string ValidParametersToJavaScript = "{"
        + $"{System.Environment.NewLine}  \"foo\": {{"
        + $"{System.Environment.NewLine}    \"type\": \"JSON\","
        + $"{System.Environment.NewLine}    \"required\": true"
        + $"{System.Environment.NewLine}  }},"
        + $"{System.Environment.NewLine}  \"bar\": \"ANY\""
        + $"{System.Environment.NewLine}}}";
}
