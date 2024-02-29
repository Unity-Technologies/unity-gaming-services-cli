using Unity.Services.Gateway.GameServerHostingApiV1.Generated.Model;

namespace Unity.Services.Cli.GameServerHosting.Services;

public static class CoreDumpStateConverter
{
    public enum StateEnum
    {
        Disabled = 0,
        Enabled = 1
    }

    public static string ConvertToString(GetCoreDumpConfig200Response.StateEnum? state)
    {
        return state switch
        {
            GetCoreDumpConfig200Response.StateEnum.NUMBER_0 => "disabled",
            GetCoreDumpConfig200Response.StateEnum.NUMBER_1 => "enabled",
            _ => "unknown"
        };
    }

    public static CreateCoreDumpConfigRequest.StateEnum ConvertToCreateStateEnum(StateEnum? state)
    {
        return state switch
        {
            StateEnum.Disabled => CreateCoreDumpConfigRequest.StateEnum.NUMBER_0,
            StateEnum.Enabled => CreateCoreDumpConfigRequest.StateEnum.NUMBER_1,
            _ => throw new ArgumentException($"Invalid state: {state}")
        };
    }

    public static UpdateCoreDumpConfigRequest.StateEnum ConvertToUpdateStateEnum(StateEnum? state)
    {
        return state switch
        {
            StateEnum.Disabled => UpdateCoreDumpConfigRequest.StateEnum.NUMBER_0,
            StateEnum.Enabled => UpdateCoreDumpConfigRequest.StateEnum.NUMBER_1,
            _ => throw new ArgumentException($"Invalid state: {state}")
        };
    }

    public static UpdateCoreDumpConfigRequest.StateEnum ConvertStringToUpdateStateEnum(string state)
    {
        return state.ToLower() switch
        {
            "disabled" => UpdateCoreDumpConfigRequest.StateEnum.NUMBER_0,
            "enabled" => UpdateCoreDumpConfigRequest.StateEnum.NUMBER_1,
            _ => throw new ArgumentException($"Invalid state: {state}")
        };
    }

    public static CreateCoreDumpConfigRequest.StateEnum ConvertStringToCreateStateEnum(string state)
    {
        return state.ToLower() switch
        {
            "disabled" => CreateCoreDumpConfigRequest.StateEnum.NUMBER_0,
            "enabled" => CreateCoreDumpConfigRequest.StateEnum.NUMBER_1,
            _ => throw new ArgumentException($"Invalid state: {state}")
        };
    }
}
