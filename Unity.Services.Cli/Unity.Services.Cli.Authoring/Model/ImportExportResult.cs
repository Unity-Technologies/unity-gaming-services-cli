using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Unity.Services.Cli.Authoring.Model;

[Serializable]
record ImportExportResult(IReadOnlyCollection<ImportExportItem> Items)
{
    [JsonIgnore]
    public string? Header { get; set; }
    public bool DryRun { get; set; }

    public override string ToString()
    {
        var builder = new StringBuilder();

        void PrintSection(string heading, ImportExportAction action)
        {
            var toPrint = Items.Where(i => i.Action == action).ToList();
            if (toPrint.Any())
            {
                builder.AppendLine(heading);
                foreach (var value in toPrint)
                {
                    builder.AppendLine($"    {value.Name}");
                }
            }
        }

        builder.AppendLine(Header);
        PrintSection("Exported:", ImportExportAction.Export);
        PrintSection("Created:", ImportExportAction.Create);
        PrintSection("Updated:", ImportExportAction.Update);
        PrintSection("Deleted:", ImportExportAction.Delete);

        return builder.ToString();
    }
}

[Serializable]
record ImportExportItem(string Name,  [property: JsonConverter(typeof(StringEnumConverter))] ImportExportAction Action, bool Success);

enum ImportExportAction
{
    Export,
    Create,
    Update,
    Delete
}
