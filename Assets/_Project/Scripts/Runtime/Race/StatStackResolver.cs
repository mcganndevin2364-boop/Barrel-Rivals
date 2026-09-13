using System.Collections.Generic;
using UnityEngine;
using BarrelRacing.Data;

namespace BarrelRacing.Runtime.Race
{
    public sealed class StatStackResolver
    {
        public static HorseStatBlock ResolveStats(
            HorseBreedData breed,
            RiderData rider,
            IEnumerable<TackData> tackList,
            QuirkEvaluator quirks,
            RaceContext context)
        {
            HorseStatBlock stats = HorseStatBlock.FromBreed(breed);

            if (tackList != null)
            {
                foreach (var tack in tackList)
                {
                    if (tack != null)
                    {
                        stats.ApplyModifier(tack.BoostedStat, tack.StatBonus);
                    }
                }
            }

            if (rider != null && rider.Style != null)
            {
                stats.ApplyModifier(StatType.Agility, (rider.Style.TurnTightnessBonus - 1.0f) * 10f);
            }

            if (quirks != null && context != null)
            {
                stats.ApplyModifier(StatType.Speed, quirks.EvaluateStatModifier(StatType.Speed, context));
                stats.ApplyModifier(StatType.Agility, quirks.EvaluateStatModifier(StatType.Agility, context));
                stats.ApplyModifier(StatType.Temperament, quirks.EvaluateStatModifier(StatType.Temperament, context));
                stats.ApplyModifier(StatType.Stamina, quirks.EvaluateStatModifier(StatType.Stamina, context));
            }

            return stats;
        }
    }
}
