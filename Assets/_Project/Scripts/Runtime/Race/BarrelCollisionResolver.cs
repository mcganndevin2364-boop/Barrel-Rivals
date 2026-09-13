using System;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public sealed class BarrelCollisionResolver
    {
        public const float KNOCK_PENALTY_SECONDS = 5.0f;
        public int TotalKnocks { get; private set; }
        public readonly bool[] KnockedBarrels = new bool[3];

        public event Action<int, Vector3> OnBarrelKnocked;

        public void Reset()
        {
            TotalKnocks = 0;
            for (int i = 0; i < 3; i++) KnockedBarrels[i] = false;
        }

        public bool RegisterBarrelHit(int barrelIndex, Vector3 hitDirection)
        {
            if (barrelIndex < 0 || barrelIndex >= 3) return false;
            if (KnockedBarrels[barrelIndex]) return false;

            KnockedBarrels[barrelIndex] = true;
            TotalKnocks++;
            OnBarrelKnocked?.Invoke(barrelIndex, hitDirection);
            return true;
        }

        public float CalculateTotalPenaltySeconds() => TotalKnocks * KNOCK_PENALTY_SECONDS;
    }
}
