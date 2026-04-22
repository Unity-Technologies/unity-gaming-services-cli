using System.Runtime.Serialization;

namespace Unity.Services.Multiplayer.Editor.Matchmaker.Authoring.Core.Model
{
    public class CliErrorResponseShim
    {
        [DataMember(IsRequired = true)] public string ResultCode { get; set; } = string.Empty;
        [DataMember(IsRequired = true)] public string Message { get; set; } = string.Empty;
    }
}
