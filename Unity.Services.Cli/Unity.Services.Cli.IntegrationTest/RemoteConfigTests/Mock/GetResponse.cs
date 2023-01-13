using System;
using System.Collections.Generic;

namespace Unity.Services.Cli.IntegrationTest.RemoteConfigTests.Mock;

[Serializable]
class GetResponse
{
    public List<Config>? Configs { get; set; }
}
