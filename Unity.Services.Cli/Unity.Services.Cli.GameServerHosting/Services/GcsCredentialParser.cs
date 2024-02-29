using System.IO.Abstractions;
using Newtonsoft.Json;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.GameServerHosting.Model;

namespace Unity.Services.Cli.GameServerHosting.Services;

class GcsCredentialParser
{
    readonly IFile m_FileSystem;

    public GcsCredentialParser(
        IFile fileSystem)
    {
        m_FileSystem = fileSystem;
    }

    public GcsCredentials Parse(string path)
    {
        if (!m_FileSystem.Exists(path))
        {
            throw new CliException(
                "File not found",
                ExitCode.HandledError);
        }

        var content = m_FileSystem.ReadAllText(path);

        GcsCredentials? credentials;
        try
        {
            credentials = JsonConvert.DeserializeObject<GcsCredentials>(content);
        }
        catch (Exception e)
        {
            throw new CliException(
                $"Invalid JSON format\n{new InvalidGcsCredentialsFileFormat()}",
                e,
                ExitCode.HandledError);
        }

        if (credentials?.PrivateKey == null || credentials?.ClientEmail == null)
        {
            throw new CliException(
                $"`private_key` or `client_email` are empty\n{new InvalidGcsCredentialsFileFormat()}",
                ExitCode.HandledError);
        }

        return credentials;
    }
}
