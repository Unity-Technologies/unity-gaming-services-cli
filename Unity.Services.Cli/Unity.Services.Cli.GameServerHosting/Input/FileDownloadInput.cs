using System.CommandLine;
using System.CommandLine.Parsing;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.GameServerHosting.Input;

public class FileDownloadInput : CommonInput
{
    public const string OutputKey = "--output";
    public const string PathKey = "--path";
    public const string ServerIdKey = "--server-id";

    public static readonly Option<string> OutputOption = new(
        OutputKey,
        "The path to save the downloaded files to."
    )
    {
        IsRequired = true,
    };

    public static readonly Option<string> PathOption = new(
        PathKey,
        "The path to the file to download."
    )
    {
        IsRequired = true,
    };

    public static readonly Option<string> ServerIdOption = new(
        ServerIdKey,
        "The unique ID of the server"
    )
    {
        IsRequired = true,
    };

    static FileDownloadInput()
    {
        OutputOption.AddValidator(ValidateOutput);
        PathOption.AddValidator(ValidatePath);
        ServerIdOption.AddValidator(ValidateServerId);
    }

    [InputBinding(nameof(OutputOption))]
    public string? Output { get; init; }

    [InputBinding(nameof(PathOption))]
    public string? Path { get; init; }

    [InputBinding(nameof(ServerIdOption))]
    public string? ServerId { get; init; }

    static void ValidateOutput(OptionResult result)
    {
        var value = result.GetValueOrDefault<string>();
        if (string.IsNullOrEmpty(value))
        {
            result.ErrorMessage = "Output path cannot be empty.";
        }
    }

    static void ValidatePath(OptionResult result)
    {
        var value = result.GetValueOrDefault<string>();
        if (string.IsNullOrEmpty(value))
        {
            result.ErrorMessage = "Path cannot be empty.";
        }
    }

    static void ValidateServerId(OptionResult result)
    {
        var value = result.GetValueOrDefault<string>();
        if (string.IsNullOrEmpty(value))
        {
            result.ErrorMessage = "Server ID cannot be empty.";
        }
        try
        {
            _ = long.Parse(value!);
        }
        catch (Exception)
        {
            result.ErrorMessage = $"Server ID '{value}' not a valid ID.";
        }

    }
}
