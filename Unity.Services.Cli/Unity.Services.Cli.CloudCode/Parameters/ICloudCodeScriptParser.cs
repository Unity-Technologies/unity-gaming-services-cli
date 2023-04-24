using Unity.Services.Gateway.CloudCodeApiV1.Generated.Model;

namespace Unity.Services.Cli.CloudCode.Parameters;

public interface ICloudCodeScriptParser
{
    /// <summary>
    /// Read script file and parse script parameters from script code
    /// </summary>
    /// <param name="scriptCode">script code</param>
    /// <param name="cancellationToken">token to cancel operation</param>
    /// <returns>list of script parameters</returns>
    Task<IReadOnlyList<ScriptParameter>> ParseScriptParametersAsync(string scriptCode, CancellationToken cancellationToken);
}
