namespace Unity.Services.Cli.GameServerHosting.IO

{
    public interface IFileSystem
    {
        Task WriteAllText(
            string path,
            string contents,
            CancellationToken token = default
            );

        Task Delete(
            string path,
            CancellationToken token = default
            );
    }
}
