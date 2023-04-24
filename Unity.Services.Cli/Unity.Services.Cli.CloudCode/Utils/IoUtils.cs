namespace Unity.Services.Cli.CloudCode;

static class IoUtils
{
    public static string NormalizePath(string path)
        => path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
}
