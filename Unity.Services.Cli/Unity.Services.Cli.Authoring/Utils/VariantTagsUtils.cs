using Unity.Services.Cli.Authoring.Exceptions;
using Unity.Services.Deployment.Core.VariantTags;

namespace Unity.Services.Cli.Authoring.Utils;

public static class VariantTagsUtils
{
    static T ExecuteWithExceptionMapping<T>(Func<T> action)
    {
        try
        {
            return action();
        }
        catch (InvalidVariantTagsException ex)
        {
            throw new DeployException(ex.Message);
        }
    }

    public static void Validate(IReadOnlyList<string>? variantTags)
    {
        var isValid = VariantTags.Validate(variantTags, out var errorMessage);
        if (!isValid)
        {
            throw new DeployException(errorMessage);
        }
    }

    public static IReadOnlyList<string> FromAdditionalProperties(
        IReadOnlyDictionary<string, object>? additionalProperties)
        => ExecuteWithExceptionMapping(() =>
            VariantTags.FromAdditionalProperties(additionalProperties));

    public static bool Equals(IReadOnlyList<string> lTags, IReadOnlyList<string> rTags)
        => VariantTags.Equals(lTags, rTags);
}
