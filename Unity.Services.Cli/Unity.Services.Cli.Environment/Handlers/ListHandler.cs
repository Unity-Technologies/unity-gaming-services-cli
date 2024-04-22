using Microsoft.Extensions.Logging;
using Spectre.Console;
using Unity.Services.Cli.Common;
using Unity.Services.Cli.Common.Console;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Logging;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Environment.Input;
using Unity.Services.Gateway.IdentityApiV1.Generated.Model;

namespace Unity.Services.Cli.Environment.Handlers;

static class ListHandler
{
    public static async Task ListAsync(EnvironmentInput input,
        IEnvironmentService environmentService,
        IConfigurationService configurationService,
        IConsoleTable consoleTable,
        ILogger logger,
        ILoadingIndicator loadingIndicator,
        CancellationToken cancellationToken)
    {
        await loadingIndicator.StartLoadingAsync("Fetching environments...", _ =>
            ListAsync(input, environmentService, configurationService, consoleTable, logger, cancellationToken));

        consoleTable.DrawTable();
    }

    internal static async Task ListAsync(EnvironmentInput input,
        IEnvironmentService environmentService,
        IConfigurationService configurationService,
        IConsoleTable table,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var projectId = input.CloudProjectId ?? throw new MissingConfigurationException(
            Keys.ConfigKeys.ProjectId, Keys.EnvironmentKeys.ProjectId);

        var environments = await environmentService
            .ListAsync(projectId, cancellationToken);

        if (input.IsJson)
        {
            logger.LogResultValue(environments);
            return;
        }

        string? currentEnvironment = null;

        try
        {
            currentEnvironment = await configurationService.GetConfigArgumentsAsync(
                Keys.ConfigKeys.EnvironmentName,
                cancellationToken);
        }
        catch (MissingConfigurationException)
        {
            // Command should still run normally if no environment is set in configuration
        }

        FillTable(environments.ToList(), currentEnvironment, table);
    }

    static void FillTable(List<EnvironmentResponse> environmentList, string? currentEnvironment, IConsoleTable table)
    {
        table.AddColumns(new Text("Environment Name"), new Text("Environment ID"));
        table.GetColumns()[1].NoWrap = true;

        environmentList.ForEach(e =>
        {
            if (currentEnvironment != null && e.Name.Equals(currentEnvironment))
            {
                table.AddRow(new Text(e.Name + " (in use)", new Style(Color.Green)), new Text(e.Id.ToString()));
            }
            else
            {
                table.AddRow(new Text(e.Name), new Text(e.Id.ToString()));
            }
        });

        if (environmentList.Count == 0)
        {
            table.AddRow(new Text("Ø"), new Text("Ø"));
        }
    }
}
