namespace Unity.Services.Cli.Common.Validator;

public interface IConfigurationValidator
{
    bool IsConfigValid(string key, string? value, out string errorMessage);

    bool IsKeyValid(string key, out string errorMessage);

    void ThrowExceptionIfConfigInvalid(string key, string value);
}
