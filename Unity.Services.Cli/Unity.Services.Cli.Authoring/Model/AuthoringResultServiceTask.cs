namespace Unity.Services.Cli.Authoring.Model;

public class AuthoringResultServiceTask<T>
    where T : AuthorResult
{
    public Task<T> AuthorResultTask { get; }
    public string ServiceType { get; }

    public AuthoringResultServiceTask(Task<T> authorResultTask, string serviceType)
    {
        AuthorResultTask = authorResultTask;
        ServiceType = serviceType;
    }
}
