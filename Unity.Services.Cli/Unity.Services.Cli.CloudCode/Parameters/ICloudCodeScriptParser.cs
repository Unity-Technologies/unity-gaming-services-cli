namespace Unity.Services.Cli.CloudCode.Parameters;

public interface ICloudCodeScriptParser
{
    public Task<string?> ParseToScriptParamsJsonAsync(string script, CancellationToken token);
}
