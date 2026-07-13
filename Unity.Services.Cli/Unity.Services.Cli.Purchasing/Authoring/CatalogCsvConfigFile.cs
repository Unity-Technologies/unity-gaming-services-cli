using Unity.Services.Cli.Authoring.Templates;
using UnityEditor.Purchasing.Editor.Authoring.Core;
using UnityEditor.Purchasing.Editor.Authoring.Core.IO;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;

namespace Unity.Services.Cli.Purchasing.Authoring;

class CatalogCsvConfigFile : IFileTemplate
{
    static readonly CatalogCsvParser k_Parser = new();

    public string Extension => Constants.CsvFileExtension;

    public string FileBodyText => GenerateDefaultContent();

    static string GenerateDefaultContent()
    {
        return k_Parser.Serialize(CatalogItem.CreateDefaultCsvCatalog());
    }
}
