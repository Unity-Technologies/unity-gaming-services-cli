using Unity.Services.DeploymentApi.Editor;
using Unity.Services.Tooling.Editor.Scheduler.Authoring.Core.Model;

namespace Unity.Services.Cli.Scheduler.Deploy;

static class SchedulerResultBuilder
{
    static readonly StringComparer k_PathComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    public static SchedulerResultItems Create(IReadOnlyList<IDeploymentItem> authored, bool fetch)
    {
        var entries = authored
            .OfType<SchedulerEntryDeploymentItem>()
            .Select(CreateDisplayEntry)
            .ToList();

        var files = CreateFiles(entries, fetch);

        var duplicateGroups = GetDuplicateGroups(entries);
        var duplicateEntries = duplicateGroups.SelectMany(group => group).ToHashSet();
        var duplicatePaths = duplicateEntries
            .Select(item => NormalizePath(item.Path))
            .ToHashSet(k_PathComparer);

        var failedEntries = entries
            .Where(item => item.Status.MessageSeverity is SeverityLevel.Error or SeverityLevel.Warning)
            .Where(item => !duplicateEntries.Contains(item))
            .Cast<IDeploymentItem>();
        var failedFiles = files
            .Where(file => file.Status.MessageSeverity == SeverityLevel.Error)
            .Where(file => !duplicatePaths.Contains(NormalizePath(file.Path)));

        return new SchedulerResultItems(
            Updated: GetItemsByAction(entries, Constants.Updated),
            Deleted: GetItemsByAction(entries, Constants.Deleted),
            Created: GetItemsByAction(entries, Constants.Created),
            Authored: files.Where(file => file.Status.MessageSeverity != SeverityLevel.Error).ToList(),
            Failed: failedFiles.Concat(failedEntries).Concat(CreateDuplicateFailures(duplicateGroups)).ToList());
    }

    static IReadOnlyList<IDeploymentItem> GetItemsByAction(
        IReadOnlyList<SchedulerEntryDeploymentItem> entries,
        string action)
    {
        return entries
            .Where(item => item.Status.MessageSeverity == SeverityLevel.Success)
            .Where(item => item.Status.MessageDetail?.StartsWith(action, StringComparison.Ordinal) == true)
            .Cast<IDeploymentItem>()
            .ToList();
    }

    static List<ScheduleFileItem> CreateFiles(
        IReadOnlyList<SchedulerEntryDeploymentItem> entries,
        bool fetch)
    {
        return entries
            .Where(item => !string.Equals(item.Path, "Remote", StringComparison.Ordinal))
            .GroupBy(item => NormalizePath(item.Path), k_PathComparer)
            .Select(group => CreateFile(GetDisplayPath(group.Key), group.ToList(), fetch))
            .ToList();
    }

    static SchedulerEntryDeploymentItem CreateDisplayEntry(SchedulerEntryDeploymentItem source)
    {
        return new SchedulerEntryDeploymentItem(GetDisplayPath(source.Path))
        {
            Name = source.Name,
            entry = source.entry,
            Progress = source.Progress,
            Status = source.Status
        };
    }

    static IReadOnlyList<IGrouping<string, SchedulerEntryDeploymentItem>> GetDuplicateGroups(
        IReadOnlyList<SchedulerEntryDeploymentItem> entries)
    {
        var failedEntriesByName = entries
            .Where(item => item.Status.MessageSeverity == SeverityLevel.Error)
            .Where(item => !string.IsNullOrEmpty(item.entry?.Name))
            .GroupBy(item => item.entry.Name, StringComparer.Ordinal);

        return failedEntriesByName
            .Where(group => group.Count() > 1)
            .ToList();
    }

