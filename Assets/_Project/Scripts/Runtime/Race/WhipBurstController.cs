using System;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public enum WhipTimingGrade { Miss = 0, Early = 1, Good = 2, Perfect = 3 }

    [DisallowMultipleComponent]
    public sealed class WhipBurstController : MonoBehaviour
    {
        public const int DefaultMaxWhipCharges = 3;
        [SerializeField, Min(1)] private int _maxWhipCharges = DefaultMaxWhipCharges;
        [SerializeField, Range(1.0f, 4.0f)] private float _whipCooldownSeconds = 2.2f;
        [SerializeField, Range(0.4f, 2.0f)] private float _burstDurationSeconds = 0.85f;

        private int _chargesRemaining;
        private float _cooldownRemainingSeconds = 0f;
        private float _activeBurstRemainingSeconds = 0f;
        private float _activeBurstVelocityBonus = 0f;

        public event Action<WhipTimingGrade, float, int> OnWhipTriggered;
        public event Action<int> OnWhipChargesChanged;
        public event Action<float> OnWhipCooldownProgress;

        public int ChargesRemaining => _chargesRemaining;
        public bool IsReady => _chargesRemaining > 0 && _cooldownRemainingSeconds <= 0f;
        public float CooldownNormalized => Mathf.Clamp01(_cooldownRemainingSeconds / _whipCooldownSeconds);
        public float ActiveBurstVelocityBonus => _activeBurstRemainingSeconds > 0f ? _activeBurstVelocityBonus : 0f;

        private void Awake() => ResetForNewRun(_maxWhipCharges);

        public void ResetForNewRun(int totalCharges)
        {
            _maxWhipCharges = Mathf.Max(1, totalCharges);
            _chargesRemaining = _maxWhipCharges;
            _cooldownRemainingSeconds = 0f;
            _activeBurstRemainingSeconds = 0f;
            _activeBurstVelocityBonus = 0f;
            OnWhipChargesChanged?.Invoke(_chargesRemaining);
        }

        public void SimulateTick(float deltaTime, bool isRollback = false)
        {
            if (_cooldownRemainingSeconds > 0f)
            {
                _cooldownRemainingSeconds = Mathf.Max(0f, _cooldownRemainingSeconds - deltaTime);
                if (!isRollback) OnWhipCooldownProgress?.Invoke(CooldownNormalized);
            }
            if (_activeBurstRemainingSeconds > 0f)
            {
                _activeBurstRemainingSeconds = Mathf.Max(0f, _activeBurstRemainingSeconds - deltaTime);
                if (_activeBurstRemainingSeconds <= 0f) _activeBurstVelocityBonus = 0f;
            }
        }

        public bool TriggerWhip(bool isExitingPocket, float exitAngleDeviation, bool isHomeStretch, bool isRollback = false)
        {
            if (!IsReady) return false;
            _chargesRemaining--;
            _cooldownRemainingSeconds = _whipCooldownSeconds;

            WhipTimingGrade grade = isHomeStretch || (isExitingPocket && exitAngleDeviation <= 25f) ? WhipTimingGrade.Perfect : isExitingPocket ? WhipTimingGrade.Good : WhipTimingGrade.Early;
            float impulse = grade == WhipTimingGrade.Perfect ? 3.2f : grade == WhipTimingGrade.Good ? 2.0f : 1.2f;
            int stylePts = grade == WhipTimingGrade.Perfect ? 100 : grade == WhipTimingGrade.Good ? 50 : 25;

            _activeBurstVelocityBonus = impulse;
            _activeBurstRemainingSeconds = _burstDurationSeconds;

            if (!isRollback)
            {
                OnWhipChargesChanged?.Invoke(_chargesRemaining);
                OnWhipTriggered?.Invoke(grade, impulse, stylePts);
            }
            return true;
        }
    }
}
EOF# --- File 6: ScoreRevealDirector.cs ---
cat << 'EOF' > Assets/_Project/Scripts/Runtime/Race/ScoreRevealDirector.cs
using System;
using System.Collections;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public enum RevealSequenceStep
    {
        Idle,
        Barrel1ZoneReveal,
        Barrel2ZoneReveal,
        Barrel3ZoneReveal,
        TimeScoreCountUp,
        StylePointsTally,
        MultiplierSlam,
        ScoreboardUpdate,
        Finished
    }

    [DisallowMultipleComponent]
    public sealed class ScoreRevealDirector : MonoBehaviour
    {
        private const float DefaultBarrelStepDuration = 0.55f;
        private const float DefaultTimeScoreDuration = 0.65f;
        private const float DefaultStyleTallyDuration = 0.45f;
        private const float DefaultMultiplierSlamDuration = 0.60f;
        private const float DefaultLeaderboardFlipDuration = 0.75f;
        private const float FastForwardTimeScale = 3.5f;

        [Header("Camera & Visuals")]
        [SerializeField] private SpectatorController _spectatorController;
        [SerializeField] private LineRenderer _idealArcRenderer;
        [SerializeField] private LineRenderer _actualArcRenderer;
        [SerializeField] private Transform[] _barrelWorldAnchors;

        private RevealSequenceStep _currentStep = RevealSequenceStep.Idle;
        private Coroutine _activeSequenceCoroutine;
        private float _playbackSpeedMultiplier = 1.0f;
        private bool _isFastForwarding = false;
        private RunScoreBreakdown _activeRunScore;
        private int _displayedP1Score;
        private int _displayedP2Score;

        public event Action<RevealSequenceStep> OnRevealStepStarted;
        public event Action<int, PocketAccuracyZone, int> OnBarrelZoneRevealed;
        public event Action<float, int> OnTimeScoreRevealed;
        public event Action<int> OnStyleScoreRevealed;
        public event Action<string, float> OnMultiplierSlammed;
        public event Action<int, int, int> OnScoreboardShifted;
        public event Action OnRevealSequenceComplete;

        public bool IsBusy => _currentStep != RevealSequenceStep.Idle && _currentStep != RevealSequenceStep.Finished;

        public void PlayScoreRevealSequence(RunScoreBreakdown breakdown, int p1Score, int p2Score, bool isLocalPlayer)
        {
            if (IsBusy) StopSequence();
            _activeRunScore = breakdown;
            _displayedP1Score = p1Score;
            _displayedP2Score = p2Score;
            _playbackSpeedMultiplier = 1.0f;
            _isFastForwarding = false;
            _activeSequenceCoroutine = StartCoroutine(ScoreRevealRoutine());
        }

        public void OnScreenTapFastForward()
        {
            if (!IsBusy) return;
            if (!_isFastForwarding) { _isFastForwarding = true; _playbackSpeedMultiplier = FastForwardTimeScale; }
            else { StopSequence(); InstantResolveAllSteps(); }
        }

        public void StopSequence()
        {
            if (_activeSequenceCoroutine != null) StopCoroutine(_activeSequenceCoroutine);
            _activeSequenceCoroutine = null;
            _currentStep = RevealSequenceStep.Idle;
        }

        private IEnumerator ScoreRevealRoutine()
        {
            for (int b = 0; b < 3; b++)
            {
                _currentStep = (RevealSequenceStep)((int)RevealSequenceStep.Barrel1ZoneReveal + b);
                OnRevealStepStarted?.Invoke(_currentStep);
                PocketAccuracyZone zone = _activeRunScore.AccuracyZones != null && _activeRunScore.AccuracyZones.Length > b ? _activeRunScore.AccuracyZones[b] : PocketAccuracyZone.Clean;
                int zonePts = _activeRunScore.AccuracyScores != null && _activeRunScore.AccuracyScores.Length > b ? _activeRunScore.AccuracyScores[b] : 100;
                OnBarrelZoneRevealed?.Invoke(b, zone, zonePts);
                yield return WaitForScaledSeconds(DefaultBarrelStepDuration);
            }

            _currentStep = RevealSequenceStep.TimeScoreCountUp;
            OnRevealStepStarted?.Invoke(_currentStep);
            OnTimeScoreRevealed?.Invoke(_activeRunScore.RawTimeSeconds, _activeRunScore.TimeScore);
            yield return WaitForScaledSeconds(DefaultTimeScoreDuration);

            _currentStep = RevealSequenceStep.StylePointsTally;
            OnRevealStepStarted?.Invoke(_currentStep);
            OnStyleScoreRevealed?.Invoke(_activeRunScore.StyleScore);
            yield return WaitForScaledSeconds(DefaultStyleTallyDuration);

            _currentStep = RevealSequenceStep.MultiplierSlam;
            OnRevealStepStarted?.Invoke(_currentStep);
            if (_activeRunScore.BullseyeMultiplierApplied) OnMultiplierSlammed?.Invoke("BULLSEYE! 2.0x", 2.0f);
            else if (_activeRunScore.StrikeMultiplierApplied) OnMultiplierSlammed?.Invoke("STRIKE! 1.5x", 1.5f);
            else if (_activeRunScore.SpareMultiplierApplied) OnMultiplierSlammed?.Invoke("SPARE! 1.2x", 1.2f);
            yield return WaitForScaledSeconds(DefaultMultiplierSlamDuration);

            _currentStep = RevealSequenceStep.ScoreboardUpdate;
            OnRevealStepStarted?.Invoke(_currentStep);
            OnScoreboardShifted?.Invoke(_displayedP1Score, _displayedP2Score, _displayedP1Score - _displayedP2Score);
            yield return WaitForScaledSeconds(DefaultLeaderboardFlipDuration);

            _currentStep = RevealSequenceStep.Finished;
            OnRevealSequenceComplete?.Invoke();
        }

        private IEnumerator WaitForScaledSeconds(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime * _playbackSpeedMultiplier;
                yield return null;
            }
        }

        private void InstantResolveAllSteps()
        {
            for (int b = 0; b < 3; b++)
            {
                PocketAccuracyZone zone = _activeRunScore.AccuracyZones != null && _activeRunScore.AccuracyZones.Length > b ? _activeRunScore.AccuracyZones[b] : PocketAccuracyZone.Clean;
                int zonePts = _activeRunScore.AccuracyScores != null && _activeRunScore.AccuracyScores.Length > b ? _activeRunScore.AccuracyScores[b] : 100;
                OnBarrelZoneRevealed?.Invoke(b, zone, zonePts);
            }
            OnTimeScoreRevealed?.Invoke(_activeRunScore.RawTimeSeconds, _activeRunScore.TimeScore);
            OnStyleScoreRevealed?.Invoke(_activeRunScore.StyleScore);
            if (_activeRunScore.BullseyeMultiplierApplied) OnMultiplierSlammed?.Invoke("BULLSEYE! 2.0x", 2.0f);
            else if (_activeRunScore.StrikeMultiplierApplied) OnMultiplierSlammed?.Invoke("STRIKE! 1.5x", 1.5f);
            else if (_activeRunScore.SpareMultiplierApplied) OnMultiplierSlammed?.Invoke("SPARE! 1.2x", 1.2f);
            OnScoreboardShifted?.Invoke(_displayedP1Score, _displayedP2Score, _displayedP1Score - _displayedP2Score);
            _currentStep = RevealSequenceStep.Finished;
            OnRevealSequenceComplete?.Invoke();
        }
    }
}
