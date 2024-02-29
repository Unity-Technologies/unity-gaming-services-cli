using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Authoring.Model.TableOutput;

[Serializable]
public class RowContent
{
    public string Name { get; protected set; } = "";
    public string Service { get; protected set; } = "";
    public string Type { get; protected set; } = "";
    public string Status { get; protected set; } = "";
    public string Details { get; protected set; } = "";
    public string Severity { get; protected set; } = "";
    public string Path { get; protected set; } = "";

    public RowContent() { }

    public RowContent(IDeploymentItem item, string service)
    {
        Name = item.Name;
        Service = service;
        Type =  ((ITypedItem)item).Type;
        Status =  item.Status.Message;
        Details = item.Status.MessageDetail;
        Severity =  item.Status.MessageSeverity.ToString();
        Path = item.Path;
    }

    public static RowContent ToRow(IDeploymentItem item)
    {
        return new RowContent
        {
            Name = item.Name,
            Type = ((ITypedItem)item).Type,
            Status = item.Status.Message,
            Details = item.Status.MessageDetail,
            Path = item.Path,
            Severity = item.Status.MessageSeverity.ToString()
        };
    }
}
