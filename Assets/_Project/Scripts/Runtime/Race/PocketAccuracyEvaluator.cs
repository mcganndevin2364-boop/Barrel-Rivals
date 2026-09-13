using System;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public enum AccuracyGrade { Perfect, Clean, Wide, Miss }

    public sealed class PocketAccuracyEvaluator
    {
        public struct BarrelTurnSummary
        {
            public float ApexDistance;
            public AccuracyGrade Grade;
            public int BasePoints;
        }

        public readonly BarrelTurnSummary[] Summaries = new BarrelTurnSummary[3];

        public void EvaluateBarrel(int barrelIndex, float apexDistance)
        {
            if (barrelIndex < 0 || barrelIndex >= 3) return;

            AccuracyGrade grade;
            int points;

            if (apexDistance <= 0.50f)
            {
                grade = AccuracyGrade.Perfect;
                points = 250;
            }
            else if (apexDistance <= 1.50f)
            {
                grade = AccuracyGrade.Clean;
                points = 100;
            }
            else if (apexDistance <= 3.0f)
            {
                grade = AccuracyGrade.Wide;
                points = 25;
            }
            else
            {
                grade = AccuracyGrade.Miss;
                points = 0;
            }

            Summaries[barrelIndex] = new BarrelTurnSummary
            {
                ApexDistance = apexDistance,
                Grade = grade,
                BasePoints = points
            };
        }

        public int GetTotalAccuracyPoints()
        {
            int sum = 0;
            for (int i = 0; i < 3; i++) sum += Summaries[i].BasePoints;
            return sum;
        }
    }
}
