using Unity.Services.Cli.Common.Exceptions;

namespace Unity.Services.Cli.Authoring.Utils;

public static class ImportExportUtils
{
    /// <summary>
    /// Determines the file name that should be used for the import or export.
    /// </summary>
    /// <param name="fileName">The value of the filename argument.</param>
    /// <param name="defaultFileName">The default filename as well as the definition of the expected extension.</param>
    /// <returns>The user provided file name if available, or the default otherwise.</returns>
    /// <exception cref="CliException">Thrown if the user provides an file name that does not match the extension of the default file name.</exception>
    public static string ResolveFileName(string? fileName, string defaultFileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return defaultFileName;
        }

        // Make sure the provided filename matches the default
        string defaultExtension = Path.GetExtension(defaultFileName);
        string inputExtension = Path.GetExtension(fileName);

        if(inputExtension != defaultExtension)
        {
            throw new CliException($"The file-name argument must have the extension '{defaultExtension}'.", null, ExitCode.HandledError);
        }

        return fileName;
    }

}
