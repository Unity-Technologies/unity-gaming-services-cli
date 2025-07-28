namespace Unity.Services.Cli.GameServerHosting.IO
{
    public class FileSystem : IFileSystem
    {
        public Task WriteAllText(
            string path,
            string contents,
            CancellationToken token = new CancellationToken())
        {
            return File.WriteAllTextAsync(path, contents, token);
        }

        public Task Delete(string path,
            CancellationToken token = default(CancellationToken))
        {
            File.Delete(path);
            return Task.CompletedTask;
        }
    }
}
