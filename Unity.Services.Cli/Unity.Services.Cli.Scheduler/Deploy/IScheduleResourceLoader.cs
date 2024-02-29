namespace Unity.Services.Cli.Scheduler.Deploy;

interface IScheduleResourceLoader
{
    Task<ScheduleFileItem> LoadResource(string filePath, CancellationToken cancellationToken);
}
