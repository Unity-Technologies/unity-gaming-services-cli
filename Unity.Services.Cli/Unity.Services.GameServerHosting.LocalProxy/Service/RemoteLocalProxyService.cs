using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Cli.Common.Networking;

namespace Unity.Services.GameServerHosting.LocalProxy.Service
{
    public class RemoteLocalProxyService : IRemoteLocalProxyService
    {
        readonly HttpClient m_HttpClient;

        public string GameServerHost { get; set; }

        public RemoteLocalProxyService(
            HttpClient httpClient,
            string projectId,
            string environmentId
            )
        {
            // These APIs are authorized with a basic auth user/pass of project/environment IDs
            m_HttpClient = httpClient;
            m_HttpClient.DefaultRequestHeaders.SetXClientIdHeader();
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{projectId}:{environmentId}"));
            m_HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", token);
        }

        public void SetHeaders(Dictionary<string, string> headers)
        {
            if (headers == null) return;

            foreach (var header in headers)
            {
                // If the header already exists, remove it first to avoid duplicates.
                m_HttpClient.DefaultRequestHeaders.Remove(header.Key);
                m_HttpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        public async Task HandleFetchUnityJwtToken(Stream downstream, CancellationToken cancellationToken)
        {
            await MakeRequestAndWriteResponseToStream(
                downstream,
                HttpMethod.Get,
                new Uri($"https://{GameServerHost}/v1/payload/token"),
                null,
                cancellationToken);
        }

        public async Task HandleUpdateServerState(
            Stream downstream,
            string rawRequest,
            string serverId,
            string allocationId,
            CancellationToken cancellationToken)
        {
            await MakeRequestAndWriteResponseToStream(
                downstream,
                HttpMethod.Patch,
                new Uri($"https://{GameServerHost}/v1/servers/{serverId}/allocations/{allocationId}"),
                new StringContent(ParseBodyFromRawRequest(rawRequest), Encoding.UTF8, "application/json"),
                cancellationToken);
        }

        public async Task HandleHoldServer(Stream downstream, string rawRequest, string serverId, CancellationToken cancellationToken)
        {
            await MakeRequestAndWriteResponseToStream(
                downstream,
                HttpMethod.Post,
                new Uri($"https://{GameServerHost}/v1/servers/{serverId}/hold"),
                new StringContent(ParseBodyFromRawRequest(rawRequest), Encoding.UTF8, "application/json"),
                cancellationToken);
        }

        public async Task HandleServerHoldStatus(Stream downstream, string serverId, CancellationToken cancellationToken)
        {
            await MakeRequestAndWriteResponseToStream(
                downstream,
                HttpMethod.Get,
                new Uri($"https://{GameServerHost}/v1/servers/{serverId}/hold"),
                null,
                cancellationToken);
        }

        public async Task HandleRemoveServerHold(Stream downstream, string serverId, CancellationToken cancellationToken)
        {
            await MakeRequestAndWriteResponseToStream(
                downstream,
                HttpMethod.Delete,
                new Uri($"https://{GameServerHost}/v1/servers/{serverId}/hold"),
                null,
                cancellationToken);
        }

        public async Task HandleReadyForPlayers(Stream downstream, string serverId, string allocationId, CancellationToken cancellationToken)
        {
            await MakeRequestAndWriteResponseToStream(
                downstream,
                HttpMethod.Post,
                new Uri($"https://{GameServerHost}/v1/server/{serverId}/allocation/{allocationId}/ready-for-players"),
                null,
                cancellationToken);
        }
        public async Task HandleUnreadyForPlayers(Stream downstream, string serverId, CancellationToken cancellationToken)
        {
            await MakeRequestAndWriteResponseToStream(
                downstream,
                HttpMethod.Post,
                new Uri($"https://{GameServerHost}/v1/server/{serverId}/unready"),
                null,
                cancellationToken);
        }

        public async Task HandleRetrieveAllocationPayload(Stream downstream, string allocationId, CancellationToken cancellationToken)
        {
            await MakeRequestAndWriteResponseToStream(
                downstream,
                HttpMethod.Get,
                new Uri($"https://{GameServerHost}/v1/payload/allocations/{allocationId}"),
                null,
                cancellationToken);
        }

        public async Task HandleReserveServer(Stream downstream, string serverId, CancellationToken cancellationToken)
        {
            await MakeRequestAndWriteResponseToStream(
                downstream,
                HttpMethod.Post,
                new Uri($"https://{GameServerHost}/v1/servers/{serverId}/reservations"),
                null,
                cancellationToken);
        }

        public async Task HandleUnreserveServer(Stream downstream, string serverId, CancellationToken cancellationToken)
        {
            await MakeRequestAndWriteResponseToStream(
                downstream,
                HttpMethod.Delete,
                new Uri($"https://{GameServerHost}/v1/servers/{serverId}/reservations"),
                null,
                cancellationToken);
        }

        async Task MakeRequestAndWriteResponseToStream(
            Stream downstream,
            HttpMethod httpMethod,
            Uri uri,
            HttpContent content,
            CancellationToken cancellationToken,
            [CallerMemberName] string callerName = "")
        {
            try
            {
                var request = new HttpRequestMessage(httpMethod, uri)
                {
                    Content = content
                };

                var response = await m_HttpClient.SendAsync(request, cancellationToken);

                var ret = await CreateOnwardsResponse(response, cancellationToken);

                await downstream.WriteAsync(ret, 0, ret.Length, cancellationToken);
            }
            catch (HttpRequestException exception)
            {
                throw new HttpRequestException($"Error making upstream HTTP request: {exception.Message}");
            }
        }

        static async Task<byte[]> CreateOnwardsResponse(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var ret = $"HTTP/1.1 {(int)response.StatusCode} {response.ReasonPhrase}\r\n";

            foreach (var header in response.Content.Headers)
            {
                ret += $"{header.Key}: {string.Join(", ", header.Value)}\r\n";
            }

            ret += "\r\n";

            var res = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!string.IsNullOrEmpty(res))
            {
                ret += res;
            }

            return Encoding.UTF8.GetBytes(ret);
        }

        static string ParseBodyFromRawRequest(string rawRequest)
        {
            var fields = rawRequest.Split("\r\n\r\n", 2);
            if (fields.Length < 2 || string.IsNullOrWhiteSpace(fields[1]))
            {
                return "";
            }

            return fields[1].Trim();
        }
    }
}
