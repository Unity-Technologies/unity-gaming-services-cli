namespace Unity.Services.Cli.GameServerHosting.Model;

class LocalFile
{
    readonly string m_PathInDirectory;
    readonly string m_SystemPath;

    public LocalFile(string systemPath, string pathInDirectory)
    {
        m_SystemPath = systemPath;
        m_PathInDirectory = pathInDirectory;
    }

    public string GetSystemPath()
    {
        return m_SystemPath;
    }

    public string GetPathInDirectory()
    {
        return m_PathInDirectory;
    }
}
