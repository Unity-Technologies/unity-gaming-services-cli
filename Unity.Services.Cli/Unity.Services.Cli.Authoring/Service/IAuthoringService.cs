namespace Unity.Services.Cli.Authoring.Service;

public interface IAuthoringService
{
    string ServiceType { get; }
    string ServiceName { get; }
    IReadOnlyList<string> FileExtensions { get; }
}
