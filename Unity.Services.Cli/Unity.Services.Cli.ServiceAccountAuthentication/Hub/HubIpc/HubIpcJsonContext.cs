#if FEATURE_HUB_AUTH
using System.Text.Json.Serialization;

namespace Unity.Services.Cli.ServiceAccountAuthentication.Hub.HubIpc;

[JsonSerializable(typeof(HubMessage))]
[JsonSerializable(typeof(HealthCheckData))]
[JsonSerializable(typeof(ConnectInfoData))]
[JsonSerializable(typeof(UserInfoData))]
[JsonSerializable(typeof(WindowShowRequest))]
partial class HubIpcJsonContext : JsonSerializerContext
{
}
#endif
