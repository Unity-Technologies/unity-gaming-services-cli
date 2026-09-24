using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.ModuleTemplate.Authoring.Core.Model
{
    [DataContract]
    public class SimpleResourceDeploymentItem : IDeploymentItem, ITypedItem
    {
        internal const string SimpleResourceTypeName = "ModuleTemplate Simple Resource";
        float m_Progress;
        DeploymentStatus m_Status;
        string m_Path;
        string m_Name;

        public SimpleResourceDeploymentItem(string path)
        {
            Name = System.IO.Path.GetFileName(path);
            Path = path;
        }

        public virtual string Type => SimpleResourceTypeName;

        public virtual string Name
        {
            get => m_Name;
            set => SetField(ref m_Name, value);
        }

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
        public SimpleResource Resource { get; set; }

        public DeploymentStatus Status
        {
            get => m_Status;
            set => SetField(ref m_Status, value);
        }

        public ObservableCollection<AssetState> States { get; } = new();

        public override string ToString()
        {
            if (Path == "Remote")
                return Resource.Id;
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

        public bool Validate()
        {
            if (Resource == null)
                return false;

            bool valid = true;
            if (string.IsNullOrWhiteSpace(Resource.Id))
            {
                States.Add(new AssetState("Missing ID", "The ID is missing", SeverityLevel.Error));
                valid = false;
            }
            return valid;
        }
    }
}
