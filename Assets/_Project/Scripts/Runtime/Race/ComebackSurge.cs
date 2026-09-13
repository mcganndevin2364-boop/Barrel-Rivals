using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public sealed class ComebackSurge
    {
        public static float EvaluateSurgeMultiplier(int trailingPoints)
        {
            if (trailingPoints >= 1000) return 1.30f;
            if (trailingPoints >= 500)  return 1.15f;
            return 1.0f;
        }
    }
}
