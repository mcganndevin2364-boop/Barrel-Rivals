using System;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public enum ComboActionType { GateLaunchClean, GateLaunchLegendary, PocketEntryClean, DriftBlueRelease, DriftOrangeRelease, DriftPurpleRelease, WhipGood, WhipPerfect, BarrelNearMiss, NeedleThreadClean, PocketExitClean }

    [DisallowMultipleComponent]
    public sealed class ComboStreakTracker : MonoBehaviour
    {
        [SerializeField, Range(2.0f, 5.0f)] private float _comboDecayWindow = 3.2f;
        [SerializeField] private bool _pauseDecayInPocket = true;

        private int _currentStreakCount = 0;
        private int _highestStreakCount = 0;
        private float _decayTimerRemaining = 0f;
        private int _totalComboPointsEarned = 0;
        private float _currentMultiplier = 1.0f;
        private bool _isInPocketZone = false;

        public event Action<ComboActionType, int, int, float> OnComboActionRegistered;
        public event Action<int> OnComboDropped;

        public int CurrentStreakCount => _currentStreakCount;
        public float CurrentMultiplier => _currentMultiplier;
        public float DecayTimerNormalized => _comboDecayWindow > 0f ? Mathf.Clamp01(_decayTimerRemaining / _comboDecayWindow) : 0f;

        public void ResetForNewRun() { _currentStreakCount = 0; _decayTimerRemaining = 0f; _totalComboPointsEarned = 0; _currentMultiplier = 1.0f; }
        public void SetInPocketZone(bool inPocket) => _isInPocketZone = inPocket;

        public void SimulateTick(float deltaTime, bool isRollback = false)
        {
            if (_currentStreakCount <= 0 || (_pauseDecayInPocket && _isInPocketZone)) return;
            if (_decayTimerRemaining > 0f)
            {
                _decayTimerRemaining -= deltaTime;
                if (_decayTimerRemaining <= 0f)
                {
                    int dropped = _currentStreakCount;
                    _currentStreakCount = 0; _currentMultiplier = 1.0f;
                    if (!isRollback) OnComboDropped?.Invoke(dropped);
                }
            }
        }

        public void RegisterAction(ComboActionType action, int basePoints, bool isRollback = false)
        {
            _currentStreakCount++;
            if (_currentStreakCount > _highestStreakCount) _highestStreakCount = _currentStreakCount;
            _decayTimerRemaining = _comboDecayWindow;

            _currentMultiplier = _currentStreakCount >= 12 ? 2.50f : _currentStreakCount >= 9 ? 2.00f : _currentStreakCount >= 6 ? 1.50f : _currentStreakCount >= 3 ? 1.25f : 1.0f;
            int earned = Mathf.RoundToInt(basePoints * _currentMultiplier);
            _totalComboPointsEarned += earned;

            if (!isRollback) OnComboActionRegistered?.Invoke(action, earned, _currentStreakCount, _currentMultiplier);
        }
    }
}
