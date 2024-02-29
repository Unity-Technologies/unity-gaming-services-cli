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
    public class SimpleResourceDeploymentItem : IResourceDeploymentItem
    {
        internal const string SimpleResourceTypeName = "ModuleTemplate Simple Resource";
        float m_Progress;
        DeploymentStatus m_Status;
        string m_Path;

        public SimpleResourceDeploymentItem(string path)
        {
            Name = System.IO.Path.GetFileName(path);
            Path = path;
        }

        /// <summary>
        /// Name of the item as shown for user feedback, normally file_name.ext
        /// </summary>
        public virtual string Type => SimpleResourceTypeName;

        public virtual string Name { get; }

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

        public IResource Resource { get; set; }

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
}
