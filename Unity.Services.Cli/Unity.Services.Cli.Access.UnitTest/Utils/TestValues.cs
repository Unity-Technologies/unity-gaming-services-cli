namespace Unity.Services.Cli.Access.UnitTest.Utils;
static class TestValues
{
    public const string ValidProjectId = "a912b1fd-541d-42e1-89f2-85436f27aabd";

    public const string ValidEnvironmentId = "00000000-0000-0000-0000-000000000000";

    public const string ValidPlayerId = "717NADo5SomGLr8MtyAeLCUQErKYf3SN";

    public const string TestAccessToken = "test-token";

    public const string InvalidProjectId = "invalidProject";

    public const string InvalidEnvironmentId = "foo";

    public const string FilePath = "policy.json";

    public const string PolicyJson =
        "{\"statements\":[{\"Sid\":\"Statement-1\",\"Action\":[\"*\"],\"Resource\":\"urn:ugs:*\",\"Principal\":\"Player\",\"Effect\":\"Deny\"}]}";

    public const string deleteOptionsJson = "{\"statementIDs\":[\"statement-1\"]}";
}
