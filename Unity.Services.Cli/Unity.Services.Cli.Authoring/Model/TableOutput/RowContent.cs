using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.Authoring.Model.TableOutput;

[Serializable]
public class RowContent
{
    public string Name { get; protected set; }
    public string Type { get; protected set; }
    public string Status{ get; protected set; }
    public string Details{ get; protected set; }
    public string Path{ get; protected set; }

    public RowContent(string name = "", string type = "", string status = "", string details = "", string path = "")
    {
        Name = name;
        Type = type;
        Status = status;
        Details = details;
        Path = path;
    }

    public static RowContent ToRow(IDeploymentItem item)
    {
        return new RowContent(
            item.Name,
            ((ITypedItem)item).Type,
            item.Status.Message,
            item.Status.MessageDetail,
            item.Path);
    }
}
