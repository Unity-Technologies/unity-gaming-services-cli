using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.ServiceAccountAuthentication.Input;

public class LoginInput : CommonInput
{
    internal const string ServiceKeyIdAlias = "--service-key-id";
    internal const string ServiceSecretKeyAlias = "--secret-key-stdin";
    internal const string ServiceUnityHubAlias = "--unity-hub";

    public static readonly Option<string> ServiceKeyIdOption = new(
        ServiceKeyIdAlias,
        "The 'key id' of the service account to connect to.")
    {
        Arity = ArgumentArity.ExactlyOne,
    };

    public static readonly Option<bool> SecretKeyOption = new(
        ServiceSecretKeyAlias,
        "The 'secret key' of the service account to connect to. Pass it through the standard input.")
    {
        Arity = ArgumentArity.Zero,
    };

    public static readonly Option<bool> UnityHubOption = new(
        ServiceUnityHubAlias,
        "Use Unity Hub authentication.")
    {
        Arity = ArgumentArity.Zero,
    };

    [InputBinding(nameof(ServiceKeyIdOption))]
    public string? ServiceKeyId { get; set; }

    [InputBinding(nameof(SecretKeyOption))]
    public bool HasSecretKeyOption { get; set; }

    [InputBinding(nameof(UnityHubOption))]
    public bool HasUnityHubOption { get; set; }
}
