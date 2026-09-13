using System;
using System.Collections.Generic;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    [DisallowMultipleComponent]
    public sealed class MatchOrchestrator : MonoBehaviour
    {
        [SerializeField] private MomentumSystem _momentumSystem;
        [SerializeField] private SplitTimeTracker _splitTracker;
        [SerializeField] private DriftChargeSystem _driftSystem;
        [SerializeField] private WhipBurstController _whipController;
        [SerializeField] private BarrelCollisionResolver _collisionResolver;
        [SerializeField] private PocketAccuracyEvaluator _pocketEvaluator;
        [SerializeField] private MatchFlowController _flowController;
        [SerializeField] private TurnManager _turnManager;
        [SerializeField] private ScoreRevealDirector _revealDirector;
        [SerializeField] private ComboStreakTracker _comboTracker;
        [SerializeField] private ComebackSurge _comebackSurge;
        [SerializeField] private InputManager _inputManager;
        [SerializeField] private CameraDirector _cameraDirector;
        [SerializeField] private VFXDirector _vfxDirector;
        [SerializeField] private AudioDirector _audioDirector;
        [SerializeField] private RaceHUD _raceHUD;
        [SerializeField] private OpponentGhostRenderer _ghostRenderer;
        [SerializeField] private NetworkedRaceState _networkedState;

        private bool _isLocalTurn = false;
        private readonly List<GhostPlaybackKeyframe> _activeKeyframes = new List<GhostPlaybackKeyframe>(512);

        private void Awake()
        {
            if (_flowController != null)
            {
                _flowController.OnCountdownTick += count => { if (_raceHUD != null) { if (count >= 0) _raceHUD.ShowCountdown(count); else _raceHUD.HideCountdown(); } };
                _flowController.OnRunStarted += (num, local) => { _isLocalTurn = local; if (_comboTracker != null) _comboTracker.ResetForNewRun(); if (_whipController != null) _whipController.ResetForNewRun(3); if (_momentumSystem != null) _momentumSystem.SetInputLock(!local); };
            }
            if (_collisionResolver != null) _collisionResolver.OnBarrelKnockedDown += (idx, pos) => { if (_raceHUD != null) _raceHUD.ShowBarrelKnockPenalty(-500); if (_cameraDirector != null) _cameraDirector.AddTrauma(0.85f); };
        }

        private void FixedUpdate()
        {
            if (_flowController == null || _flowController.CurrentState != MatchFlowState.RunInProgress || !_isLocalTurn) return;
            float dt = Time.fixedDeltaTime;

            var input = _inputManager != null ? _inputManager.SampleAndConsumeInputFrame() : RaceInputFrame.Empty;
            bool inPocket = _pocketEvaluator != null && _pocketEvaluator.IsCurrentlyInPocket;

            if (_driftSystem != null) { _driftSystem.SimulateTick(dt, input.Steering, _momentumSystem != null ? _momentumSystem.CurrentSpeedRatio : 0f, inPocket, 0.5f); if (input.DriftReleaseTriggered) _driftSystem.ReleaseDriftBoost(); }
            if (_whipController != null) { _whipController.SimulateTick(dt); if (input.WhipTriggered) _whipController.TriggerWhip(inPocket, 0f, false); }
            if (_momentumSystem != null) _momentumSystem.SimulateTick(dt, input.Steering, input.Throttle, _driftSystem != null ? _driftSystem.ActiveSurgeMultiplier : 1.0f, _whipController != null ? _whipController.ActiveBurstVelocityBonus : 0f);
            if (_splitTracker != null) _splitTracker.SimulateTick(dt);
            if (_comboTracker != null) { _comboTracker.SetInPocketZone(inPocket); _comboTracker.SimulateTick(dt); }
        }

        private void Update()
        {
            if (_raceHUD != null && _splitTracker != null) { _raceHUD.UpdateRaceTimer(_splitTracker.ElapsedTimeSeconds); _raceHUD.UpdateSplitDelta(_splitTracker.CurrentSplitDelta); }
            if (_raceHUD != null && _momentumSystem != null) _raceHUD.UpdateSpeedometer(_momentumSystem.CurrentSpeedMps, _momentumSystem.TargetMaxSpeedMps);
            if (_cameraDirector != null && _momentumSystem != null) _cameraDirector.UpdateSpeedRatio(_momentumSystem.CurrentSpeedRatio);
        }
    }
}
