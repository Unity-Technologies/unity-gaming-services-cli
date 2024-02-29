using System.CommandLine;
using System.CommandLine.Parsing;
using Unity.Services.Cli.Common.Input;
using static Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model.CreateBuildRequest;

namespace Unity.Services.Cli.GameServerHosting.Input;

class BuildCreateInput : CommonInput
{
    public const string NameKey = "--name";
    public const string OsFamilyKey = "--os-family";
    public const string TypeKey = "--type";
    public const string BuildVersionNameKey = "--build-version-name";

    public static readonly Option<string> BuildNameOption = new(NameKey, "The name of the build")
    {
        IsRequired = true
    };

    public static readonly Option<OsFamilyEnum> BuildOsFamilyOption = new(OsFamilyKey, "The OS of the build")
    {
        IsRequired = true
    };

    public static readonly Option<BuildTypeEnum> BuildTypeOption = new(TypeKey, "The type of the build")
    {
        IsRequired = true
    };

    public static readonly Option<string> BuildVersionNameOption = new(
        BuildVersionNameKey,
        "The name of the build version to create");

    static BuildCreateInput()
    {
        BuildOsFamilyOption.AddValidator(ValidateOsFamilyEnum);
        BuildTypeOption.AddValidator(ValidateBuildTypeEnum);
    }

    [InputBinding(nameof(BuildNameOption))]
    public string? BuildName { get; init; }

    [InputBinding(nameof(BuildOsFamilyOption))]
    public OsFamilyEnum? BuildOsFamily { get; init; }

    [InputBinding(nameof(BuildTypeOption))]
    public BuildTypeEnum? BuildType { get; init; }

    [InputBinding(nameof(BuildVersionNameOption))]
    public string? BuildVersionName { get; init; }

    static void ValidateOsFamilyEnum(OptionResult result)
    {
        try
        {
            result.GetValueOrDefault();
        }
        catch (Exception)
        {
            result.ErrorMessage =
                $"Invalid option for --os-family. Did you mean one of the following? {string.Join(", ", Enum.GetNames<OsFamilyEnum>())}";
        }
    }

    static void ValidateBuildTypeEnum(OptionResult result)
    {
        try
        {
            result.GetValueOrDefault();
        }
        catch (Exception)
        {
            result.ErrorMessage =
                $"Invalid option for --type. Did you mean one of the following? {string.Join(", ", Enum.GetNames<BuildTypeEnum>())}";
        }
    }
}
