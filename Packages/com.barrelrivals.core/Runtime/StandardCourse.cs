using System;
namespace BarrelRivals.Core
{
    /// <summary>Version 1 layout in metres. Score line is Z=0; route validation is separate.</summary>
    public static class StandardCourse
    {
        public const double FeetToMetres = 0.3048;
        public const double FirstPairDistance = 90 * FeetToMetres;
        public const double ThirdBarrelDistance = 105 * FeetToMetres;
        public const double ScoreLineOffset = 60 * FeetToMetres;
        public readonly struct Point
        {
            public double X { get; }
            public double Z { get; }
            public Point(double x, double z) { X = x; Z = z; }
        }
        public static Point Barrel(int index)
        {
            double half = FirstPairDistance / 2;
            switch (index)
            {
                case 0: return new Point(-half, ScoreLineOffset);
                case 1: return new Point(half, ScoreLineOffset);
                case 2: return new Point(0, ScoreLineOffset + Math.Sqrt(
                    ThirdBarrelDistance * ThirdBarrelDistance - half * half));
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
}
