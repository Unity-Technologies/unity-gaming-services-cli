using Spectre.Console;
using Unity.Services.Cli.Deploy.Input;
using Unity.Services.Cli.Deploy.Model;

namespace Unity.Services.Cli.Deploy.Service;

public interface IFetchService
{
    string ServiceType { get; }
    protected string FileExtension { get; }

    Task<FetchResult> FetchAsync(
        FetchInput input,
        StatusContext? loadingContext,
        CancellationToken cancellationToken);
}
