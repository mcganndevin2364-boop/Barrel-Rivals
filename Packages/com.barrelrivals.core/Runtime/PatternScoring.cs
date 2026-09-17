using System;
using System.Collections.Generic;

namespace BarrelRivals.Core
{
    public enum PatternKind { Circle, Triangle, Square }
    public enum SkillGrade { Bad, Good, Great, Perfect }
    public enum TraceReason { Accepted, Missing, Invalid, TooSmall, OpenShape, ExcessLength, LowAccuracy }

    public readonly struct TracePoint
    {
        public double X { get; }
        public double Y { get; }
        public TracePoint(double x, double y) { X = x; Y = y; }
    }

    public readonly struct TraceEvaluation
    {
        public int Quality { get; }
        public int SpeedBonus { get; }
        public int Score { get; }
        public TraceReason Reason { get; }
        public SkillGrade Grade => Score >= 90 ? SkillGrade.Perfect : Score >= 75 ? SkillGrade.Great : Score >= 55 ? SkillGrade.Good : SkillGrade.Bad;
        public TraceEvaluation(int quality, int speedBonus, TraceReason reason)
        { Quality = quality; SpeedBonus = speedBonus; Score = (quality * 9 + 5) / 10 + speedBonus; Reason = reason; }
    }

    /// <summary>Practice rules v1. Screen-independent, orientation-preserving closed-template grading.
    /// This is a quality grader for assigned shapes, not identity recognition or an anti-cheat system.</summary>
    public static class PatternScoring
    {
        public const int SampleCount = 64;
        public const int MaximumPoints = 512;

        public static TracePoint[] Template(PatternKind kind)
        {
            switch (kind)
            {
                case PatternKind.Circle:
                    var circle = new TracePoint[65];
                    for (int i = 0; i < circle.Length; i++)
                    {
                        double angle = 2 * Math.PI * i / 64;
                        circle[i] = new TracePoint(0.5 + 0.34 * Math.Cos(angle), 0.5 + 0.34 * Math.Sin(angle));
                    }
                    return circle;
                case PatternKind.Triangle:
                    return new[] { new TracePoint(.5, .86), new TracePoint(.14, .18), new TracePoint(.86, .18), new TracePoint(.5, .86) };
                case PatternKind.Square:
                    return new[] { new TracePoint(.17, .17), new TracePoint(.83, .17), new TracePoint(.83, .83), new TracePoint(.17, .83), new TracePoint(.17, .17) };
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        public static TraceEvaluation Evaluate(IReadOnlyList<TracePoint> trace, PatternKind kind, long durationMs)
        {
            if (trace == null || trace.Count < 4) return Failure(TraceReason.Missing);
            if (trace.Count > MaximumPoints || durationMs < 80 || durationMs > PracticeRun.DrawingWindowMs)
                return Failure(TraceReason.Invalid);
            double minX = 1, minY = 1, maxX = 0, maxY = 0;
            for (int i = 0; i < trace.Count; i++)
            {
                TracePoint p = trace[i];
                if (!Finite(p.X) || !Finite(p.Y) || p.X < 0 || p.X > 1 || p.Y < 0 || p.Y > 1)
                    return Failure(TraceReason.Invalid);
                minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
                minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
            }
            double extent = Math.Max(maxX - minX, maxY - minY);
            if (extent < .28 || Math.Min(maxX - minX, maxY - minY) < .15) return Failure(TraceReason.TooSmall);
            double closure = Distance(trace[0], trace[trace.Count - 1]) / extent;
            if (closure > .28) return Failure(TraceReason.OpenShape);

            TracePoint[] input = Normalize(trace, minX, minY, maxX, maxY);
            TracePoint[] target = Normalize(Template(kind));
            double lengthRatio = Length(input) / Length(target);
            if (lengthRatio < .65 || lengthRatio > 1.6) return Failure(TraceReason.ExcessLength);
            TracePoint[] samples = ResampleClosed(input);
            TracePoint[] reference = ResampleClosed(target);
            double error = double.MaxValue;
            // Closed shapes accept any start point and either traversal direction, but preserve orientation/aspect.
            for (int shift = 0; shift < SampleCount; shift++)
                for (int direction = -1; direction <= 1; direction += 2)
                {
                    double sum = 0;
                    for (int i = 0; i < SampleCount; i++)
                    {
                        int index = (shift + direction * i + SampleCount) % SampleCount;
                        sum += Distance(samples[i], reference[index]);
                    }
                    error = Math.Min(error, sum / SampleCount);
                }
            // Allow the small phase quantization error from cyclic 64-sample alignment.
            double geometric = Clamp01(1 - Math.Max(0,error-.025) / .22);
            double closureQuality = Clamp01(1 - closure / .28);
            double lengthQuality = Clamp01(1 - Math.Abs(lengthRatio - 1) / .6);
            int quality = (int)Math.Round(100 * geometric * (.75 + .15 * closureQuality + .10 * lengthQuality));
            // Speed cannot rescue a poor shape. Duration is bounded by the owned drawing window.
            int bonus = quality >= 70 ? (int)Math.Round(10 * Clamp01(1 - durationMs / (double)PracticeRun.DrawingWindowMs)) : 0;
            return new TraceEvaluation(quality, bonus, quality >= 55 ? TraceReason.Accepted : TraceReason.LowAccuracy);
        }

        public static TraceEvaluation Failure(TraceReason reason) => new TraceEvaluation(0, 0, reason);
        internal static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        internal static double Distance(TracePoint a, TracePoint b) => Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y));
        private static double Clamp01(double value) => Math.Max(0, Math.Min(1, value));
        private static TracePoint[] Normalize(IReadOnlyList<TracePoint> points)
        {
            double x0=double.MaxValue, y0=double.MaxValue, x1=double.MinValue, y1=double.MinValue;
            for (int i=0;i<points.Count;i++) { var p=points[i]; x0=Math.Min(x0,p.X); x1=Math.Max(x1,p.X); y0=Math.Min(y0,p.Y); y1=Math.Max(y1,p.Y); }
            return Normalize(points,x0,y0,x1,y1);
        }
        private static TracePoint[] Normalize(IReadOnlyList<TracePoint> points,double x0,double y0,double x1,double y1)
        {
            double scale=Math.Max(x1-x0,y1-y0), cx=(x0+x1)/2, cy=(y0+y1)/2;
            var result=new TracePoint[points.Count];
            for(int i=0;i<points.Count;i++) result[i]=new TracePoint((points[i].X-cx)/scale,(points[i].Y-cy)/scale);
            return result;
        }
        private static double Length(IReadOnlyList<TracePoint> points)
        { double length=0; for(int i=1;i<points.Count;i++) length+=Distance(points[i-1],points[i]); return length; }
        private static TracePoint[] ResampleClosed(TracePoint[] points)
        {
            var closed=new TracePoint[points.Length+1]; Array.Copy(points,closed,points.Length); closed[closed.Length-1]=points[0];
            double total=Length(closed), traversed=0; int segment=1;
            var result=new TracePoint[SampleCount];
            for(int i=0;i<SampleCount;i++)
            {
                double desired=i*total/SampleCount, length=Distance(closed[segment-1],closed[segment]);
                while(segment<closed.Length-1 && traversed+length<desired)
                { traversed+=length; segment++; length=Distance(closed[segment-1],closed[segment]); }
                double t=length>1e-12 ? (desired-traversed)/length : 0;
                var a=closed[segment-1]; var b=closed[segment];
                result[i]=new TracePoint(a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t);
            }
            return result;
        }
    }
}
