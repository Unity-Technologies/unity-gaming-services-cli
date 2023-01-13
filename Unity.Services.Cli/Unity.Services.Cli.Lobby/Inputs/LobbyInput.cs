using System.CommandLine;
using Unity.Services.Cli.Common.Input;
using static Unity.Services.MpsLobby.LobbyApiV1.Generated.Model.TokenRequest;

namespace Unity.Services.Cli.Lobby.Input

{
    /// <summary>
    /// Class defining options or arguments that expect a request body, be it a file or string.
    /// </summary>
    public class LobbyBodyInput : CommonInput
    {
        /* Optional request body as file input or raw string. */
        private protected const string k_JsonBodyDescription = "If this is a file path, the content of the file is used; otherwise, the raw string is used.";
        private const string k_DefaultJsonBody = "{}";

        public static readonly Option<string> JsonFileOrBodyOption = new(
            aliases: new[] { "-b", "--body" },
            getDefaultValue: () => k_DefaultJsonBody,
            description: $"The JSON body. {k_JsonBodyDescription}"
        );

        [InputBinding(nameof(JsonFileOrBodyOption))]
        public virtual string? JsonFileOrBody { get; set; }

        /* Query filter. */
        public static readonly Argument<string> QueryFilterArgument = new("filter", $"The JSON filter to use for querying. {k_JsonBodyDescription}");

        [InputBinding(nameof(QueryFilterArgument))]
        public string? QueryFilter { get; set; }

        /* Player details. */
        public static readonly Argument<string> PlayerDetailsArgument = new("player-details", $"The JSON player details. {k_JsonBodyDescription}");

        [InputBinding(nameof(PlayerDetailsArgument))]
        public string? PlayerDetails { get; set; }
    }

    /// <summary>
    /// Class defining common inputs for the base `lobby` command.
    /// </summary>
    public class CommonLobbyInput : LobbyBodyInput
    {
        /* Lobby ID. */
        public static readonly Argument<string> LobbyIdArgument = new("lobby-id", "The lobby ID");

        [InputBinding(nameof(LobbyIdArgument))]
        public virtual string? LobbyId { get; set; }

        /* Service ID. */
        public static readonly Option<string> ServiceIdOption = new Option<string>("--service-id", "The service ID") { IsRequired = true };

        [InputBinding(nameof(ServiceIdOption))]
        public string? ServiceId { get; set; }

        /* Player ID. */
        public static readonly Option<string> PlayerIdOption = new Option<string>("--player-id", "The player ID to impersonate");

        [InputBinding(nameof(PlayerIdOption))]
        public virtual string? PlayerId { get; set; }
    }

    /// <summary>
    /// Class defining the inputs specific to the `join` subcommand (i.e. `lobby join ...`).
    /// </summary>
    public class JoinInput : CommonLobbyInput
    {
        /* Lobby ID. */
        public static readonly Option<string> LobbyIdOption = new(new[]
        {
            "--lobby-id"
        });

        [InputBinding(nameof(LobbyIdOption))]
        public override string? LobbyId { get; set; }

        /* Lobby code. */
        public static readonly Option<string> LobbyCodeOption = new(new[]
        {
            "--lobby-code"
        });

        [InputBinding(nameof(LobbyCodeOption))]
        public string? LobbyCode { get; set; }
    }

    /// <summary>
    /// Class for inputs that require a request body. Used for lobby creates, player updates, and lobby updates.
    /// </summary>
    public class RequiredBodyInput : CommonLobbyInput
    {
        /* Required request body as file input or raw string. */
        public static readonly Argument<string> RequestBodyArgument = new("body", k_JsonBodyDescription);

        [InputBinding(nameof(RequestBodyArgument))]
        public override string? JsonFileOrBody { get; set; }
    }

    /// <summary>
    /// Class defining the inputs specific to the `player` subcommand (i.e. `lobby player ...`).
    /// </summary>
    public class PlayerInput : RequiredBodyInput
    {
        /* Player ID. */
        public static readonly Argument<string> PlayerIdArgument = new("player-id", "The player ID");

        [InputBinding(nameof(PlayerIdArgument))]
        public override string? PlayerId { get; set; }
    }

    /// <summary>
    /// Class defining the inputs specific to the `token` subcommand.
    /// </summary>
    public class LobbyTokenInput : PlayerInput
    {
        /* Token type */
        public static readonly Argument<TokenTypeEnum> TokenTypeArgument = new("type", "The token type");

        [InputBinding(nameof(TokenTypeArgument))]
        public TokenTypeEnum TokenType { get; set; }
    }

    public class LobbyConfigUpdateInput : RequiredBodyInput
    {
        public static readonly Argument<string> ConfigIdArgument = new("config-id", "The ID of the config to update");

        [InputBinding(nameof(ConfigIdArgument))]
        public string? ConfigId { get; set; }
    }
}
