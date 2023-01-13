using System;
using System.Collections.Generic;

namespace Unity.Services.Cli.IntegrationTest.RemoteConfigTests.Mock;

[Serializable]
class Config
{
    public string? ProjectId { get; set; }
    public string? EnvironmentId { get; set; }
    public string? Type { get; set; }
    public string? Id { get; set; }
    public string? Version { get; set; }
    public string? CreatedAt { get; set; }
    public string? UpdatedAt { get; set; }
    public List<RemoteConfigEntry>? Value { get; set; }
}


[Serializable]
public struct RemoteConfigEntry
{
    public string key;
    public object value;
    public string type;
}
