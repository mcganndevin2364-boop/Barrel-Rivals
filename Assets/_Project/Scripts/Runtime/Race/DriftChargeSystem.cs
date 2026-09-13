using System;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public enum DriftTier { None, Blue, Orange, Purple }

    public sealed class DriftChargeSystem
    {
        public DriftTier CurrentTier { get; private set; } = DriftTier.None;
        public float DriftChargeDuration { get; private set; }

        public event Action<DriftTier> OnTierEscalated;

        public void UpdateDrift(bool isDrifting, float deltaTime)
        {
            if (!isDrifting)
            {
                CurrentTier = DriftTier.None;
                DriftChargeDuration = 0f;
                return;
            }

            DriftChargeDuration += deltaTime;

            DriftTier newTier = DriftTier.None;
            if (DriftChargeDuration >= 2.5f) newTier = DriftTier.Purple;
            else if (DriftChargeDuration >= 1.5f) newTier = DriftTier.Orange;
            else if (DriftChargeDuration >= 0.6f) newTier = DriftTier.Blue;

            if (newTier != CurrentTier)
            {
                CurrentTier = newTier;
                OnTierEscalated?.Invoke(CurrentTier);
            }
        }

        public float GetTurboMultiplier()
        {
            return CurrentTier switch
            {
                DriftTier.Blue => 1.06f,
                DriftTier.Orange => 1.12f,
                DriftTier.Purple => 1.20f,
                _ => 1.00f
            };
        }
    }
}
