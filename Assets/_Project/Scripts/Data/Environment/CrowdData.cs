using UnityEngine;

namespace BarrelRacing.Data
{
    [CreateAssetMenu(fileName = "NewCrowdConfig", menuName = "BarrelRacing/Data/Crowd Config")]
    public class CrowdData : ScriptableObject
    {
        public string CrowdId;
        [Range(0f, 1f)] public float BaseExcitement = 0.3f;
        [Range(0f, 1f)] public float MaxVolume = 1.0f;
        public float DecayRatePerSecond = 0.15f;
    }
}
EOFcat << 'EOF' > Assets/_Project/Scripts/Runtime/Race/DeterministicRng.cs
using System;

namespace BarrelRacing.Runtime.Race
{
    public sealed class DeterministicRng
    {
        private ulong _s0;
        private ulong _s1;

        public DeterministicRng(ulong seed)
        {
            SetSeed(seed);
        }

        public void SetSeed(ulong seed)
        {
            _s0 = seed == 0 ? 0x853c49e6748fea9bUL : seed;
            _s1 = (_s0 ^ 0xda3e39cb94b95bdbUL) + 0x9e3779b97f4a7c15UL;
        }

        public ulong NextUlong()
        {
            ulong x = _s0;
            ulong y = _s1;
            _s0 = y;
            x ^= x << 23;
            _s1 = x ^ y ^ (x >> 17) ^ (y >> 26);
            return _s1 + y;
        }

        public float Value()
        {
            return (NextUlong() >> 40) * (1.0f / (1 << 24));
        }

        public float Range(float min, float max)
        {
            return min + (max - min) * Value();
        }

        public int Range(int min, int max)
        {
            if (min >= max) return min;
            return min + (int)(NextUlong() % (ulong)(max - min));
        }
    }
}
