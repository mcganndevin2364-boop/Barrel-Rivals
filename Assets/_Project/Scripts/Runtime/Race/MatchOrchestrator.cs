using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public sealed class MatchOrchestrator : MonoBehaviour
    {
        public readonly TurnManager TurnManager = new TurnManager();
        public readonly EconomyManager Economy = new EconomyManager();
        public readonly TierManager Tier = new TierManager();

        private void Awake()
        {
            Economy.Initialize(5000, 100);
            Tier.SelectTier(0);
        }
    }
}
