using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Cli.CloudCode.Deploy;

[Serializable]
public class ModuleDeployContent : IDeploymentItem, ITypedItem
{
    /// <summary>
    /// Name of the deploy content
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Type of the deploy content
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Path of the deploy content
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public float Progress
    {
        get => m_Progress;
        set => SetField(ref m_Progress, value);
    }

    public DeploymentStatus Status
    {
        get => m_Status;
        set => SetField(ref m_Status, value);
    }

    [JsonIgnore]
    public ObservableCollection<AssetState> States { get; }

    DeploymentStatus m_Status;
    float m_Progress;

    /// <summary>
    /// Detail message for the status
    /// </summary>
    [JsonIgnore]
    public string Detail => m_Status.MessageDetail;

    public ModuleDeployContent(string name, string type, string path, float progress = 0, DeploymentStatus? status = null)
    {
        Name = name;
        Type = type;
        Path = path;
        States = new ObservableCollection<AssetState>();
        Progress = progress;
        m_Status = status ?? DeploymentStatus.Empty;
    }

    public ModuleDeployContent(
        string name,
        string type,
        string path,
        float progress,
        string status,
        string? detail = null,
        SeverityLevel level = SeverityLevel.None)
        : this(name, type, path, progress, new DeploymentStatus(status, detail ?? string.Empty, level))
    {
    }

    public override string ToString()
    {
        return $"'{Path}'";
    }

    /// <summary>
    /// Event will be raised when a property of the instance is changed
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Sets the field and raises an OnPropertyChanged event.
    /// </summary>
    /// <param name="field">The field to set.</param>
    /// <param name="value">The value to set.</param>
    /// <param name="onFieldChanged">The callback.</param>
    /// <param name="propertyName">Name of the property to set.</param>
    /// <typeparam name="T">Type of the parameter.</typeparam>
    protected void SetField<T>(
        ref T field,
        T value,
        Action<T>? onFieldChanged = null,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        OnPropertyChanged(propertyName!);
        onFieldChanged?.Invoke(field);
    }

    void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
