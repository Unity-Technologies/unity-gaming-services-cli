using System.CommandLine;

namespace Unity.Services.Cli;

public static class FileTemplateRegistry
{
    static readonly System.Collections.Concurrent.ConcurrentDictionary<Command, (string Extension, string BodyText)> s_Templates = new();

    public static void Register(Command command, string extension, string bodyText)
    {
        s_Templates[command] = (extension, bodyText);
    }

    public static (string Extension, string BodyText)? GetTemplate(Command command)
    {
        return s_Templates.TryGetValue(command, out var template) ? template : null;
    }
}
