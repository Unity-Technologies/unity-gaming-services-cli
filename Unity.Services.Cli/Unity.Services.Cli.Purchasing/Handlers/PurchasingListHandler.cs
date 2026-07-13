using Microsoft.Extensions.Logging;
using Spectre.Console;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Input;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Utils;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Core.Service;

namespace Unity.Services.Cli.Purchasing.Handlers;

static class PurchasingListHandler
{
    public static async Task PurchasingListHandlerHandlerAsync(
        CommonInput input,
        IUnityEnvironment unityEnvironment,
        ILiveContentConfigClient client,
        IConsoleTable consoleTable,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync(
            "Fetching Purchasing catalog...",
            _ => PurchasingListAsync(input, unityEnvironment, client, consoleTable, logger, cancellationToken));

        consoleTable.DrawTable();
    }

    static async Task PurchasingListAsync(
        CommonInput input,
        IUnityEnvironment unityEnvironment,
        ILiveContentConfigClient client,
        IConsoleTable consoleTable,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var environmentId = await unityEnvironment.FetchIdentifierAsync(cancellationToken);
        var projectId = input.CloudProjectId!;
        await client.Initialize(environmentId, projectId, cancellationToken);
        var listResult = await client.List(cancellationToken);

        if (input.IsJson)
        {
            logger.LogResultValue(listResult);
            return;
        }

        FillTable(listResult, consoleTable);
    }

    static void FillTable(List<CatalogItem> items, IConsoleTable table)
    {
        table.AddColumns(
            new Text("SKU"),
            new Text("Product Type"),
            new Text("Language"),
            new Text("Title"),
            new Text("Currency"),
            new Text("Amount"));

        var empty = new Text("");

        foreach (var item in items)
        {
            table.AddRow(
                new Text(item.uSku ?? ""),
                new Text(item.ProductType.ToString()),
                empty, empty, empty, empty);

            if (item.ProductDetails != null)
            {
                foreach (var pd in item.ProductDetails)
                {
                    table.AddRow(
                        empty, empty,
                        new Text(pd.Language.ToString()),
                        new Text(pd.Title ?? ""),
                        empty, empty);
                }
            }

            if (item.PricingDetails != null)
            {
                foreach (var pricing in item.PricingDetails)
                {
                    table.AddRow(
                        empty, empty,
                        empty, empty,
                        new Text(pricing.CurrencyCode ?? ""),
                        new Text(pricing.Amount.ToString("F2")));
                }
            }
        }

        if (items.Count == 0)
        {
            table.AddRow(
                new Text("No items found"),
                empty, empty, empty, empty, empty);
        }
    }
}
