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
        public string ActionScopeType { get; set; }
        [DataMember]
        public string Filter { get; set; }
        [DataMember]
        public WebhookConfig Webhook { get; set; }

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
        public TriggerConfig(string name, string eventType, string actionType, string actionUrn, string filter, string actionScopeType = null, WebhookConfig webhook = null)
        {
            Name = name;
            EventType = eventType;
            ActionType = actionType;
            ActionUrn = actionUrn;
            ActionScopeType = actionScopeType;
            Filter = filter;
            Webhook = webhook;
        }

        /// <summary>
        /// Creates a trigger with a known deployment id. Use when the first string is an id (not a display name), so the call is not ambiguous with the JSON deserialization constructor.
        /// </summary>
        /// <param name="path">When non-null, assigns <see cref="Path"/> (including empty string).</param>
        public static TriggerConfig CreateWithId(
            string id,
            string name,
            string eventType,
            string actionType,
            string actionUrn,
            string filter,
            string actionScopeType = null,
            WebhookConfig webhook = null,
            string path = null)
        {
            var config = new TriggerConfig(id, name, eventType, actionType, actionUrn, filter, actionScopeType, webhook);
            if (path != null)
                config.Path = path;
            return config;
        }

        TriggerConfig(string id, string name, string eventType, string actionType, string actionUrn, string filter, string actionScopeType = null, WebhookConfig webhook = null)
        {
            Id = id;
            Name = name;
            EventType = eventType;
            ActionType = actionType;
            ActionUrn = actionUrn;
            ActionScopeType = actionScopeType;
            Filter = filter;
            Webhook = webhook;
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
