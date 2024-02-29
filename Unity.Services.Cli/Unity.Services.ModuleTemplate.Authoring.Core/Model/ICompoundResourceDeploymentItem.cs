using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.ModuleTemplate.Authoring.Core.Model
{
    public interface ICompoundResourceDeploymentItem : IDeploymentItem, ITypedItem
    {
        new float Progress { get; set; }

        //TODO: Rename to match your model (e.g. script, entry, pool, etc)
        List<INestedResourceDeploymentItem> Items { get; set; }
    }

    public class CompoundResourceDeploymentItem : ICompoundResourceDeploymentItem
    {
        internal const string CompoundResourceTypeName = "ModuleTemplate Compound Resource";
        float m_Progress;
        DeploymentStatus m_Status;
        string m_Path;

        public CompoundResourceDeploymentItem(string path)
        {
            Name = System.IO.Path.GetFileName(path);
            Path = path;
            Type = CompoundResourceTypeName;
            Items = new List<INestedResourceDeploymentItem>();
        }

        /// <summary>
        /// Name of the item as shown for user feedback, normally file_name.ext
        /// </summary>
        public string Type { get; }

        public string Name { get; }
        public string Path
        {
            get => m_Path;
            set => SetField(ref m_Path, value);
        }

        public float Progress
        {
            get => m_Progress;
            set => SetField(ref m_Progress, value);
        }

        public List<INestedResourceDeploymentItem> Items { get; set; }

        public DeploymentStatus Status
        {
            get => m_Status;
            set => SetField(ref m_Status, value);
        }

        public ObservableCollection<AssetState> States { get; } = new();

        public override string ToString()
        {
            return $"'{Path}'";
        }

        /// <summary>
        /// Event will be raised when a property of the instance is changed
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

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
            Action<T> onFieldChanged = null,
            [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            onFieldChanged?.Invoke(field);
        }
    }

    public interface INestedResourceDeploymentItem : IResourceDeploymentItem
    {
        public ICompoundResourceDeploymentItem Parent { get; set; }
    }

    public class NestedResourceDeploymentItem : SimpleResourceDeploymentItem, INestedResourceDeploymentItem
    {
        internal const string CompoundResourceTypeName = "ModuleTemplate Resource Entry";
        readonly string m_Id;
        public NestedResourceDeploymentItem(string path, IResource resource) : base(path)
        {
            Resource = resource;
            m_Id = resource.Id;
        }

        public NestedResourceDeploymentItem(ICompoundResourceDeploymentItem parent, IResource resource) : base(parent.Path)
        {
            Parent = parent;
            Resource = resource;
            m_Id = resource.Id;
        }

        public override string Name => Resource?.Id ?? m_Id;
        public ICompoundResourceDeploymentItem Parent { get; set; }

        /// <summary>
        /// Name of the item as shown for user feedback, normally file_name.ext
        /// </summary>
        public override string Type => CompoundResourceTypeName;

        public override string ToString()
        {
            if (Path == "Remote")
                return Name;
            return $"{Name} in '{Path}'";
        }
    }
}
