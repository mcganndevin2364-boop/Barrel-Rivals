using System;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public sealed class MatchFlowController : MonoBehaviour
    {
        public enum MatchPhase { Matchmaking, TurnIntro, Racing, RevealScores, MatchComplete }
        public MatchPhase CurrentPhase { get; private set; } = MatchPhase.Matchmaking;

        public event Action<MatchPhase> OnPhaseChanged;

        public void TransitionTo(MatchPhase newPhase)
        {
            CurrentPhase = newPhase;
            OnPhaseChanged?.Invoke(newPhase);
        }
    }
}
