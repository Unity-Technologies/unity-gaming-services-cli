using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.CloudSave.Input;

class QueryDataInput : CommonInput
{
    /* Optional request body as file input or raw string. */
    protected const string JsonBodyDescription = "If this is a file path, the content of the file is used; otherwise, the raw string is used.";
    const string k_DefaultJsonBody = "";

    public static readonly Option<string> JsonFileOrBodyOption = new(
        aliases: new[] { "-b", "--body" },
        getDefaultValue: () => k_DefaultJsonBody,
        description: $"The JSON body. {JsonBodyDescription}"
    );

    [InputBinding(nameof(JsonFileOrBodyOption))]
    public virtual string? JsonFileOrBody { get; set; }

    public static readonly Option<string?> VisibilityOption = new Option<string?>("--visibility", "A string representing the visibility of the index to be queried.");
    [InputBinding(nameof(VisibilityOption))]
    public string? Visibility { get; set; }
}