    static IEnumerable<IDeploymentItem> CreateDuplicateFailures(
        IReadOnlyList<IGrouping<string, SchedulerEntryDeploymentItem>> duplicateGroups)
    {
        var entriesByPath = duplicateGroups
            .SelectMany(group => group)
            .GroupBy(item => NormalizePath(item.Path), k_PathComparer);

        foreach (var pathGroup in entriesByPath)
        {
            yield return new ScheduleFileItem(
                new ScheduleConfigFile(Array.Empty<SchedulerEntry>()),
                GetDisplayPath(pathGroup.Key),
                status: new DeploymentStatus(
                    "Duplicate schedules",
                    CreateDuplicateFailureDetail(pathGroup.Key, pathGroup, duplicateGroups),
                    SeverityLevel.Error));
        }
    }

    static string CreateDuplicateFailureDetail(
        string currentPath,
        IEnumerable<SchedulerEntryDeploymentItem> entries,
        IReadOnlyList<IGrouping<string, SchedulerEntryDeploymentItem>> duplicateGroups)
    {
        var duplicateLines = entries
            .Select(item => item.entry.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .Select(name => FormatDuplicateLine(
                name,
                GetOtherPaths(currentPath, name, duplicateGroups)));

        return "Duplicate identifiers:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, duplicateLines)
            + Environment.NewLine
            + "Give each schedule a unique identifier, or process the files separately.";
    }

    static string FormatDuplicateLine(string identifier, IReadOnlyList<string> otherPaths)
    {
        if (otherPaths.Count == 0)
            return $"        {identifier}";

        var displayedPaths = otherPaths.Select(path => $"'{GetDisplayPath(path)}'");
        return $"        {identifier} (also in {string.Join(", ", displayedPaths)})";
    }

    static IReadOnlyList<string> GetOtherPaths(
        string currentPath,
        string identifier,
        IReadOnlyList<IGrouping<string, SchedulerEntryDeploymentItem>> duplicateGroups)
    {
        return duplicateGroups
            .Where(group => string.Equals(group.Key, identifier, StringComparison.Ordinal))
            .SelectMany(group => group)
            .Select(item => NormalizePath(item.Path))
            .Where(path => !k_PathComparer.Equals(path, currentPath))
            .Distinct(k_PathComparer)
            .OrderBy(path => path, k_PathComparer)
            .ToList();
    }

    static ScheduleFileItem CreateFile(
        string path,
        IReadOnlyList<SchedulerEntryDeploymentItem> entries,
        bool fetch)
    {
        var failed = entries.Count(item => item.Status.MessageSeverity is SeverityLevel.Error or SeverityLevel.Warning);
        var operation = fetch ? "fetch" : "deploy";
        var completed = fetch ? "fetched" : "deployed";
        var status = failed switch
        {
            0 => new DeploymentStatus(completed, $"All items successfully {completed}", SeverityLevel.Success),
            _ when failed == entries.Count => new DeploymentStatus($"Failed to {operation}", $"All items failed to {operation}", SeverityLevel.Error),
            _ => new DeploymentStatus($"Partial {operation}", $"Some items failed to {operation}", SeverityLevel.Warning)
        };

        return new ScheduleFileItem(
            new ScheduleConfigFile(Array.Empty<SchedulerEntry>()),
            path,
            100,
            status);
    }

    static string GetDisplayPath(string path)
    {
        if (string.Equals(path, "Remote", StringComparison.Ordinal))
            return path;

        var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), path);
        return Path.IsPathRooted(relativePath) || relativePath.StartsWith(".", StringComparison.Ordinal)
            ? relativePath
            : $".{Path.DirectorySeparatorChar}{relativePath}";
    }

    static string NormalizePath(string path)
    {
        return string.Equals(path, "Remote", StringComparison.Ordinal)
            ? path
            : Path.GetFullPath(path);
    }
}

sealed record SchedulerResultItems(
    IReadOnlyList<IDeploymentItem> Updated,
    IReadOnlyList<IDeploymentItem> Deleted,
    IReadOnlyList<IDeploymentItem> Created,
    IReadOnlyList<IDeploymentItem> Authored,
    IReadOnlyList<IDeploymentItem> Failed);
