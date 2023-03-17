using Spectre.Console;
using Unity.Services.Cli.Authoring.Input;
using Unity.Services.Cli.Authoring.Model;

namespace Unity.Services.Cli.Authoring.Service;

public interface IFetchService
{
    string ServiceType { get; }
    protected string FileExtension { get; }

    Task<FetchResult> FetchAsync(
        FetchInput input,
        StatusContext? loadingContext,
        CancellationToken cancellationToken);
}
