using Newtonsoft.Json;

namespace Unity.Services.Cli.Common.Persister;

public class JsonFilePersister<T> : IPersister<T>
{
    public string FilePath { get; }

    public JsonFilePersister(string path)
    {
        FilePath = path;
    }

    /// <inheritdoc cref="IPersister{T}.LoadAsync"/>
    public async Task<T?> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(FilePath))
        {
            return default;
        }

        var json = await File.ReadAllTextAsync(FilePath, cancellationToken);
        return JsonConvert.DeserializeObject<T>(json);
    }

    /// <inheritdoc cref="IPersister{T}.SaveAsync"/>
    public async Task SaveAsync(T data, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists();
        var json = JsonConvert.SerializeObject(data, Formatting.Indented);
        await File.WriteAllTextAsync(FilePath, json, cancellationToken);
    }

    internal void EnsureDirectoryExists()
    {
        var directoryName = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directoryName)
            && !Directory.Exists(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }
    }

    /// <inheritdoc cref="IPersister{T}.DeleteAsync"/>
    public Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(FilePath))
        {
            return Task.CompletedTask;
        }

        try
        {
            File.Delete(FilePath);
        }
        catch (Exception e)
        {
            return Task.FromException(e);
        }

        return Task.CompletedTask;
    }
}
