using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Leaderboards.Authoring.Core.Model
{
    [Serializable]
    public class LeaderboardConfig : ILeaderboardConfig
    {
        float m_Progress;
        DeploymentStatus m_Status;
        internal const string ConfigType = "Leaderboard";

        public LeaderboardConfig() : this(
            "myLeaderboard",
            "My Leaderboard")
        {
        }

        public LeaderboardConfig(
            string id,
            string name,
            SortOrder sortOrder = SortOrder.Asc,
            UpdateType updateType = UpdateType.KeepBest)
        {
            Id = id;
            Name = name;
            Path = string.Empty;
            States = new ObservableCollection<AssetState>();
            SortOrder = sortOrder;
            UpdateType = updateType;
        }

        public SortOrder SortOrder { get; set; }
        public UpdateType UpdateType { get; set; }
        public string Id { get; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string Type => ConfigType;

        /// <inheritdoc/>
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
        public decimal BucketSize { get; set; }
        public ResetConfig ResetConfig { get; set; }
        public TieringConfig TieringConfig { get; set; }

        public override string ToString()
        {
            if (Path == "Remote")
                return Id;
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
            OnPropertyChanged(propertyName!);
            onFieldChanged?.Invoke(field);
        }

        void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
