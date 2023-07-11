using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.GameServerHosting.Input;

class ServerIdInput : CommonInput
{
    public const string ServerIdKey = "server-id";

    public static readonly Argument<string> ServerIdArgument = new(ServerIdKey, "The unique ID of the server");

    static ServerIdInput()
    {
        ServerIdArgument.AddValidator(result =>
        {
            var value = result.GetValueOrDefault<string>();
            try
            {
                _ = long.Parse(value);
            }
            catch (Exception)
            {
                result.ErrorMessage = $"Server ID '{value}' not a valid ID.";
            }
        });
    }

    [InputBinding(nameof(ServerIdArgument))]
    public string? ServerId { get; init; }
}
