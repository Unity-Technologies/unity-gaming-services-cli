using System.CommandLine;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Common.Input;

namespace Unity.Services.Cli.Purchasing.Input;

public class PurchasingNewFileInput : NewFileInput
{
    public static readonly Option<bool> CsvOption = new(
        "--csv",
        "Create a .catalog.csv file instead of .ucat");

    [InputBinding(nameof(CsvOption))]
    public bool Csv { get; set; }
}
