using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.ModuleTemplate.Authoring.Core.Model
{
    public class CompoundResourceDeploymentItem : IDeploymentItem, ITypedItem
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
            Items = new List<NestedResourceDeploymentItem>();
        }

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

        //TODO: Rename to match your model (e.g. script, entry, pool, etc)
        public List<NestedResourceDeploymentItem> Items { get; set; }

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

        public event PropertyChangedEventHandler PropertyChanged;

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

    public class NestedResourceDeploymentItem : SimpleResourceDeploymentItem
    {
        internal const string CompoundResourceTypeName = "ModuleTemplate Resource Entry";
        readonly string m_Id;

        public NestedResourceDeploymentItem(string path, SimpleResource resource) : base(path)
        {
            Resource = resource;
            m_Id = resource.Id;
        }

        public NestedResourceDeploymentItem(CompoundResourceDeploymentItem parent, SimpleResource resource) : base(parent.Path)
        {
            Parent = parent;
            Resource = resource;
            m_Id = resource.Id;
        }

        public override string Name => Resource?.Id ?? m_Id;
        public CompoundResourceDeploymentItem Parent { get; set; }

        public override string Type => CompoundResourceTypeName;

        public override string ToString()
        {
            if (Path == "Remote")
                return Name;
            return $"{Name} in '{Path}'";
        }
    }
}
