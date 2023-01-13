using Unity.Services.Cli.Common.Persister;

namespace Unity.Services.Cli.Authentication.UnitTest;

class MemoryTokenPersister : IPersister<string>
{
    public string? PersistedToken { get; set; }

    public Task<string?> LoadAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(PersistedToken);

    public Task SaveAsync(string data, CancellationToken cancellationToken = default)
    {
        PersistedToken = data;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        PersistedToken = null;
        return Task.CompletedTask;
    }
}
