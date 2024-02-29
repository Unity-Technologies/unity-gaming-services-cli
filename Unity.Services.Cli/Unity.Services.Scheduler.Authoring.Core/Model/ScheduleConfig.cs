using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Scheduler.Authoring.Core.Model
{
    [DataContract]
    public class ScheduleConfig : IScheduleConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }

        [DataMember]
        public string EventName { get; set; }

        [DataMember(Name = "Type")]
        public string ScheduleType { get; set; }

        [DataMember]
        public string Schedule { get; set; }

        [DataMember]
        public int PayloadVersion { get; set; }

        [DataMember]
        public string Payload { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        float IScheduleConfig.Progress
        {
            get => Progress;
            set => Progress = value;
        }

        public string Path { get; set; }
        public float Progress { get; set; }
        public DeploymentStatus Status { get; set; }
        public ObservableCollection<AssetState> States { get; set; }

        // Explicit interface implementation is needed to avoid conflicting with serialization of ScheduleType property
        string ITypedItem.Type => "Schedule";

        public ScheduleConfig()
        {

        }

        public ScheduleConfig(
            string name,
            string eventName,
            string scheduleType,
            string schedule,
            int payloadVersion,
            string payload)
        {
            EventName = eventName;
            ScheduleType = scheduleType;
            Schedule = schedule;
            PayloadVersion = payloadVersion;
            Payload = payload;
            Name = name;
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
