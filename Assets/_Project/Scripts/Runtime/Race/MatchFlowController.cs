using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public enum MatchFlowState
    {
        Uninitialized,
        MatchmakingLobby,
        PreMatchIntro,
        DoubleDownWindow,
        PreRunCountdown,
        RunInProgress,
        PostRunScoreReveal,
        TurnHandover,
        TiebreakerNotice,
        MatchCompleteEvaluation,
        RematchPrompt,
        AbortedOrDisconnected
    }

    [DisallowMultipleComponent]
    public sealed class MatchFlowController : MonoBehaviour
    {
        private const float PreMatchIntroDurationSec = 3.5f;
        private const float DoubleDownWindowSec = 4.0f;
        private const float PreRunCountdownSec = 3.0f;
        private const float TurnHandoverDurationSec = 2.0f;
        private const float MatchCompletePodiumDurationSec = 5.0f;

        [Header("Dependencies")]
        [SerializeField] private TurnManager _turnManager;
        [SerializeField] private ScoreRevealDirector _scoreRevealDirector;
        [SerializeField] private EconomyManager _economyManager;
        [SerializeField] private TierManager _tierManager;
        [SerializeField] private SpectatorController _spectatorController;
        [SerializeField] private MomentumSystem _momentumSystem;
        [SerializeField] private RunScoringSystem _scoringSystem;

        [Header("Tuning")]
        [SerializeField, Min(1)] private int _maxMatchRuns = 4;
        [SerializeField, Min(1)] private int _maxTiebreakers = 2;
        [SerializeField] private bool _enableDoubleDownPhase = true;

        private MatchFlowState _currentState = MatchFlowState.Uninitialized;
        private string _activeMatchId;
        private int _activeTierIndex;
        private bool _isLocalPlayerTurn;
        private Coroutine _activeFlowCoroutine;
        private int _player1TotalScore;
        private int _player2TotalScore;
        private readonly List<RunScoreBreakdown> _matchHistory = new List<RunScoreBreakdown>(8);

        public event Action<MatchFlowState> OnMatchFlowStateChanged;
        public event Action<string, int, int> OnMatchIntroStarted;
        public event Action<float> OnDoubleDownWindowStarted;
        public event Action<int> OnCountdownTick;
        public event Action<int, bool> OnRunStarted;
        public event Action<MatchSettlementReport> OnMatchConcluded;
        public event Action<string> OnMatchAborted;

        public MatchFlowState CurrentState => _currentState;
        public int Player1TotalScore => _player1TotalScore;
        public int Player2TotalScore => _player2TotalScore;

        private void Awake()
        {
            if (_scoreRevealDirector != null) _scoreRevealDirector.OnRevealSequenceComplete += HandleScoreRevealComplete;
        }

        private void OnDestroy()
        {
            if (_scoreRevealDirector != null) _scoreRevealDirector.OnRevealSequenceComplete -= HandleScoreRevealComplete;
        }

        public bool StartMatch(string matchId, int tierIndex, string player1Id, string player2Id, bool isLocalPlayer1)
        {
            _activeMatchId = matchId;
            _activeTierIndex = tierIndex;
            _player1TotalScore = 0;
            _player2TotalScore = 0;
            _matchHistory.Clear();

            TierDefinition tier = _tierManager != null ? _tierManager.GetTierDefinition(tierIndex) : default;
            if (_economyManager != null && !_economyManager.LockMatchEscrow(matchId, tierIndex, tier.EntryFee, tier.WinPrize, $"escrow_{matchId}"))
            {
                OnMatchAborted?.Invoke("Insufficient coins for entry.");
                return false;
            }

            if (_turnManager != null) _turnManager.InitializeMatch(matchId, player1Id, player2Id, isLocalPlayer1);
            TransitionTo(MatchFlowState.PreMatchIntro);
            return true;
        }

        private void TransitionTo(MatchFlowState newState)
        {
            if (_activeFlowCoroutine != null) StopCoroutine(_activeFlowCoroutine);
            _currentState = newState;
            OnMatchFlowStateChanged?.Invoke(_currentState);

            switch (_currentState)
            {
                case MatchFlowState.PreMatchIntro: _activeFlowCoroutine = StartCoroutine(PreMatchIntroRoutine()); break;
                case MatchFlowState.DoubleDownWindow: _activeFlowCoroutine = StartCoroutine(DoubleDownRoutine()); break;
                case MatchFlowState.PreRunCountdown: _activeFlowCoroutine = StartCoroutine(PreRunCountdownRoutine()); break;
                case MatchFlowState.RunInProgress: _activeFlowCoroutine = StartCoroutine(RunInProgressRoutine()); break;
                case MatchFlowState.TurnHandover: _activeFlowCoroutine = StartCoroutine(TurnHandoverRoutine()); break;
                case MatchFlowState.MatchCompleteEvaluation: _activeFlowCoroutine = StartCoroutine(MatchEvaluationRoutine()); break;
            }
        }

        private IEnumerator PreMatchIntroRoutine()
        {
            TierDefinition tier = _tierManager != null ? _tierManager.GetTierDefinition(_activeTierIndex) : default;
            OnMatchIntroStarted?.Invoke(_activeMatchId, _activeTierIndex, tier.WinPrize);
            if (_spectatorController != null) _spectatorController.SetCameraMode(SpectatorCameraMode.WideArena);
            yield return new WaitForSeconds(PreMatchIntroDurationSec);
            TransitionTo(_enableDoubleDownPhase ? MatchFlowState.DoubleDownWindow : MatchFlowState.PreRunCountdown);
        }

        private IEnumerator DoubleDownRoutine()
        {
            OnDoubleDownWindowStarted?.Invoke(DoubleDownWindowSec);
            yield return new WaitForSeconds(DoubleDownWindowSec);
            TransitionTo(MatchFlowState.PreRunCountdown);
        }

        private IEnumerator PreRunCountdownRoutine()
        {
            _isLocalPlayerTurn = _turnManager != null && _turnManager.IsLocalPlayerTurn;
            if (_momentumSystem != null) { _momentumSystem.TeleportToGate(Vector3.zero, Quaternion.identity); _momentumSystem.SetInputLock(true); }
            if (!_isLocalPlayerTurn && _spectatorController != null) _spectatorController.SetCameraMode(SpectatorCameraMode.GhostChase);

            for (int count = 3; count > 0; count--) { OnCountdownTick?.Invoke(count); yield return new WaitForSeconds(1.0f); }
            OnCountdownTick?.Invoke(0);
            yield return new WaitForSeconds(0.2f);
            TransitionTo(MatchFlowState.RunInProgress);
        }

        private IEnumerator RunInProgressRoutine()
        {
            int runNum = _turnManager != null ? _turnManager.CurrentRunNumber : 1;
            OnRunStarted?.Invoke(runNum, _isLocalPlayerTurn);
            if (_isLocalPlayerTurn && _momentumSystem != null) _momentumSystem.SetInputLock(false);
            if (_turnManager != null) _turnManager.NotifyRunStarted();
            yield return null;
        }

        public void CompleteActiveRun(RunScoreBreakdown scoreBreakdown)
        {
            if (_currentState != MatchFlowState.RunInProgress) return;
            if (_momentumSystem != null) _momentumSystem.SetInputLock(true);

            _matchHistory.Add(scoreBreakdown);
            if (_isLocalPlayerTurn) _player1TotalScore += scoreBreakdown.FinalRunScore;
            else _player2TotalScore += scoreBreakdown.FinalRunScore;

            _currentState = MatchFlowState.PostRunScoreReveal;
            OnMatchFlowStateChanged?.Invoke(_currentState);

            if (_scoreRevealDirector != null) _scoreRevealDirector.PlayScoreRevealSequence(scoreBreakdown, _player1TotalScore, _player2TotalScore, _isLocalPlayerTurn);
            else HandleScoreRevealComplete();
        }

        private void HandleScoreRevealComplete()
        {
            int totalRuns = _matchHistory.Count;
            if (totalRuns >= _maxMatchRuns)
            {
                if (_player1TotalScore == _player2TotalScore && totalRuns < (_maxMatchRuns + _maxTiebreakers)) TransitionTo(MatchFlowState.TiebreakerNotice);
                else TransitionTo(MatchFlowState.MatchCompleteEvaluation);
            }
            else TransitionTo(MatchFlowState.TurnHandover);
        }

        private IEnumerator TurnHandoverRoutine()
        {
            if (_turnManager != null) _turnManager.AdvanceToNextTurn();
            yield return new WaitForSeconds(TurnHandoverDurationSec);
            TransitionTo(MatchFlowState.PreRunCountdown);
        }

        private IEnumerator MatchEvaluationRoutine()
        {
            bool isWinner = _player1TotalScore > _player2TotalScore;
            bool isDraw = _player1TotalScore == _player2TotalScore;
            int trophyDelta = 0;

            if (_tierManager != null)
            {
                trophyDelta = _tierManager.EvaluateMatchTrophies(_activeTierIndex, isWinner, isDraw);
                _tierManager.ApplyTrophyDelta(trophyDelta, _activeTierIndex);
            }

            MatchSettlementReport report = default;
            if (_economyManager != null)
            {
                report = _economyManager.SettleMatch(_activeMatchId, isWinner, isDraw, false, false, trophyDelta, $"settle_{_activeMatchId}");
            }

            OnMatchConcluded?.Invoke(report);
            yield return new WaitForSeconds(MatchCompletePodiumDurationSec);
            _currentState = MatchFlowState.RematchPrompt;
            OnMatchFlowStateChanged?.Invoke(_currentState);
        }
    }
}
