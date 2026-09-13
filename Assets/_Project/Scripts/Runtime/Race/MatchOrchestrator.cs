using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public sealed class MatchOrchestrator : MonoBehaviour
    {
        public readonly TurnManager TurnManager = new TurnManager();
        public readonly EconomyManager Economy = new EconomyManager();
        public readonly TierManager Tier = new TierManager();
        public readonly BarrelCollisionResolver CollisionResolver = new BarrelCollisionResolver();

        [SerializeField] private RaceHUD _hud;

        private void Awake()
        {
            Economy.Initialize(5000, 100);
            Tier.SelectTier(0);

            TurnManager.OnTurnStarted += HandleTurnStarted;
            TurnManager.OnMatchFinished += HandleMatchFinished;
        }

        private void OnDestroy()
        {
            TurnManager.OnTurnStarted -= HandleTurnStarted;
            TurnManager.OnMatchFinished -= HandleMatchFinished;
        }

        private void HandleTurnStarted(int round, bool isPlayerTurn)
        {
            CollisionResolver.Reset();
            if (_hud != null)
            {
                _hud.UpdateRoundInfo(round, isPlayerTurn);
                _hud.UpdatePenalty(0);
            }
        }

        private void HandleMatchFinished(float playerAvg, float opponentAvg, bool playerWon)
        {
            if (_hud != null)
            {
                _hud.UpdateScoreboard(playerAvg, opponentAvg);
            }

            if (playerWon)
            {
                long prize = Tier.CurrentTier.WinPrize;
                Economy.CreditPrize(prize, System.Guid.NewGuid().ToString());
                Economy.ModifyTrophies(Tier.CurrentTier.TrophyWin);
            }
            else
            {
                Economy.ModifyTrophies(-Tier.CurrentTier.TrophyLoss);
            }
        }
    }
}
