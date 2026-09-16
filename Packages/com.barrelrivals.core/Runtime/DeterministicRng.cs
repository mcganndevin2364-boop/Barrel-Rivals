using System;

namespace BarrelRivals.Core
{
    /// <summary>Version 1 xorshift32 stream. Use separate instances for gameplay and presentation.
    /// This reproducible generator is not suitable for secrets or paid reward selection.</summary>
    public sealed class DeterministicRng
    {
        private uint _state;

        public DeterministicRng(uint seed)
        {
            // Xorshift's all-zero state is absorbing. Map it to a fixed nonzero seed.
            _state = seed == 0 ? 0x6D2B79F5u : seed;
        }

        public uint NextUInt()
        {
            uint value = _state;
            value ^= value << 13;
            value ^= value >> 17;
            value ^= value << 5;
            return _state = value;
        }

        /// <summary>Uniform 24-bit sample in [0, 1).</summary>
        public float NextUnitFloat() => (NextUInt() >> 8) * (1f / 16777216f);

        /// <summary>Sample between finite ordered bounds; rounding may reach the upper bound.</summary>
        public float Range(float minimum, float maximum)
        {
            if (float.IsNaN(minimum) || float.IsInfinity(minimum) ||
                float.IsNaN(maximum) || float.IsInfinity(maximum) || minimum > maximum)
                throw new ArgumentOutOfRangeException(nameof(maximum), "Bounds must be finite and ordered.");

            if (minimum == maximum) return minimum;
            // Use double for the interval so opposite finite float extremes cannot overflow it.
            return (float)(minimum + ((double)maximum - minimum) * NextUnitFloat());
        }
    }
}
