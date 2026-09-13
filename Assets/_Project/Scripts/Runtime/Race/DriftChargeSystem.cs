using System;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public enum DriftTier { None = 0, Blue = 1, Orange = 2, Purple = 3 }

    [System.Serializable]
    public struct DriftStateSnapshot
    {
        public int CurrentTier;
        public float AccumulatedChargeSeconds;
        public bool IsDrifting;
        public float ActiveSurgeRemainingSeconds;
        public float ActiveSurgeMultiplier;
        public float ActiveSurgeDurationTotal;
        public int TotalDriftBoostsReleased;
        public DriftTier Tier => (DriftTier)CurrentTier;
    }

    [DisallowMultipleComponent]
    public sealed class DriftChargeSystem : MonoBehaviour
    {
        private const float MinSteerThreshold = 0.45f;
        private const float MinSpeedRatio = 0.50f;

        [SerializeField, Range(0.2f, 1.0f)] private float _blueChargeThreshold = 0.50f;
        [SerializeField, Range(0.8f, 2.0f)] private float _orangeChargeThreshold = 1.20f;
        [SerializeField, Range(1.5f, 3.5f)] private float _purpleChargeThreshold = 2.00f;
        [SerializeField, Range(0.0f, 0.5f)] private float _agilityChargeSpeedBonus = 0.30f;

        private DriftTier _currentTier = DriftTier.None;
        private float _accumulatedChargeSeconds = 0f;
        private bool _isDrifting = false;
        private float _activeSurgeRemainingSeconds = 0f;
        private float _activeSurgeMultiplier = 1.0f;
        private float _activeSurgeDurationTotal = 0f;
        private int _totalDriftBoostsReleased = 0;

        public event Action<DriftTier> OnDriftTierReached;
        public event Action<DriftTier, float, float, int> OnDriftBoostReleased;
        public event Action OnDriftCancelled;
        public event Action<float> OnDriftChargeProgress;

        public DriftTier CurrentTier => _currentTier;
        public bool IsDrifting => _isDrifting;
        public float ActiveSurgeMultiplier => _activeSurgeRemainingSeconds > 0.0f ? _activeSurgeMultiplier : 1.0f;

        public void SimulateTick(float deltaTime, float steeringInput, float currentSpeedRatio, bool inPocketZone, float agilityNorm, bool isRollback = false)
        {
            if (_activeSurgeRemainingSeconds > 0f)
            {
                _activeSurgeRemainingSeconds = Mathf.Max(0f, _activeSurgeRemainingSeconds - deltaTime);
                if (_activeSurgeRemainingSeconds <= 0f) { _activeSurgeMultiplier = 1.0f; _activeSurgeDurationTotal = 0f; }
            }

            float absSteer = Mathf.Abs(steeringInput);
            bool qualifies = (absSteer >= MinSteerThreshold || inPocketZone) && currentSpeedRatio >= MinSpeedRatio;

            if (qualifies)
            {
                _isDrifting = true;
                float agilityBonus = 1.0f + (agilityNorm * _agilityChargeSpeedBonus);
                float pocketBonus = inPocketZone ? 1.25f : 1.0f;
                _accumulatedChargeSeconds += deltaTime * agilityBonus * pocketBonus * absSteer;

                DriftTier prev = _currentTier;
                if (_accumulatedChargeSeconds >= _purpleChargeThreshold) _currentTier = DriftTier.Purple;
                else if (_accumulatedChargeSeconds >= _orangeChargeThreshold) _currentTier = DriftTier.Orange;
                else if (_accumulatedChargeSeconds >= _blueChargeThreshold) _currentTier = DriftTier.Blue;
                else _currentTier = DriftTier.None;

                if (!isRollback && _currentTier > prev) OnDriftTierReached?.Invoke(_currentTier);
                if (!isRollback) OnDriftChargeProgress?.Invoke(Mathf.Clamp01(_accumulatedChargeSeconds / _purpleChargeThreshold));
            }
            else if (_isDrifting)
            {
                if (_currentTier > DriftTier.None) ReleaseDriftBoost(isRollback);
                else CancelDrift(isRollback);
            }
        }

        public void ReleaseDriftBoost(bool isRollback = false)
        {
            if (_currentTier == DriftTier.None) { CancelDrift(isRollback); return; }

            DriftTier releasedTier = _currentTier;
            float multiplier = releasedTier == DriftTier.Purple ? 1.20f : releasedTier == DriftTier.Orange ? 1.12f : 1.06f;
            float duration = releasedTier == DriftTier.Purple ? 2.40f : releasedTier == DriftTier.Orange ? 1.80f : 1.20f;
            int stylePts = releasedTier == DriftTier.Purple ? 200 : releasedTier == DriftTier.Orange ? 100 : 50;

            _activeSurgeMultiplier = multiplier;
            _activeSurgeRemainingSeconds = duration;
            _activeSurgeDurationTotal = duration;
            _totalDriftBoostsReleased++;

            _isDrifting = false;
            _accumulatedChargeSeconds = 0f;
            _currentTier = DriftTier.None;

            if (!isRollback) OnDriftBoostReleased?.Invoke(releasedTier, multiplier, duration, stylePts);
        }

        public void CancelDrift(bool isRollback = false)
        {
            _isDrifting = false;
            _accumulatedChargeSeconds = 0f;
            _currentTier = DriftTier.None;
            if (!isRollback) OnDriftCancelled?.Invoke();
        }

        public DriftStateSnapshot GetSnapshot() => new DriftStateSnapshot { CurrentTier = (int)_currentTier, AccumulatedChargeSeconds = _accumulatedChargeSeconds, IsDrifting = _isDrifting, ActiveSurgeRemainingSeconds = _activeSurgeRemainingSeconds, ActiveSurgeMultiplier = _activeSurgeMultiplier, ActiveSurgeDurationTotal = _activeSurgeDurationTotal, TotalDriftBoostsReleased = _totalDriftBoostsReleased };
        public void RestoreSnapshot(DriftStateSnapshot s) { _currentTier = s.Tier; _accumulatedChargeSeconds = s.AccumulatedChargeSeconds; _isDrifting = s.IsDrifting; _activeSurgeRemainingSeconds = s.ActiveSurgeRemainingSeconds; _activeSurgeMultiplier = s.ActiveSurgeMultiplier; _activeSurgeDurationTotal = s.ActiveSurgeDurationTotal; _totalDriftBoostsReleased = s.TotalDriftBoostsReleased; }
    }
}
