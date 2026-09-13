using System;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public sealed class TurnManager
    {
        public const int TOTAL_ROUNDS = 3;
        public const int TOTAL_TURNS = TOTAL_ROUNDS * 2; // 6 turns total (each player runs 3 times)

        public int CurrentRound { get; private set; } = 1; // 1 to 3
        public bool IsPlayerTurn { get; private set; } = true; // Player vs Opponent
        public int TurnIndex { get; private set; } = 0; // 0 to 5
        public bool MatchComplete { get; private set; }

        public readonly float[] PlayerRoundTimes = new float[TOTAL_ROUNDS];
        public readonly float[] OpponentRoundTimes = new float[TOTAL_ROUNDS];

        public event Action<int, bool> OnTurnStarted; // (roundNumber, isPlayerTurn)
        public event Action<int, bool, float> OnRunFinished; // (roundNumber, isPlayerTurn, roundFinalTime)
        public event Action<float, float, bool> OnMatchFinished; // (playerAvgTime, opponentAvgTime, playerWon)

        public void StartMatch(bool playerGoesFirst = true)
        {
            CurrentRound = 1;
            TurnIndex = 0;
            IsPlayerTurn = playerGoesFirst;
            MatchComplete = false;

            for (int i = 0; i < TOTAL_ROUNDS; i++)
            {
                PlayerRoundTimes[i] = 0f;
                OpponentRoundTimes[i] = 0f;
            }

            OnTurnStarted?.Invoke(CurrentRound, IsPlayerTurn);
        }

        public void RecordRunTime(float finalRoundTime)
        {
            if (MatchComplete) return;

            int roundIdx = CurrentRound - 1;
            if (IsPlayerTurn)
            {
                PlayerRoundTimes[roundIdx] = finalRoundTime;
            }
            else
            {
                OpponentRoundTimes[roundIdx] = finalRoundTime;
            }

            OnRunFinished?.Invoke(CurrentRound, IsPlayerTurn, finalRoundTime);

            TurnIndex++;
            if (TurnIndex >= TOTAL_TURNS)
            {
                FinishMatch();
            }
            else
            {
                // Advance turn: switch player, and if we completed both runs, increment round
                IsPlayerTurn = !IsPlayerTurn;
                CurrentRound = (TurnIndex / 2) + 1;
                OnTurnStarted?.Invoke(CurrentRound, IsPlayerTurn);
            }
        }

        public float GetPlayerAverageTime()
        {
            float sum = 0f;
            for (int i = 0; i < TOTAL_ROUNDS; i++) sum += PlayerRoundTimes[i];
            return sum / TOTAL_ROUNDS;
        }

        public float GetOpponentAverageTime()
        {
            float sum = 0f;
            for (int i = 0; i < TOTAL_ROUNDS; i++) sum += OpponentRoundTimes[i];
            return sum / TOTAL_ROUNDS;
        }

        private void FinishMatch()
        {
            MatchComplete = true;
            float pAvg = GetPlayerAverageTime();
            float oAvg = GetOpponentAverageTime();
            bool playerWon = pAvg < oAvg; // Fastest (lowest) average time wins

            OnMatchFinished?.Invoke(pAvg, oAvg, playerWon);
        }
    }
}
