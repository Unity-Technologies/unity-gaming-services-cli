using System.IO;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Services.GameServerHosting.LocalProxy.Service
{
    public interface IRemoteLocalProxyService
    {
        public string GameServerHost { set; }

        // Authentication APIs
        public Task HandleFetchUnityJwtToken(Stream downstream, CancellationToken cancellationToken);
        // Game Server APIs
        public Task HandleUpdateServerState(Stream downstream, string rawRequest, string serverId, string allocationId, CancellationToken cancellationToken);
        // Server Hold & Release APIs
        public Task HandleHoldServer(Stream downstream, string rawRequest, string serverId, CancellationToken cancellationToken);
        public Task HandleServerHoldStatus(Stream downstream, string serverId, CancellationToken cancellationToken);
        public Task HandleRemoveServerHold(Stream downstream, string serverId, CancellationToken cancellationToken);
        public Task HandleReadyForPlayers(Stream downstream, string serverId, string allocationId, CancellationToken cancellationToken);
        public Task HandleUnreadyForPlayers(Stream downstream, string serverId, CancellationToken cancellationToken);
        // Payload APIs
        public Task HandleRetrieveAllocationPayload(Stream downstream, string allocationId, CancellationToken cancellationToken);
        // Reservation APIs
        public Task HandleReserveServer(Stream downstream, string serverId, CancellationToken cancellationToken);
        public Task HandleUnreserveServer(Stream downstream, string serverId, CancellationToken cancellationToken);
    }
}
