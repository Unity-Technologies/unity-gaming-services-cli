using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Triggers.Authoring.Core.Model
{
    [DataContract]
    public class TriggerConfig : ITriggerConfig
    {
        public string Id { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string EventType { get; set; }
        [DataMember]
        public string ActionType { get; set; }
        [DataMember]
        public string ActionUrn { get; set; }
        [DataMember]
        public string Filter { get; set; }

        public string Path { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public float Progress { get; set; }
        public DeploymentStatus Status { get; set; }
        public ObservableCollection<AssetState> States { get; set; }
        public string Type => "Trigger";

        public TriggerConfig()
        {

        }

        [JsonConstructor]
        public TriggerConfig(string name, string eventType, string actionType, string actionUrn, string filter)
        {
            Name = name;
            EventType = eventType;
            ActionType = actionType;
            ActionUrn = actionUrn;
            Filter = filter;
        }

        public TriggerConfig(string id, string name, string eventType, string actionType, string actionUrn, string filter)
        {
            Id = id;
            Name = name;
            EventType = eventType;
            ActionType = actionType;
            ActionUrn = actionUrn;
            Filter = filter;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public override string ToString()
        {
            if (Path == "Remote")
                return Name;
            return $"'{Name}' in '{Path}'";
        }
    }
}
