using System.Text;

namespace Unity.Services.Cli.MockServer.Common;

public static class CommonKeys
{
    public const string ProjectIdKey = "projects";
    public const string EnvironmentIdKey = "environments";

    // These environment name and id are the same as the example response from the
    // environment api spec and should be kept as such to facilitate
    // the use for the mock server default response
    public const string ValidEnvironmentName = "production";
    public const string ValidEnvironmentId = "390121ca-bb43-494f-b418-55be4e0c0faf";
    public const string ValidProjectId = "12345678-1111-2222-3333-123412341234";

    public const string ValidServiceAccKeyId = "0e250400-c34a-4600-ac4b-f058b0d86b76";
    public const string ValidServiceAccSecretKey = "apddVS3FPsTeN1hI_zBuHmHPF9WT2KAi";
    public static string ValidAccessToken { get; } = Convert.ToBase64String(
        Encoding.UTF8.GetBytes($"{ValidServiceAccKeyId}:{ValidServiceAccSecretKey}"));
}
