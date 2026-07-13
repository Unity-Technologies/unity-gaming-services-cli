#if FEATURE_HUB_AUTH
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Unity.Services.Cli.Common.Networking;
using Unity.Services.Cli.ServiceAccountAuthentication.Exceptions;

namespace Unity.Services.Cli.ServiceAccountAuthentication.Hub.TokenExchange;

partial class TokenExchangeClient : ITokenExchangeClient
{
    const string k_ExchangePath = "/api/auth/v1/genesis-token-exchange/unity";

    readonly HttpClient m_HttpClient;

    public TokenExchangeClient(HttpClient httpClient)
    {
        m_HttpClient = httpClient;
    }

    public async Task<string> ExchangeAsync(string genesisToken, CancellationToken cancellationToken)
    {
        var request = new ExchangeRequest { Token = genesisToken };

        HttpResponseMessage response;
        try
        {
            response = await m_HttpClient.PostAsJsonAsync(
                EndpointHelper.GetCurrentEndpointFor<TokenExchangeEndpoints>() + k_ExchangePath, request, TokenExchangeJsonContext.Default.ExchangeRequest, cancellationToken);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            throw new HubIpcUnavailableException(
                "Failed to exchange Hub token for Unity credentials. "
                + "Check your network connection or run `ugs logout` then `ugs login` to retry.",
                ex);
        }

        var result = await response.Content.ReadFromJsonAsync(TokenExchangeJsonContext.Default.ExchangeResponse, cancellationToken);
        if (string.IsNullOrEmpty(result?.Token))
        {
            throw new HubIpcUnavailableException(
                "Token exchange returned an empty Unity token.");
        }

        return result.Token;
    }

    internal class ExchangeRequest
    {
        [JsonPropertyName("token")]
        public string? Token { get; init; }
    }

    internal class ExchangeResponse
    {
        [JsonPropertyName("token")]
        public string? Token { get; init; }
    }

    [JsonSerializable(typeof(ExchangeRequest))]
    [JsonSerializable(typeof(ExchangeResponse))]
    partial class TokenExchangeJsonContext : JsonSerializerContext
    {
    }
}
#endif
