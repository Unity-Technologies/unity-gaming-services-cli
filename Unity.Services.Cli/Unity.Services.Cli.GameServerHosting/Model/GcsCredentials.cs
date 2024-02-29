using Newtonsoft.Json;

namespace Unity.Services.Cli.GameServerHosting.Model;

public class GcsCredentials
{
    public GcsCredentials(string clientEmail, string privateKey)
    {
        ClientEmail = clientEmail;
        PrivateKey = privateKey;
    }

    [JsonProperty("client_email")]
    public string ClientEmail { get; set; }

    [JsonProperty("private_key")]
    public string PrivateKey { get; set; }
}
