using System;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public sealed class BarrelProximityTension
    {
        public const float PROXIMITY_THRESHOLD = 3.0f;
        public const float THREAD_NEEDLE_THRESHOLD = 0.35f;

        public float ClosestDistance { get; private set; } = float.MaxValue;
        public int ActiveBarrelIndex { get; private set; } = -1;
        public float TensionIntensity { get; private set; }

        public event Action<int, float> OnNearMissDetected;
        public event Action<int> OnThreadTheNeedle;

        private bool _needleFired;

        public void Reset()
        {
            ClosestDistance = float.MaxValue;
            ActiveBarrelIndex = -1;
            TensionIntensity = 0f;
            _needleFired = false;
        }

        public void UpdateProximity(Vector3 horsePos, Vector3 barrelPos, int barrelIndex)
        {
            ActiveBarrelIndex = barrelIndex;
            float dist = Vector3.Distance(horsePos, barrelPos);
            ClosestDistance = Mathf.Min(ClosestDistance, dist);

            if (dist <= PROXIMITY_THRESHOLD)
            {
                TensionIntensity = 1.0f - Mathf.Clamp01(dist / PROXIMITY_THRESHOLD);
                if (dist <= THREAD_NEEDLE_THRESHOLD && !_needleFired)
                {
                    _needleFired = true;
                    OnThreadTheNeedle?.Invoke(barrelIndex);
                }
                else if (dist <= 0.6f && !_needleFired)
                {
                    OnNearMissDetected?.Invoke(barrelIndex, dist);
                }
            }
            else
            {
                TensionIntensity = 0f;
            }
        }
    }
}
