using System;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public sealed class ComboStreakTracker
    {
        public int PerfectTurnStreak { get; private set; }

        public void RegisterTurnAccuracy(AccuracyGrade grade)
        {
            if (grade == AccuracyGrade.Perfect) PerfectTurnStreak++;
            else PerfectTurnStreak = 0;
        }

        public void Reset() => PerfectTurnStreak = 0;
    }
}
