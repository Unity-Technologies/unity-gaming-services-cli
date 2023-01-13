using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Persister;
using Unity.Services.Cli.Common.Validator;

namespace Unity.Services.Cli.Common;

class ConfigurationService : IConfigurationService
{
    readonly IPersister<Models.Configuration> m_ConfigurationPersister;
    readonly IConfigurationValidator m_ConfigurationValidator;
    internal const string KeyNotSetErrorMessage = "{0} is not set in project configuration.";

    public ConfigurationService(IPersister<Models.Configuration> configurationPersister,
        IConfigurationValidator configurationValidator)
    {
        m_ConfigurationPersister = configurationPersister;
        m_ConfigurationValidator = configurationValidator;
    }

    public async Task SetConfigArgumentsAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var isConfigValid = m_ConfigurationValidator.IsConfigValid(key, value, out var errorMessage);

        if (!isConfigValid)
        {
            throw new ConfigValidationException(key, value, errorMessage);
        }

        var prefs = await m_ConfigurationPersister.LoadAsync(cancellationToken) ?? new Models.Configuration();

        prefs.SetValue(key, value);

        await m_ConfigurationPersister.SaveAsync(prefs, cancellationToken);
    }

    public async Task<string?> GetConfigArgumentsAsync(string key, CancellationToken cancellationToken = default)
    {
        var prefs = await m_ConfigurationPersister.LoadAsync(cancellationToken) ?? new Models.Configuration();
        string exceptionMsg = string.Format(KeyNotSetErrorMessage, Keys.ConfigKeys.ProjectId);

        var isKeyValid = m_ConfigurationValidator.IsKeyValid(key, out var errorMessage);

        if (!isKeyValid)
        {
            throw new ConfigValidationException(key, null, errorMessage);
        }

        string? loadedValue = prefs.GetValue(key);

        if (string.IsNullOrEmpty(loadedValue))
        {
            throw new MissingConfigurationException(key, exceptionMsg);
        }

        return loadedValue;
    }

    public async Task DeleteConfigArgumentsAsync(string[] keys, CancellationToken cancellationToken = default)
    {
        var prefs = await m_ConfigurationPersister.LoadAsync(cancellationToken) ?? new Configuration();
        bool hasConfigChanged = false;

        foreach (string key in keys)
        {
            var isConfigValid = m_ConfigurationValidator.IsKeyValid(key, out var errorMessage);

            if (!isConfigValid)
            {
                throw new ConfigValidationException(key, null, errorMessage);
            }

            if (!string.IsNullOrEmpty(prefs.GetValue(key)))
            {
                prefs.DeleteValue(key);
                hasConfigChanged = true;
            }
        }

        if (hasConfigChanged)
        {
            await m_ConfigurationPersister.SaveAsync(prefs, cancellationToken);
        }
    }
}
