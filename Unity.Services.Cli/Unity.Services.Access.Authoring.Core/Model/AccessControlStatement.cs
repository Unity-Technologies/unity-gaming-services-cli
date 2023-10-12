using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Access.Authoring.Core.Model
{
    [Serializable]
    [DataContract(Name = "Statement")]
    public class AccessControlStatement : IAcessControlStatement
    {
        public string Type  => "Project Access Control Statement";

        public AccessControlStatement() { }
        [DataMember(Name = "Sid", IsRequired = true, EmitDefaultValue = true)]
        public string Sid { get; set; }

        [DataMember(Name = "Action", IsRequired = true, EmitDefaultValue = true)]
        public List<string> Action { get; set; }

        [DataMember(Name = "Effect", IsRequired = true, EmitDefaultValue = true)]
        public string Effect { get; set; }

        [DataMember(Name = "Principal", IsRequired = true, EmitDefaultValue = true)]
        public string Principal { get; set; }

        [DataMember(Name = "Resource", IsRequired = true, EmitDefaultValue = true)]
        public string Resource { get; set; }

        [DataMember(Name = "ExpiresAt", EmitDefaultValue = false)]
        public DateTime ExpiresAt { get; set; }

        [DataMember(Name = "Version", EmitDefaultValue = false)]
        public string Version { get; set; }

        float m_Progress;
        DeploymentStatus m_Status;

        public string Name { get; set; }
        public string Path { get; set; }

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

        public ObservableCollection<AssetState> States { get; }

        public override string ToString()
        {
            return $"'{Sid}' in '{Path}'";
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
            OnPropertyChanged(propertyName!);
            onFieldChanged?.Invoke(field);
        }

        void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool HasStatementChanged(IReadOnlyList<AccessControlStatement> referenceList)
        {
            var reference = referenceList.SingleOrDefault(r => r.Sid == Sid);

            return reference != null && (reference.Effect != Effect || reference.Principal != Principal ||
                                      reference.Resource != Resource || reference.Version != Version ||
                                      reference.ExpiresAt != ExpiresAt ||
                                      HasStatementActionChanged(reference.Action, Action));
        }

        static bool HasStatementActionChanged(List<string> remoteAction, List<string> localAction)
        {
            var orderedRemoteAction = remoteAction.OrderByDescending(s => s).ToList();
            var orderedLocalAction = localAction.OrderByDescending(s => s).ToList();

            return orderedRemoteAction.Count != orderedLocalAction.Count
                   || !orderedLocalAction.SequenceEqual(orderedRemoteAction);
        }
    }
}
