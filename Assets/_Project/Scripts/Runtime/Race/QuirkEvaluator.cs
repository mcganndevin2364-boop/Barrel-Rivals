using System;
using System.Collections.Generic;
using UnityEngine;
using BarrelRacing.Data;

namespace BarrelRacing.Runtime.Race
{
    public sealed class QuirkEvaluator
    {
        public readonly struct EvaluatedQuirkImpact
        {
            public readonly HorseQuirkData Quirk;
            public readonly float ModifierValue;
            public readonly bool IsActive;

            public EvaluatedQuirkImpact(HorseQuirkData quirk, float modifierValue, bool isActive)
            {
                Quirk = quirk;
                ModifierValue = modifierValue;
                IsActive = isActive;
            }
        }

        private readonly List<HorseQuirkData> _quirks = new List<HorseQuirkData>(4);

        public void Initialize(IEnumerable<HorseQuirkData> quirks)
        {
            _quirks.Clear();
            if (quirks != null) _quirks.AddRange(quirks);
        }

        public float EvaluateStatModifier(StatType stat, RaceContext context)
        {
            float total = 0f;
            for (int i = 0; i < _quirks.Count; i++)
            {
                var q = _quirks[i];
                if (q != null && q.AffectedStat == stat && q.EvaluateTrigger(context))
                {
                    total += q.StatModifier;
                }
            }
            return total;
        }
    }
}
