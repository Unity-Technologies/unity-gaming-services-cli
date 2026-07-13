using Unity.Services.Cli.Authoring.Utils;
using Unity.Services.Deployment.Core.Model;

namespace Unity.Services.Cli.Authoring.Model;

/// <summary>
/// Represents a file path with an associated deployment definition.
/// This is an intermediary type that bridges a raw file path string and a fully resolved
/// deployment item.
/// <para>
///
/// This class exists as a temporary convenience wrapper and is intended to be removed in a future
/// refactor in favor of an <c>IDeploymentItem IResolve(path, variantTags, cancellationToken)</c>
/// method on the fetch/deploy interface.
///
/// </para>
/// </summary>
public record AuthoringFile
{

    public string Path { get; }
    internal IDeploymentDefinition? DeploymentDefinition { get; }
    IReadOnlyList<string>? VariantTagsOverwrite { get; set; } = null;

    internal AuthoringFile(string path, IDeploymentDefinition? ddef = null)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        DeploymentDefinition = ddef;
    }

    public IReadOnlyList<string> VariantTags
    {
        set => VariantTagsOverwrite = value;
        get => VariantTagsOverwrite ?? VariantTagsUtils.FromAdditionalProperties(DeploymentDefinition?.AdditionalProperties);
    }

}

public class AuthoringFilePathComparer : IEqualityComparer<AuthoringFile>
{
    public bool Equals(AuthoringFile? x, AuthoringFile? y) => x?.Path == y?.Path;
    public int GetHashCode(AuthoringFile obj) => obj?.Path?.GetHashCode() ?? 0;
}
