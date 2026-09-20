using System;

namespace BarrelRivals.Core.Reins
{
    public enum ReinsPhase { Ready, Approach, Racing, Drive, Complete, Cancelled, TimedOut }
    public enum ReinsLaunchOutcome { Pending, Perfect, Good, Weak, TimedOut }
    public enum DriveSide { None, Left, Right }
    public enum ReinsSurface { HardPack, LooseSand, TackyClay, MuddySlop, Mixed }
    public enum ReinsTimingGrade { None, Miss, Good, Great, Perfect }
    public enum ReinsPocketZone { None, Safe, Risk, Pocket, Kiss }

    /// <summary>Bounded movement temperament, not learning, purchased timing tolerance or artificial input lag.</summary>
    public sealed class ReinsHorseProfile
    {
        public int NervePermille { get; }
        public int FirePermille { get; }
        public int BiddabilityPermille { get; }
        public int HeartPermille { get; }
        public ReinsHorseProfile(int nervePermille = 500, int firePermille = 500, int biddabilityPermille = 500, int heartPermille = 500)
        {
            Validate(nervePermille); Validate(firePermille); Validate(biddabilityPermille); Validate(heartPermille);
            NervePermille = nervePermille; FirePermille = firePermille;
            BiddabilityPermille = biddabilityPermille; HeartPermille = heartPermille;
        }
        private static void Validate(int value)
        { if (value < 0 || value > 1000) throw new ArgumentOutOfRangeException(nameof(value)); }
    }

    public readonly struct ReinsSurfaceTuning
    {
        public ReinsSurface Surface { get; }
        public double SpeedMultiplier { get; }
        public double GripMultiplier { get; }
        internal ReinsSurfaceTuning(ReinsSurface surface, double speed, double grip)
        { Surface = surface; SpeedMultiplier = speed; GripMultiplier = grip; }
    }

    /// <summary>Both riders receive the same explicit surface/round/seed contract. No run-order degradation.</summary>
    public sealed class ReinsManifest
    {
        public uint Seed { get; }
        public ReinsSurface Surface { get; }
        public int RoundIndex { get; }
        public ReinsHorseProfile Horse { get; }
        public ReinsManifest(uint seed, ReinsSurface surface = ReinsSurface.HardPack, int roundIndex = 0, ReinsHorseProfile horse = null)
        {
            if (surface < ReinsSurface.HardPack || surface > ReinsSurface.Mixed) throw new ArgumentOutOfRangeException(nameof(surface));
            if (roundIndex < 0 || roundIndex > 2) throw new ArgumentOutOfRangeException(nameof(roundIndex));
            Seed = seed; Surface = surface; RoundIndex = roundIndex; Horse = horse ?? new ReinsHorseProfile();
        }
        public ReinsSurface SurfaceAt(double x, double z)
        {
            if (!ReinsMath.Finite(x) || !ReinsMath.Finite(z)) throw new ArgumentOutOfRangeException(nameof(x));
            if (Surface != ReinsSurface.Mixed) return Surface;
            // Six broad, readable patches. Same coordinates and manifest always select the same patch.
            uint cell = (uint)((x < 0 ? 0 : 1) + (z < 12 ? 0 : z < 34 ? 2 : 4));
            uint hash = unchecked(Seed * 747796405u + cell * 2891336453u + 277803737u);
            return (ReinsSurface)((hash ^ (hash >> 16)) % 4);
        }
        public ReinsSurfaceTuning TuningAt(double x, double z)
        {
            ReinsSurface surface = SurfaceAt(x, z);
            double speed = 1, grip = 1;
            switch (surface)
            {
                case ReinsSurface.HardPack: speed = 1.05; grip = 1.10; break;
                case ReinsSurface.LooseSand: speed = .92; grip = .85; break;
                case ReinsSurface.TackyClay: speed = 1.02; grip = 1.20; break;
                case ReinsSurface.MuddySlop: speed = .88; grip = .75; break;
            }
            return new ReinsSurfaceTuning(surface, speed * (1 - .015 * RoundIndex), grip * (1 - .035 * RoundIndex));
        }
    }

    /// <summary>One 20ms input frame. Cadence/Drive are events; LaunchHeld is sampled state after Start arms the launch at tick zero.</summary>
    public readonly struct ReinsInput
    {
        public int LeftPermille { get; }
        public int RightPermille { get; }
        public bool CadenceTap { get; }
        public bool LaunchHeld { get; }
        public bool Wrap { get; }
        public DriveSide Drive { get; }
        public ReinsInput(int leftPermille = 0, int rightPermille = 0, bool cadenceTap = false,
            bool launchHeld = false, bool wrap = false, DriveSide drive = DriveSide.None)
        {
            if (leftPermille < 0 || leftPermille > 1000) throw new ArgumentOutOfRangeException(nameof(leftPermille));
            if (rightPermille < 0 || rightPermille > 1000) throw new ArgumentOutOfRangeException(nameof(rightPermille));
            if (drive < DriveSide.None || drive > DriveSide.Right) throw new ArgumentOutOfRangeException(nameof(drive));
            LeftPermille = leftPermille; RightPermille = rightPermille; CadenceTap = cadenceTap;
            LaunchHeld = launchHeld; Wrap = wrap; Drive = drive;
        }
    }

    internal static class ReinsMath
    {
        public static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        public static double Clamp(double value, double low, double high) => Math.Max(low, Math.Min(high, value));
        public static double Distance(double x, double z, double otherX, double otherZ)
            => Math.Sqrt((x - otherX) * (x - otherX) + (z - otherZ) * (z - otherZ));
        public static double Angle(double value)
        {
            while (value > Math.PI) value -= Math.PI * 2;
            while (value < -Math.PI) value += Math.PI * 2;
            return value;
        }
    }
}
