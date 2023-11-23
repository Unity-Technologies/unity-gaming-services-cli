using System.CommandLine;
using System.CommandLine.Parsing;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.GameServerHosting.Input;

public class FileListInput : CommonInput
{
    public const string LimitKey = "--limit";
    public const string ModifiedFromKey = "--modified-from";
    public const string ModifiedToKey = "--modified-to";
    public const string PathFilterKey = "--path-filter";
    public const string ServerIdKey = "--server-id";

    public static readonly Option<string> LimitOption = new(
        LimitKey,
        "Limit the number of items returned in the results. Default and max is 600."
    );

    public static readonly Option<string> ModifiedFromOption = new(
        ModifiedFromKey,
        "The start date to filter files list by"
    );

    public static readonly Option<string> ModifiedToOption = new(
        ModifiedToKey,
        "The end date to filter files list by."
    );

    public static readonly Option<string> PathFilterOption = new(
        PathFilterKey,
        "The path to filter files list by."
    );

    public static readonly Option<long[]> ServerIdOption = new(
        ServerIdKey,
        "The server Ids to retrieve files from."
    )
    {
        AllowMultipleArgumentsPerToken = true,
        IsRequired = true,
    };

    static FileListInput()
    {
        // set default value to 600
        LimitOption.SetDefaultValue("600");
        PathFilterOption.SetDefaultValue("");

        ModifiedFromOption.AddValidator(ValidateModifiedFrom);
        ModifiedToOption.AddValidator(ValidateModifiedTo);
        ServerIdOption.AddValidator(ValidateServerId);
    }

    [InputBinding(nameof(LimitOption))]
    public string? Limit { get; init; }

    [InputBinding(nameof(ModifiedFromOption))]
    public string? ModifiedFrom { get; init; }

    [InputBinding(nameof(ModifiedToOption))]
    public string? ModifiedTo { get; init; }

    [InputBinding(nameof(PathFilterOption))]
    public string? PathFilter { get; init; }

    [InputBinding(nameof(ServerIdOption))]
    public long[]? ServerIds { get; init; }

    static void ValidateModifiedFrom(OptionResult result)
    {
        var dateString = result.GetValueOrDefault<string>();
        if (dateString == null)
        {
            return;
        }

        var dateNow = DateTime.Now;
        if (DateTime.TryParse(dateString, out var modifiedFrom))
        {
            if (dateNow < modifiedFrom)
            {
                // provided modifiedFrom is in the future
                result.ErrorMessage = $"Invalid option for --modified-from. {dateString} is in the future";
            }
        }
        else
        {
            result.ErrorMessage = $"Invalid option for --modified-from. {dateString} is not valid date";
        }
    }

    static void ValidateModifiedTo(OptionResult result)
    {
        var dateString = result.GetValueOrDefault<string>();
        if (dateString == null)
        {
            return;
        }

        var dateNow = DateTime.Now;
        if (DateTime.TryParse(dateString, out var modifiedTo))
        {
            if (dateNow < modifiedTo)
            {
                // provided modifiedFrom is in the future
                result.ErrorMessage = $"Invalid option for --modified-to. {dateString} is in the future";
            }
        }
        else
        {
            result.ErrorMessage = $"Invalid option for --modified-to. {dateString} is not valid date";
        }
    }

    static void ValidateServerId(OptionResult result)
    {
        var servers = result.GetValueOrDefault<long[]>();
        if (servers?.Length == 0)
        {
            result.ErrorMessage = $"Invalid option for --server-id. No server IDs provided";
        }
    }
}
