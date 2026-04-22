namespace Unity.Services.Cli.Common.Logging;

[Serializable]
class JsonLogMessage
{
    public object? Message { get; set; }
    public string? Type { get; set; }
}
