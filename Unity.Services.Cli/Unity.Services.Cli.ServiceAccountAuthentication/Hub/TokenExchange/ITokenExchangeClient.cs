#if FEATURE_HUB_AUTH
namespace Unity.Services.Cli.ServiceAccountAuthentication.Hub.TokenExchange;

interface ITokenExchangeClient
{
    Task<string> ExchangeAsync(string genesisToken, CancellationToken cancellationToken);
}
#endif
