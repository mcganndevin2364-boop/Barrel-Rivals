using System;

namespace BarrelRivals.Core.Reins
{
    /// <summary>Prototype swept circular contact and ordered continuous winding. Inputs are positions from the
    /// simulation, not client-authoritative route messages. Double geometry still needs cross-runtime qualification.</summary>
    public sealed class ReinsCourseJudge
    {
        public const double HorseRadius = .42;
        public const double BarrelRadius = .40;
        public const double ContactRadius = HorseRadius + BarrelRadius;
        public const double KissInnerRadius = 1.0;
        public const double KissOuterRadius = 1.2;
        public const double PocketOuterRadius = 2.2;
        public const double RiskOuterRadius = 3.5;
        public const double EntryRadius = 5.0;
        public const double RequiredWindingRadians = 4.35;
        public int BarrelIndex { get; private set; }
        public int KnockCount { get; private set; }
        public int StylePoints { get; private set; }
        public double TurnProgress01 => ReinsMath.Clamp(_winding / RequiredWindingRadians, 0, 1);
        public bool TurnActive => _turnActive;
        public ReinsPocketZone LastCompletedZone { get; private set; }
        private readonly bool[] _knocked = new bool[3];
        private readonly int[] _styles = new int[3];
        private bool _turnActive, _canEnter = true;
        private double _angle, _winding, _bestWinding, _closest = double.MaxValue;

        public bool IsBarrelKnocked(int index)
        {
            if (index < 0 || index > 2) throw new ArgumentOutOfRangeException(nameof(index));
            return _knocked[index];
        }
        public static int TurnDirection(int index)
        {
            if (index < 0 || index > 2) throw new ArgumentOutOfRangeException(nameof(index));
            return index == 0 ? -1 : 1;
        }
        public static ReinsPocketZone ZoneForDistance(double distance)
        {
            if (!ReinsMath.Finite(distance) || distance < KissInnerRadius || distance > EntryRadius) return ReinsPocketZone.None;
            if (distance <= KissOuterRadius) return ReinsPocketZone.Kiss;
            if (distance <= PocketOuterRadius) return ReinsPocketZone.Pocket;
            if (distance <= RiskOuterRadius) return ReinsPocketZone.Risk;
            return ReinsPocketZone.Safe;
        }

        /// <returns>True only on the single frame that legally completes the next ordered turn.</returns>
        public bool Step(double previousX, double previousZ, double x, double z)
        {
            if (!ReinsMath.Finite(previousX) || !ReinsMath.Finite(previousZ) || !ReinsMath.Finite(x) || !ReinsMath.Finite(z))
                throw new ArgumentOutOfRangeException(nameof(x));
            double moved = ReinsMath.Distance(previousX, previousZ, x, z);
            if (moved > 2) { ResetTurn(false); return false; }
            for (int i = 0; i < 3; i++)
            {
                var barrel = StandardCourse.Barrel(i);
                if (!_knocked[i] && SweptDistance(previousX, previousZ, x, z, barrel.X, barrel.Z) <= ContactRadius)
                {
                    _knocked[i] = true; KnockCount++; StylePoints -= _styles[i]; _styles[i] = 0;
                }
            }
            if (BarrelIndex == 3 || moved < 1e-10) return false;
            var target = StandardCourse.Barrel(BarrelIndex);
            double distance = ReinsMath.Distance(x, z, target.X, target.Z);
            double previousDistance = ReinsMath.Distance(previousX, previousZ, target.X, target.Z);
            if (distance > 7.5) { ResetTurn(true); return false; }
            if (!_canEnter && distance > EntryRadius + .5) _canEnter = true;
            if (!_turnActive)
            {
                if (!_canEnter || distance > EntryRadius || distance >= previousDistance) return false;
                var source = BarrelIndex == 0 ? new StandardCourse.Point(0, -9) : StandardCourse.Barrel(BarrelIndex - 1);
                double incomingLength = ReinsMath.Distance(source.X, source.Z, target.X, target.Z);
                double dot = ((x - target.X) * (source.X - target.X) + (z - target.Z) * (source.Z - target.Z)) / Math.Max(.001, distance * incomingLength);
                if (dot < .34) return false;
                _turnActive = true; _canEnter = false; _angle = Math.Atan2(x - target.X, z - target.Z);
                _winding = _bestWinding = 0; _closest = distance;
                return false;
            }
            double angle = Math.Atan2(x - target.X, z - target.Z);
            double change = ReinsMath.Angle(angle - _angle) * TurnDirection(BarrelIndex);
            _angle = angle; _winding += change; _bestWinding = Math.Max(_bestWinding, _winding);
            _closest = Math.Min(_closest, SweptDistance(previousX, previousZ, x, z, target.X, target.Z));
            // Net angle cannot be farmed with alternating left/right sweeps. A substantial reversal requires re-entry.
            if (_bestWinding - _winding > .6) { ResetTurn(false); return false; }
            if (_winding < RequiredWindingRadians || distance < 2 || distance > 6.5 || distance <= previousDistance + .000001) return false;
            var next = BarrelIndex < 2 ? StandardCourse.Barrel(BarrelIndex + 1) : new StandardCourse.Point(0, 0);
            double destinationLength = ReinsMath.Distance(x, z, next.X, next.Z);
            double outgoing = ((x - previousX) * (next.X - x) + (z - previousZ) * (next.Z - z)) / Math.Max(.001, moved * destinationLength);
            if (outgoing < .65) return false;
            LastCompletedZone = _knocked[BarrelIndex] ? ReinsPocketZone.None : ZoneForDistance(_closest);
            int style = LastCompletedZone == ReinsPocketZone.Kiss ? 1000 : LastCompletedZone == ReinsPocketZone.Pocket ? 350 : LastCompletedZone == ReinsPocketZone.Risk ? 100 : 0;
            _styles[BarrelIndex] = style; StylePoints += style; BarrelIndex++; ResetTurn(true);
            return true;
        }

        private void ResetTurn(bool allowEntry)
        { _turnActive = false; _canEnter = allowEntry; _winding = _bestWinding = 0; _closest = double.MaxValue; }

        public static double SweptDistance(double ax, double az, double bx, double bz, double cx, double cz)
        {
            double dx = bx - ax, dz = bz - az, squared = dx * dx + dz * dz;
            double t = squared > 1e-15 ? ReinsMath.Clamp(((cx - ax) * dx + (cz - az) * dz) / squared, 0, 1) : 0;
            return ReinsMath.Distance(ax + t * dx, az + t * dz, cx, cz);
        }
    }
}
