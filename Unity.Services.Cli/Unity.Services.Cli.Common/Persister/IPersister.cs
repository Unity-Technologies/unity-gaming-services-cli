namespace Unity.Services.Cli.Common.Persister;

public interface IPersister<T>
{
    /// <summary>
    /// Try to load the persisted value if any.
    /// </summary>
    /// <returns>
    /// Returns the loaded value if any were saved;
    /// returns default otherwise.
    /// </returns>
    Task<T?> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(T data, CancellationToken cancellationToken = default);

    Task DeleteAsync(CancellationToken cancellationToken = default);
}
