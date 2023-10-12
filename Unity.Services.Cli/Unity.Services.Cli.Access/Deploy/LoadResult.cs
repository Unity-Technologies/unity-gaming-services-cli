using Unity.Services.Access.Authoring.Core.Model;

namespace Unity.Services.Cli.Access.Deploy;

public class LoadResult
{
    public IReadOnlyList<IProjectAccessFile> Loaded { get; }
    public IReadOnlyList<IProjectAccessFile> Failed { get; }

    public LoadResult(IReadOnlyList<IProjectAccessFile> loaded, IReadOnlyList<IProjectAccessFile> failed)
    {
        Loaded = loaded;
        Failed = failed;
    }
}
