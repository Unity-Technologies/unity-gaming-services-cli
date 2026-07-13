using System.CommandLine;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.CloudSave.Input;

public class GetCustomItemsInput : CommonInput
{
    public static readonly Option<string?> CustomIdValue = new Option<string?>("--custom-id", "The custom entity ID to set the item for.")
    {
        IsRequired = true
    };
    [InputBinding(nameof(CustomIdValue))]
    public string? CustomId { get; set; }

    public static readonly Option<ICollection<string>?> KeysValue = new Option<ICollection<string>?>("--keys", "The keys to be retrieved. Accepts multiple keys. If not set, all keys will be retrieved.");
    [InputBinding(nameof(KeysValue))]
    public ICollection<string>? Keys { get; set; }

    public static readonly Option<string?> AfterValue = new Option<string?>("--after", "The key after which to retrieve the next page of keys.");
    [InputBinding(nameof(AfterValue))]
    public string? After { get; set; }

    public static readonly Option<string?> VisibilityOption = new Option<string?>("--visibility", "A string representing the visibility of the data to be retrieved. If not set, will use default visibility.");
    [InputBinding(nameof(VisibilityOption))]
    public string? Visibility { get; set; }
}
