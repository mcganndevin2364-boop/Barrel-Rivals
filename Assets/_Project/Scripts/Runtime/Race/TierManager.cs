using System;
using System.Collections.Generic;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    [System.Serializable]
    public struct TierDefinition
    {
        public int TierIndex;
        public string TierName;
        public string ArenaName;
        public string SceneName;
        public int EntryFee;
        public int WinPrize;
        public int HouseRake;
        public int RequiredTrophiesToUnlock;
        public int MaxTrophyCap;
        public int TrophyWinDelta;
        public int TrophyLossDelta;
        public float ParTimeSeconds;
        public float DriftTimingWindow;
        public float PerfectZoneRadius;
        public float CleanZoneRadius;
        public float WideZoneRadius;

        public TierDefinition(int tierIndex, string tierName, string arenaName, string sceneName, int entryFee, int winPrize, int requiredTrophies, int maxTrophyCap, int trophyWinDelta, int trophyLossDelta, float parTime, float driftWindow, float perfectRadius, float cleanRadius, float wideRadius)
        {
            TierIndex = tierIndex;
            TierName = tierName;
            ArenaName = arenaName;
            SceneName = sceneName;
            EntryFee = entryFee;
            WinPrize = winPrize;
            HouseRake = (entryFee * 2) - winPrize;
            RequiredTrophiesToUnlock = requiredTrophies;
            MaxTrophyCap = maxTrophyCap;
            TrophyWinDelta = trophyWinDelta;
            TrophyLossDelta = trophyLossDelta;
            ParTimeSeconds = parTime;
            DriftTimingWindow = driftWindow;
            PerfectZoneRadius = perfectRadius;
            CleanZoneRadius = cleanRadius;
            WideZoneRadius = wideRadius;
        }
    }

    [DisallowMultipleComponent]
    public sealed class TierManager : MonoBehaviour
    {
        public const int MaxDemotionShieldCharges = 3;
        [SerializeField] private List<TierDefinition> _tierDefinitions = new List<TierDefinition>();
        [SerializeField] private bool _enableDemotionShield = true;
        [SerializeField, Min(1)] private int _shieldChargesOnPromotion = 3;

        private int _currentTrophies = 0;
        private int _highestTrophies = 0;
        private int _selectedTierIndex = 0;
        private int _highestUnlockedTierIndex = 0;
        private int _activeDemotionShieldCharges = 0;
        private int _highestEverUnlockedTierIndex = 0;

        public event Action<int, string> OnTierUnlocked;
        public event Action<int, int> OnTierPromoted;
        public event Action<int, int> OnTierDemoted;
        public event Action<int, int, int, int> OnTrophiesChanged;
        public event Action<int> OnDemotionShieldConsumed;
        public event Action<int> OnSelectedTierChanged;

        public int CurrentTrophies => _currentTrophies;
        public int HighestTrophies => _highestTrophies;
        public int SelectedTierIndex => _selectedTierIndex;
        public int HighestUnlockedTierIndex => _highestUnlockedTierIndex;
        public int ActiveDemotionShieldCharges => _activeDemotionShieldCharges;

        private void Awake() => ValidateAndSortTierDefinitions();
        private void OnValidate() => ValidateAndSortTierDefinitions();

        private void ValidateAndSortTierDefinitions()
        {
            if (_tierDefinitions == null || _tierDefinitions.Count == 0) BuildCanonicalTierDefinitions();
            _tierDefinitions.Sort((a, b) => a.RequiredTrophiesToUnlock.CompareTo(b.RequiredTrophiesToUnlock));
            for (int i = 0; i < _tierDefinitions.Count; i++) { var t = _tierDefinitions[i]; t.TierIndex = i; _tierDefinitions[i] = t; }
        }

        private void BuildCanonicalTierDefinitions()
        {
            _tierDefinitions = new List<TierDefinition>
            {
                new TierDefinition(0, "Bronze Arena", "Oak Ridge Practice Ring", "Arena_Bronze", 500, 900, 0, 200, 20, 5, 16.0f, 0.50f, 0.60f, 1.60f, 3.20f),
                new TierDefinition(1, "Silver Arena", "Dusty Gulch Fairgrounds", "Arena_Silver", 2000, 3600, 200, 500, 22, 12, 15.0f, 0.40f, 0.50f, 1.50f, 3.00f),
                new TierDefinition(2, "Gold Arena", "Lone Star Coliseum", "Arena_Gold", 8000, 14400, 500, 1000, 24, 18, 14.0f, 0.30f, 0.45f, 1.40f, 2.80f),
                new TierDefinition(3, "Diamond Arena", "Royal Stampede Superdome", "Arena_Diamond", 25000, 45000, 1000, 2000, 25, 22, 13.0f, 0.25f, 0.40f, 1.30f, 2.60f),
                new TierDefinition(4, "Champion Arena", "Triple Crown Invitational", "Arena_Champion", 75000, 135000, 2000, 99999, 25, 25, 12.5f, 0.20f, 0.35f, 1.20f, 2.40f)
            };
        }

        public void LoadPersistedProgression(int trophies, int highestTrophies, int selectedTier, int demotionShields, int highestEverUnlockedTier)
        {
            _currentTrophies = Math.Max(0, trophies);
            _highestTrophies = Math.Max(_currentTrophies, highestTrophies);
            _activeDemotionShieldCharges = Mathf.Clamp(demotionShields, 0, MaxDemotionShieldCharges);
            _highestEverUnlockedTierIndex = Math.Max(0, highestEverUnlockedTier);
            RecalculateUnlockedTiers(true);
            SelectTier(selectedTier);
        }

        public bool SelectTier(int tierIndex)
        {
            if (tierIndex < 0 || tierIndex >= _tierDefinitions.Count || tierIndex > _highestUnlockedTierIndex) return false;
            _selectedTierIndex = tierIndex;
            OnSelectedTierChanged?.Invoke(_selectedTierIndex);
            return true;
        }

        public TierDefinition GetTierDefinition(int tierIndex)
        {
            if (_tierDefinitions == null || _tierDefinitions.Count == 0) ValidateAndSortTierDefinitions();
            return _tierDefinitions[Mathf.Clamp(tierIndex, 0, _tierDefinitions.Count - 1)];
        }

        public int EvaluateMatchTrophies(int tierIndex, bool isWinner, bool isDraw)
        {
            if (isDraw) return 0;
            TierDefinition tier = GetTierDefinition(tierIndex);
            if (isWinner) return _currentTrophies >= tier.MaxTrophyCap ? 0 : tier.TrophyWinDelta;
            if (_enableDemotionShield && _activeDemotionShieldCharges > 0 && tierIndex == _highestUnlockedTierIndex)
            {
                _activeDemotionShieldCharges--;
                OnDemotionShieldConsumed?.Invoke(_activeDemotionShieldCharges);
                return 0;
            }
            return -tier.TrophyLossDelta;
        }

        public void ApplyTrophyDelta(int delta, int tierIndex)
        {
            int oldTrophies = _currentTrophies;
            _currentTrophies = Math.Max(0, _currentTrophies + delta);
            if (_currentTrophies > _highestTrophies) _highestTrophies = _currentTrophies;
            OnTrophiesChanged?.Invoke(oldTrophies, _currentTrophies, delta, tierIndex);
            RecalculateUnlockedTiers(false);
        }

        private void RecalculateUnlockedTiers(bool suppressEvents)
        {
            int previousHighest = _highestUnlockedTierIndex;
            int newHighest = 0;
            for (int i = 0; i < _tierDefinitions.Count; i++) if (_currentTrophies >= _tierDefinitions[i].RequiredTrophiesToUnlock) newHighest = i;
            _highestUnlockedTierIndex = newHighest;
            if (suppressEvents) return;

            if (newHighest > previousHighest)
            {
                if (newHighest > _highestEverUnlockedTierIndex) { _highestEverUnlockedTierIndex = newHighest; _activeDemotionShieldCharges = _shieldChargesOnPromotion; }
                for (int t = previousHighest + 1; t <= newHighest; t++) OnTierUnlocked?.Invoke(t, _tierDefinitions[t].TierName);
                OnTierPromoted?.Invoke(previousHighest, newHighest);
                SelectTier(newHighest);
            }
            else if (newHighest < previousHighest)
            {
                OnTierDemoted?.Invoke(previousHighest, newHighest);
                if (_selectedTierIndex > newHighest) SelectTier(newHighest);
            }
        }
    }
}
