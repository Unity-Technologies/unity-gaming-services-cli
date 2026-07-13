#if FEATURE_HUB_AUTH
namespace Unity.Services.Cli.ServiceAccountAuthentication.Hub.HubIpc;

class HubUserInfo
{
    public bool Valid { get; }
    public string? AccessToken { get; }
    public long? AccessTokenExpiration { get; }
    public string? Name { get; }
    public string? DisplayName { get; }
    public string? UserId { get; }
    public string? PrimaryOrg { get; }

    public HubUserInfo(
        bool valid,
        string? accessToken,
        long? accessTokenExpiration,
        string? name,
        string? displayName,
        string? userId,
        string? primaryOrg)
    {
        Valid = valid;
        AccessToken = accessToken;
        AccessTokenExpiration = accessTokenExpiration;
        Name = name;
        DisplayName = displayName;
        UserId = userId;
        PrimaryOrg = primaryOrg;
    }

    internal static HubUserInfo FromData(UserInfoData data)
    {
        return new HubUserInfo(
            data.Valid,
            data.AccessToken,
            data.AccessTokenExpiration,
            data.Name,
            data.DisplayName,
            data.UserId,
            data.PrimaryOrg);
    }
}
#endif
