using System;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public sealed class RunScoringSystem
    {
        public struct RunScoreBreakdown
        {
            public int TimeScore;
            public int AccuracyScore;
            public int StyleScore;
            public int PenaltyScore;
            public float Multiplier;
            public int FinalScore;
        }

        public static int CalculateTimeScore(float runTime, float parTime)
        {
            float diff = runTime - parTime;
            if (diff <= -2.0f) return 1000;
            if (diff <= -1.0f) return 900;
            if (diff <= 0.0f)  return 750;
            if (diff <= 2.0f)  return 500;
            if (diff <= 5.0f)  return 250;
            return 100;
        }

        public static RunScoreBreakdown CalculateTotalRunScore(
            float runTime,
            float parTime,
            int accuracyPoints,
            int stylePoints,
            int penalties,
            float multiplier = 1.0f)
        {
            int timeScore = CalculateTimeScore(runTime, parTime);
            int rawTotal = Mathf.Max(0, timeScore + accuracyPoints + stylePoints + penalties);
            int finalScore = Mathf.RoundToInt(rawTotal * Mathf.Max(1.0f, multiplier));

            return new RunScoreBreakdown
            {
                TimeScore = timeScore,
                AccuracyScore = accuracyPoints,
                StyleScore = stylePoints,
                PenaltyScore = penalties,
                Multiplier = multiplier,
                FinalScore = finalScore
            };
        }
    }
}
