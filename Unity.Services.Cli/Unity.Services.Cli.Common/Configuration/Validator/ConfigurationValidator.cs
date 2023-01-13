using System.Text.RegularExpressions;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;

namespace Unity.Services.Cli.Common.Validator;

public class ConfigurationValidator : IConfigurationValidator
{
    const string k_EnvironmentNameRegexPattern = "^[a-zA-Z0-9_-]*$";
    const string k_GuidRegexPattern = "^[{]?[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}[}]?$";

    public const string EnvironmentNameInvalidMessage = "Valid input should have only alphanumerical and dash (-) characters.";
    public const string GuidInvalidMessage =
        "Valid input should have characters 0-9, a-f, A-F and follow the format XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX.";

    public const string InvalidKeyMsg = "Invalid key.";
    public const string NullKeyMsg = "The key cannot be null or empty.";
    public const string NullValueMsg = "The value cannot be null or empty.";

    public bool IsConfigValid(string key, string? value, out string errorMessage)
    {
        if (string.IsNullOrEmpty(key))
        {
            errorMessage = NullKeyMsg;
            return false;
        }

        if (string.IsNullOrEmpty(value))
        {
            errorMessage = NullValueMsg;
            return false;
        }

        switch (key)
        {
            case Keys.ConfigKeys.EnvironmentId:
                return IsEnvironmentIdValid(value, out errorMessage);
            case Keys.ConfigKeys.EnvironmentName:
                return IsEnvironmentNameValid(value, out errorMessage);
            case Keys.ConfigKeys.ProjectId:
                return IsProjectIdValid(value, out errorMessage);
            default:
                errorMessage = InvalidKeyMsg;
                return false;
        }
    }

    public bool IsKeyValid(string key, out string errorMessage)
    {
        errorMessage = "";

        if (string.IsNullOrEmpty(key))
        {
            errorMessage = NullKeyMsg;
            return false;
        }

        if (!Keys.ConfigKeys.Keys.Contains(key))
        {
            errorMessage = InvalidKeyMsg;
            return false;
        }

        return true;
    }

    public void ThrowExceptionIfConfigInvalid(string key, string value)
    {
        if (!IsConfigValid(key, value, out var envError))
        {
            throw new ConfigValidationException(key, value, envError);
        }
    }

    static bool IsEnvironmentIdValid(string value, out string errorMessage)
    {
        var guidRegex = new Regex(k_GuidRegexPattern);
        if (value.Any(char.IsWhiteSpace) || !guidRegex.IsMatch(value))
        {
            errorMessage = GuidInvalidMessage;
            return false;
        }

        errorMessage = "";
        return true;
    }

    static bool IsEnvironmentNameValid(string value, out string errorMessage)
    {
        var guidRegex = new Regex(k_EnvironmentNameRegexPattern);
        if (value.Any(char.IsWhiteSpace) || !guidRegex.IsMatch(value))
        {
            errorMessage = EnvironmentNameInvalidMessage;
            return false;
        }

        errorMessage = "";
        return true;
    }

    static bool IsProjectIdValid(string value, out string errorMessage)
    {
        var guidRegex = new Regex(k_GuidRegexPattern);
        if (value.Any(char.IsWhiteSpace) || !guidRegex.IsMatch(value))
        {
            errorMessage = GuidInvalidMessage;
            return false;
        }

        errorMessage = "";
        return true;
    }
}
