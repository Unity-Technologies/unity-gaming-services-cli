using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.CloudSave.Input;

class SetCustomItemInput : CommonInput
{
    public static readonly Option<string?> CustomIdValue = new Option<string?>("--custom-id", "The custom entity ID to set the item for.")
    {
        IsRequired = true
    };
    [InputBinding(nameof(CustomIdValue))]
    public string? CustomId { get; set; }

    public static readonly Option<string?> KeyValue = new Option<string?>("--key", "A string representing the key of the data item to be updated or created.");
    [InputBinding(nameof(KeyValue))]
    public string? Key { get; set; }

    public static readonly Option<string?> ValueValue = new Option<string?>("--value", "Any string, number, boolean, or JSON string with a maximum size of 5 MB.");
    [InputBinding(nameof(ValueValue))]
    public string? Value { get; set; }

    public static readonly Option<string?> WriteLockValue = new Option<string?>("--writelock", "Enforces conflict checking when updating an existing data item. This field should be omitted when creating a new data item.");
    [InputBinding(nameof(WriteLockValue))]
    public string? WriteLock { get; set; }

    public static readonly Option<string?> VisibilityOption = new Option<string?>("--visibility", "A string representing the visibility of the data to be set. If not set, will use default visibility.");
    [InputBinding(nameof(VisibilityOption))]
    public string? Visibility { get; set; }
}
