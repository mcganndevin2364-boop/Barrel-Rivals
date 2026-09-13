using System;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public enum WhipAccuracy { Miss, Early, Good, Perfect }

    public sealed class WhipBurstController
    {
        public const int MAX_WHIP_CHARGES = 3;
        public const float COOLDOWN = 2.2f;

        public int RemainingCharges { get; private set; } = MAX_WHIP_CHARGES;
        public float CooldownTimer { get; private set; }

        public void Reset()
        {
            RemainingCharges = MAX_WHIP_CHARGES;
            CooldownTimer = 0f;
        }

        public void Update(float deltaTime)
        {
            if (CooldownTimer > 0f) CooldownTimer = Mathf.Max(0f, CooldownTimer - deltaTime);
        }

        public bool TriggerWhip(float rhythmTimingOffset, out float speedBonus)
        {
            speedBonus = 0f;
            if (RemainingCharges <= 0 || CooldownTimer > 0f) return false;

            RemainingCharges--;
            CooldownTimer = COOLDOWN;

            float abs = Mathf.Abs(rhythmTimingOffset);
            if (abs <= 0.08f) speedBonus = 3.2f;
            else if (abs <= 0.18f) speedBonus = 2.0f;
            else if (abs <= 0.30f) speedBonus = 1.2f;
            else speedBonus = 0.5f;

            return true;
        }
    }
}
