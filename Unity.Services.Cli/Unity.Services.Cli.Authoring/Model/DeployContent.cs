namespace Unity.Services.Cli.Authoring.Model;

[Serializable]
public class DeployContent
{
    /// <summary>
    /// Name of the deploy content
    /// </summary>
    public readonly string Name;

    /// <summary>
    /// Type of the deploy content
    /// </summary>
    public readonly string Type;

    /// <summary>
    /// Deploy progress of the content
    /// </summary>
    public float Progress { get; set; }

    /// <summary>
    /// Deploy status of the content
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// Path of the deploy content
    /// </summary>
    public readonly string Path;

    /// <summary>
    /// Detail message for the status
    /// </summary>
    public string Detail { get; set; }

    public DeployContent(string name, string type, string path, float progress = 0, string status = "", string detail = "")
    {
        Name = name;
        Type = type;
        Path = path;
        Progress = progress;
        Status = status;
        Detail = detail;
    }
}
