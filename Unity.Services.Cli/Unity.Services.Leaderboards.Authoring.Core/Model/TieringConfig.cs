using System;
using System.Collections.Generic;

namespace Unity.Services.Leaderboards.Authoring.Core.Model
{
    [Serializable]
    public class TieringConfig
    {
        public Strategy Strategy { get; set; }
        public List<Tier> Tiers { get; set; }
    }
}
