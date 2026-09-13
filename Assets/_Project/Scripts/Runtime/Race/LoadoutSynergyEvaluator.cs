using System.Collections.Generic;
using BarrelRacing.Data;

namespace BarrelRacing.Runtime.Race
{
    public sealed class LoadoutSynergyEvaluator
    {
        public static float EvaluateSynergyMultiplier(RiderData rider, IEnumerable<TackData> tackList)
        {
            if (rider == null || tackList == null) return 1.0f;

            int matchingItems = 0;
            foreach (var tack in tackList)
            {
                if (tack != null && rider.Style != null && tack.BoostedStat == StatType.Agility)
                {
                    matchingItems++;
                }
            }

            return 1.0f + (matchingItems * 0.02f);
        }
    }
}
