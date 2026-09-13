using System;
using UnityEngine;
using BarrelRacing.Data;

namespace BarrelRacing.Runtime.Race
{
    public sealed class SurfaceRuntimeState
    {
        public TrackSurfaceData ActiveSurface { get; private set; }
        public float DynamicGripModifier { get; private set; } = 1.0f;
        public float DynamicDragModifier { get; private set; } = 1.0f;

        public void SetSurface(TrackSurfaceData surface)
        {
            ActiveSurface = surface;
            DynamicGripModifier = surface != null ? surface.GripCoefficient : 1.0f;
            DynamicDragModifier = surface != null ? surface.RollingResistance : 0.05f;
        }

        public void ApplyRutDegradation(float severity)
        {
            DynamicGripModifier = Mathf.Clamp(DynamicGripModifier - severity * 0.05f, 0.7f, 1.3f);
        }
    }
}
