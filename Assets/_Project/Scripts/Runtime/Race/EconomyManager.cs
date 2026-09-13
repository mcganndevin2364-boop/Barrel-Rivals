using System;
using System.Collections.Generic;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public enum TransactionReason
    {
        MatchEntryFee,
        MatchPrizePayout,
        MatchConsolationPayout,
        MatchRefundCancelled,
        DoubleDownStakeIncrease,
        UnderdogUpsetBounty,
        CleanSweepBonus,
        WinStreakBonus,
        TackRepairCost,
        DailyLoginBonus,
        AdminAdjustment,
        StorePurchase
    }

    public enum TransactionFailureReason
    {
        None,
        InsufficientCoins,
        InsufficientGems,
        InvalidAmount,
        InvalidTransactionId,
        DuplicateTransactionId,
        EscrowNotFound,
        EscrowAlreadySettled,
        WalletLocked
    }

    [System.Serializable]
    public struct MatchEscrowRecord
    {
        public string MatchId;
        public int TierIndex;
        public int BaseEntryFee;
        public int TotalPlayerEscrow;
        public int TotalPrizePool;
        public bool IsDoubledDown;
        public bool IsSettled;
        public double EscrowTimestampUtc;

        public MatchEscrowRecord(string matchId, int tierIndex, int baseEntryFee, int totalPlayerEscrow, int totalPrizePool)
        {
            MatchId = matchId;
            TierIndex = tierIndex;
            BaseEntryFee = baseEntryFee;
            TotalPlayerEscrow = totalPlayerEscrow;
            TotalPrizePool = totalPrizePool;
            IsDoubledDown = false;
            IsSettled = false;
            EscrowTimestampUtc = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }
    }

    public struct MatchSettlementReport
    {
        public string MatchId;
        public int TierIndex;
        public bool IsWinner;
        public bool IsDraw;
        public long NetCoinChange;
        public long GrossPrizeCoins;
        public long WinStreakBonusCoins;
        public long UnderdogBountyCoins;
        public long CleanSweepBonusCoins;
        public int TrophyDelta;
        public int CurrentWinStreak;
        public bool WasDoubledDown;
    }

    [DisallowMultipleComponent]
    public sealed class EconomyManager : MonoBehaviour
    {
        public const long MaxCoinCapacity = 999_999_999_999L;
        public const int MaxGemCapacity = 999_999;
        public const int WinStreakBonusThreshold = 3;
        public const float WinStreakMultiplierPerWin = 0.05f;
        public const float MaxWinStreakMultiplier = 0.25f;
        public const float CleanSweepBonusPercent = 0.10f;

        [Header("Starting Wallet")]
        [SerializeField, Min(0)] private long _startingCoins = 2500;
        [SerializeField, Min(0)] private int _startingGems = 50;

        [Header("House & Economy Tuning")]
        [SerializeField, Range(0.0f, 0.30f)] private float _standardHouseRakePercent = 0.10f;
        [SerializeField, Range(0.0f, 0.50f)] private float _consolationFeeRefundPercent = 0.0f;
        [SerializeField, Range(1.0f, 3.0f)] private float _doubleDownMultiplier = 2.0f;

        [Header("Debug")]
        [SerializeField] private bool _enableDebugLogs = false;

        private long _currentCoins;
        private int _currentGems;
        private int _currentWinStreak;
        private int _highestWinStreak;
        private long _lifetimeCoinsEarned;
        private long _lifetimeCoinsSpent;
        private int _matchesPlayed;
        private int _matchesWon;

        private readonly Dictionary<string, MatchEscrowRecord> _activeEscrows = new Dictionary<string, MatchEscrowRecord>(8);
        private readonly HashSet<string> _processedTransactionIds = new HashSet<string>(StringComparer.Ordinal);

        public event Action<long, long, TransactionReason> OnCoinsChanged;
        public event Action<int, int, TransactionReason> OnGemsChanged;
        public event Action<string, int> OnEscrowLocked;
        public event Action<string, bool, long> OnEscrowSettled;
        public event Action<string, int, string> OnEscrowRefunded;
        public event Action<string, int> OnDoubleDownAccepted;
        public event Action<TransactionFailureReason, string> OnTransactionFailed;
        public event Action<int, int> OnWinStreakChanged;

        public long CurrentCoins => _currentCoins;
        public int CurrentGems => _currentGems;
        public int CurrentWinStreak => _currentWinStreak;
        public int HighestWinStreak => _highestWinStreak;
        public long LifetimeCoinsEarned => _lifetimeCoinsEarned;
        public long LifetimeCoinsSpent => _lifetimeCoinsSpent;
        public int MatchesPlayed => _matchesPlayed;
        public int MatchesWon => _matchesWon;
        public float WinRate => _matchesPlayed > 0 ? (float)_matchesWon / _matchesPlayed : 0.0f;

        private void Awake()
        {
            _currentCoins = Math.Max(0L, Math.Min(_startingCoins, MaxCoinCapacity));
            _currentGems = Mathf.Clamp(_startingGems, 0, MaxGemCapacity);
        }

        public void LoadPersistedWallet(long coins, int gems, int winStreak, int highestStreak, long lifetimeEarned, long lifetimeSpent, int matchesPlayed, int matchesWon)
        {
            _currentCoins = Math.Max(0L, Math.Min(coins, MaxCoinCapacity));
            _currentGems = Mathf.Clamp(gems, 0, MaxGemCapacity);
            _currentWinStreak = Mathf.Max(0, winStreak);
            _highestWinStreak = Mathf.Max(_currentWinStreak, highestStreak);
            _lifetimeCoinsEarned = Math.Max(0L, Math.Min(lifetimeEarned, MaxCoinCapacity));
            _lifetimeCoinsSpent = Math.Max(0L, Math.Min(lifetimeSpent, MaxCoinCapacity));
            _matchesPlayed = Mathf.Max(0, matchesPlayed);
            _matchesWon = Mathf.Max(0, matchesWon);
        }

        public bool CanAffordEntryFee(int entryFee) => entryFee >= 0 && _currentCoins >= entryFee;
        public bool CanAffordGems(int gemAmount) => gemAmount >= 0 && _currentGems >= gemAmount;

        public bool LockMatchEscrow(string matchId, int tierIndex, int entryFee, int calculatedPrizePool, string transactionId)
        {
            if (string.IsNullOrEmpty(matchId)) { RaiseFailure(TransactionFailureReason.InvalidAmount, "MatchId is empty"); return false; }
            if (string.IsNullOrEmpty(transactionId)) { RaiseFailure(TransactionFailureReason.InvalidTransactionId, "TransactionId is empty"); return false; }
            if (_activeEscrows.ContainsKey(matchId) || _processedTransactionIds.Contains(transactionId)) { RaiseFailure(TransactionFailureReason.DuplicateTransactionId, transactionId); return false; }
            if (entryFee < 0 || _currentCoins < entryFee) { RaiseFailure(TransactionFailureReason.InsufficientCoins, "Insufficient balance"); return false; }

            long previousCoins = _currentCoins;
            _currentCoins -= entryFee;
            _lifetimeCoinsSpent = SafeAddCoins(_lifetimeCoinsSpent, entryFee);
            _processedTransactionIds.Add(transactionId);

            var escrow = new MatchEscrowRecord(matchId, tierIndex, entryFee, entryFee, calculatedPrizePool);
            _activeEscrows.Add(matchId, escrow);

            OnCoinsChanged?.Invoke(previousCoins, _currentCoins, TransactionReason.MatchEntryFee);
            OnEscrowLocked?.Invoke(matchId, entryFee);
            return true;
        }

        public bool ApplyDoubleDown(string matchId, string transactionId)
        {
            if (!_activeEscrows.TryGetValue(matchId, out MatchEscrowRecord escrow)) { RaiseFailure(TransactionFailureReason.EscrowNotFound, matchId); return false; }
            if (escrow.IsDoubledDown) { RaiseFailure(TransactionFailureReason.InvalidAmount, "Already doubled down"); return false; }
            if (string.IsNullOrEmpty(transactionId) || _processedTransactionIds.Contains(transactionId)) { RaiseFailure(TransactionFailureReason.DuplicateTransactionId, transactionId); return false; }

            int additionalCost = escrow.BaseEntryFee;
            if (_currentCoins < additionalCost) { RaiseFailure(TransactionFailureReason.InsufficientCoins, "Cannot afford Double Down"); return false; }

            long previousCoins = _currentCoins;
            _currentCoins -= additionalCost;
            _lifetimeCoinsSpent = SafeAddCoins(_lifetimeCoinsSpent, additionalCost);
            _processedTransactionIds.Add(transactionId);

            escrow.TotalPlayerEscrow += additionalCost;
            escrow.TotalPrizePool = Mathf.RoundToInt(escrow.TotalPrizePool * _doubleDownMultiplier);
            escrow.IsDoubledDown = true;
            _activeEscrows[matchId] = escrow;

            OnCoinsChanged?.Invoke(previousCoins, _currentCoins, TransactionReason.DoubleDownStakeIncrease);
            OnDoubleDownAccepted?.Invoke(matchId, escrow.TotalPrizePool);
            return true;
        }

        public MatchSettlementReport SettleMatch(string matchId, bool isWinner, bool isDraw, bool isCleanSweep, bool isUnderdogUpset, int trophyDelta, string settlementTransactionId)
        {
            if (!_activeEscrows.TryGetValue(matchId, out MatchEscrowRecord escrow) || escrow.IsSettled) return default;
            if (!string.IsNullOrEmpty(settlementTransactionId)) _processedTransactionIds.Add(settlementTransactionId);

            _matchesPlayed++;
            long grossPrizeCoins = 0;
            long winStreakBonusCoins = 0;
            long underdogBountyCoins = 0;
            long cleanSweepBonusCoins = 0;
            long netCoinChange = -escrow.TotalPlayerEscrow;

            if (isWinner)
            {
                _matchesWon++;
                _currentWinStreak++;
                if (_currentWinStreak > _highestWinStreak) _highestWinStreak = _currentWinStreak;

                grossPrizeCoins = escrow.TotalPrizePool;
                if (_currentWinStreak >= WinStreakBonusThreshold)
                {
                    int streakCount = _currentWinStreak - WinStreakBonusThreshold + 1;
                    float streakMultiplier = Mathf.Min(streakCount * WinStreakMultiplierPerWin, MaxWinStreakMultiplier);
                    winStreakBonusCoins = Mathf.RoundToInt(grossPrizeCoins * streakMultiplier);
                }

                if (isCleanSweep) cleanSweepBonusCoins = Mathf.RoundToInt(grossPrizeCoins * CleanSweepBonusPercent);
                if (isUnderdogUpset) underdogBountyCoins = Mathf.RoundToInt(escrow.BaseEntryFee * 0.25f);

                long totalCredit = grossPrizeCoins + winStreakBonusCoins + cleanSweepBonusCoins + underdogBountyCoins;
                long previousCoins = _currentCoins;
                _currentCoins = SafeAddCoins(_currentCoins, totalCredit);
                _lifetimeCoinsEarned = SafeAddCoins(_lifetimeCoinsEarned, totalCredit);
                netCoinChange = totalCredit - escrow.TotalPlayerEscrow;

                OnCoinsChanged?.Invoke(previousCoins, _currentCoins, TransactionReason.MatchPrizePayout);
                OnEscrowSettled?.Invoke(matchId, true, totalCredit);
            }
            else if (isDraw)
            {
                long refund = escrow.TotalPlayerEscrow;
                long previousCoins = _currentCoins;
                _currentCoins = SafeAddCoins(_currentCoins, refund);
                netCoinChange = 0;
                OnCoinsChanged?.Invoke(previousCoins, _currentCoins, TransactionReason.MatchRefundCancelled);
                OnEscrowSettled?.Invoke(matchId, false, refund);
            }
            else
            {
                _currentWinStreak = 0;
                if (_consolationFeeRefundPercent > 0.001f)
                {
                    long consolation = Mathf.RoundToInt(escrow.TotalPlayerEscrow * _consolationFeeRefundPercent);
                    long previousCoins = _currentCoins;
                    _currentCoins = SafeAddCoins(_currentCoins, consolation);
                    netCoinChange += consolation;
                    OnCoinsChanged?.Invoke(previousCoins, _currentCoins, TransactionReason.MatchConsolationPayout);
                }
                OnEscrowSettled?.Invoke(matchId, false, 0);
            }

            _activeEscrows.Remove(matchId);
            OnWinStreakChanged?.Invoke(_currentWinStreak, GetWinStreakBonusPercent(_currentWinStreak));

            return new MatchSettlementReport
            {
                MatchId = matchId,
                TierIndex = escrow.TierIndex,
                IsWinner = isWinner,
                IsDraw = isDraw,
                NetCoinChange = netCoinChange,
                GrossPrizeCoins = grossPrizeCoins,
                WinStreakBonusCoins = winStreakBonusCoins,
                UnderdogBountyCoins = underdogBountyCoins,
                CleanSweepBonusCoins = cleanSweepBonusCoins,
                TrophyDelta = trophyDelta,
                CurrentWinStreak = _currentWinStreak,
                WasDoubledDown = escrow.IsDoubledDown
            };
        }

        public bool RefundAbortedMatch(string matchId, string reason)
        {
            if (!_activeEscrows.TryGetValue(matchId, out MatchEscrowRecord escrow) || escrow.IsSettled) return false;
            long refund = escrow.TotalPlayerEscrow;
            long previousCoins = _currentCoins;
            _currentCoins = SafeAddCoins(_currentCoins, refund);
            _lifetimeCoinsSpent = Math.Max(0L, _lifetimeCoinsSpent - refund);
            _activeEscrows.Remove(matchId);
            OnCoinsChanged?.Invoke(previousCoins, _currentCoins, TransactionReason.MatchRefundCancelled);
            OnEscrowRefunded?.Invoke(matchId, (int)refund, reason);
            return true;
        }

        public int GetWinStreakBonusPercent(int streak)
        {
            if (streak < WinStreakBonusThreshold) return 0;
            int count = streak - WinStreakBonusThreshold + 1;
            return Mathf.RoundToInt(Mathf.Min(count * WinStreakMultiplierPerWin, MaxWinStreakMultiplier) * 100f);
        }

        private long SafeAddCoins(long current, long delta)
        {
            try { return Math.Max(0L, Math.Min(checked(current + delta), MaxCoinCapacity)); }
            catch (OverflowException) { return delta > 0 ? MaxCoinCapacity : 0L; }
        }

        private void RaiseFailure(TransactionFailureReason reason, string details)
        {
            if (_enableDebugLogs) Debug.LogWarning($"[EconomyManager] {reason}: {details}");
            OnTransactionFailed?.Invoke(reason, details);
        }
    }
}
