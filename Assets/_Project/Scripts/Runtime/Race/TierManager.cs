using System;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    [Serializable]
    public struct TierConfig
    {
        public string TierName;
        public long EntryFee;
        public long WinPrize;
        public float ParTime;
        public int TrophyWin;
        public int TrophyLoss;
        public int MaxTrophyCap;
    }

    public sealed class TierManager
    {
        private readonly TierConfig[] _tiers = new TierConfig[]
        {
            new TierConfig { TierName = "Bronze Arena", EntryFee = 500, WinPrize = 900, ParTime = 16.0f, TrophyWin = 20, TrophyLoss = 5, MaxTrophyCap = 200 },
            new TierConfig { TierName = "Silver Arena", EntryFee = 2000, WinPrize = 3600, ParTime = 15.0f, TrophyWin = 22, TrophyLoss = 12, MaxTrophyCap = 500 },
            new TierConfig { TierName = "Gold Arena", EntryFee = 8000, WinPrize = 14400, ParTime = 14.0f, TrophyWin = 24, TrophyLoss = 18, MaxTrophyCap = 1000 },
            new TierConfig { TierName = "Diamond Arena", EntryFee = 25000, WinPrize = 45000, ParTime = 13.0f, TrophyWin = 25, TrophyLoss = 22, MaxTrophyCap = 2000 },
            new TierConfig { TierName = "Champion Arena", EntryFee = 75000, WinPrize = 135000, ParTime = 12.5f, TrophyWin = 25, TrophyLoss = 25, MaxTrophyCap = 99999 }
        };

        public int CurrentTierIndex { get; private set; }
        public TierConfig CurrentTier => _tiers[Mathf.Clamp(CurrentTierIndex, 0, _tiers.Length - 1)];

        public void SelectTier(int index) => CurrentTierIndex = Mathf.Clamp(index, 0, _tiers.Length - 1);
    }
}
