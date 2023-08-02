using System;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Leaderboards.Authoring.Core.Model
{
    public interface ILeaderboardConfig : IDeploymentItem, ITypedItem
    {
        string Id { get; }
        new float Progress { get; set; }

        SortOrder SortOrder { get; set; }
        UpdateType UpdateType { get; set; }
        Decimal BucketSize { get; set; }
        ResetConfig ResetConfig { get; set; }
        TieringConfig TieringConfig { get; set; }
    }
}
