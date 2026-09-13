using System;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public enum ComebackSurgeTier { None = 0, AdrenalineFlow = 1, MiracleSurge = 2 }

    [DisallowMultipleComponent]
    public sealed class ComebackSurge : MonoBehaviour
    {
        [SerializeField] private bool _enableComebackSurge = true;
        [SerializeField, Min(100)] private int _tier1Deficit = 300;
        [SerializeField, Min(300)] private int _tier2Deficit = 600;

        private ComebackSurgeTier _currentTier = ComebackSurgeTier.None;
        private int _pointDeficit = 0;
        private bool _isClutchActive = false;
        private float _driftBonus = 0f;

        public event Action<ComebackSurgeTier, int> OnComebackSurgeActivated;
        public event Action OnComebackSurgeDeactivated;

        public void EvaluateComebackState(int runNumber, int runnerScore, int opponentScore, bool isRollback = false)
        {
            if (!_enableComebackSurge || runNumber < 3) { DeactivateSurge(isRollback); return; }
            _pointDeficit = opponentScore - runnerScore;
            if (_pointDeficit < _tier1Deficit) { DeactivateSurge(isRollback); return; }

            _currentTier = (_pointDeficit >= _tier2Deficit && runNumber >= 4) ? ComebackSurgeTier.MiracleSurge : ComebackSurgeTier.AdrenalineFlow;
            _driftBonus = _currentTier == ComebackSurgeTier.MiracleSurge ? 0.20f : 0.10f;
            _isClutchActive = true;

            if (!isRollback) OnComebackSurgeActivated?.Invoke(_currentTier, _pointDeficit);
        }

        public void DeactivateSurge(bool isRollback = false)
        {
            if (!_isClutchActive && _currentTier == ComebackSurgeTier.None) return;
            _currentTier = ComebackSurgeTier.None;
            _driftBonus = 0f;
            _isClutchActive = false;
            if (!isRollback) OnComebackSurgeDeactivated?.Invoke();
        }
    }
}
