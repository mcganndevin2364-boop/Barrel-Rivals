using System;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public sealed class TurnManager
    {
        public const int MAX_ROUNDS = 4;
        public int CurrentRound { get; private set; } = 1;
        public bool IsPlayerTurn { get; private set; } = true;
        public bool MatchComplete { get; private set; }

        public event Action<int, bool> OnRoundStarted;
        public event Action<int, int> OnMatchFinished;

        public void StartMatch(bool playerGoesFirst = true)
        {
            CurrentRound = 1;
            IsPlayerTurn = playerGoesFirst;
            MatchComplete = false;
            OnRoundStarted?.Invoke(CurrentRound, IsPlayerTurn);
        }

        public void AdvanceTurn(int playerTotalScore, int opponentTotalScore)
        {
            if (MatchComplete) return;

            if (CurrentRound >= MAX_ROUNDS)
            {
                MatchComplete = true;
                OnMatchFinished?.Invoke(playerTotalScore, opponentTotalScore);
                return;
            }

            CurrentRound++;
            IsPlayerTurn = !IsPlayerTurn;
            OnRoundStarted?.Invoke(CurrentRound, IsPlayerTurn);
        }
    }
}
EOFcat << 'EOF' > Assets/_Project/Scripts/Runtime/Race/SpectatorController.cs
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public sealed class SpectatorController : MonoBehaviour
    {
        [SerializeField] private Camera _spectatorCamera;
        [SerializeField] private Transform _targetHorse;
        [SerializeField] private Vector3 _offset = new Vector3(0, 5, -10);

        private void LateUpdate()
        {
            if (_targetHorse == null || _spectatorCamera == null) return;
            _spectatorCamera.transform.position = _targetHorse.position + _offset;
            _spectatorCamera.transform.LookAt(_targetHorse.position + Vector3.up * 1.5f);
        }

        public void BindTarget(Transform target) => _targetHorse = target;
    }
}
