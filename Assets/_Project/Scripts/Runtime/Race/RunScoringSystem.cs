using System;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public sealed class RunScoringSystem
    {
        public struct RunResult
        {
            public float RawTime;
            public int KnockedBarrelsCount;
            public float PenaltySeconds;
            public float FinalRoundTime; // RawTime + PenaltySeconds
        }

        public struct MatchAverageSummary
        {
            public float[] RoundTimes;
            public float AverageTime;
            public int TotalKnockedBarrels;
        }

        public static RunResult CalculateRoundResult(float rawElapsedSeconds, int knockedBarrels)
        {
            float penalty = knockedBarrels * BarrelCollisionResolver.KNOCK_PENALTY_SECONDS;
            float finalTime = Mathf.Max(0.1f, rawElapsedSeconds + penalty);

            return new RunResult
            {
                RawTime = rawElapsedSeconds,
                KnockedBarrelsCount = knockedBarrels,
                PenaltySeconds = penalty,
                FinalRoundTime = finalTime
            };
        }

        public static MatchAverageSummary CalculateMatchAverage(float[] roundTimes, int totalKnocks)
        {
            if (roundTimes == null || roundTimes.Length == 0)
            {
                return new MatchAverageSummary { RoundTimes = Array.Empty<float>(), AverageTime = 0f, TotalKnockedBarrels = 0 };
            }

            float sum = 0f;
            for (int i = 0; i < roundTimes.Length; i++)
            {
                sum += roundTimes[i];
            }

            return new MatchAverageSummary
            {
                RoundTimes = (float[])roundTimes.Clone(),
                AverageTime = sum / roundTimes.Length,
                TotalKnockedBarrels = totalKnocks
            };
        }
    }
}
