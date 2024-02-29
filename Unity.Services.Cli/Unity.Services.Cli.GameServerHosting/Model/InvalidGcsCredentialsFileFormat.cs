namespace Unity.Services.Cli.GameServerHosting.Model;

public class InvalidGcsCredentialsFileFormat
{
    public override string ToString()
    {
        return $@"Invalid GCS credentials file format. Please check the file and try again.

The file should be in the following format:
{{
  ...
  ""private_key"": ""..."",
  ""client_email"": ""..."",
  ...
}}

See: https://developers.google.com/workspace/guides/create-credentials#create_credentials_for_a_service_account
";
    }
}
