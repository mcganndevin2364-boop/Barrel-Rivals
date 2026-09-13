using System;
using System.Collections.Generic;
using UnityEngine;
using BarrelRacing.Data;

namespace BarrelRacing.Runtime.Race
{
    public sealed class ZoneTracker
    {
        private readonly List<TrackZoneData> _trackZones = new List<TrackZoneData>(8);
        public TrackZoneData ActiveZone { get; private set; }
        public TrackSurfaceData ActiveSurfaceOverride { get; private set; }
        public float CurrentZoneProgressNormalized { get; private set; }

        public event Action<TrackZoneData> OnZoneEntered;
        public event Action<TrackZoneData> OnZoneExited;

        public void InitializeZones(IEnumerable<TrackZoneData> zones)
        {
            _trackZones.Clear();
            if (zones != null) _trackZones.AddRange(zones);
            ActiveZone = null;
            ActiveSurfaceOverride = null;
            CurrentZoneProgressNormalized = 0f;
        }

        public void UpdatePosition(Vector3 horsePosition, float horseSpeed)
        {
            TrackZoneData foundZone = null;
            for (int i = 0; i < _trackZones.Count; i++)
            {
                var zone = _trackZones[i];
                if (zone != null && zone.ContainsPoint(horsePosition))
                {
                    foundZone = zone;
                    CurrentZoneProgressNormalized = zone.CalculateProgress(horsePosition);
                    break;
                }
            }

            if (foundZone != ActiveZone)
            {
                if (ActiveZone != null) OnZoneExited?.Invoke(ActiveZone);
                ActiveZone = foundZone;
                ActiveSurfaceOverride = foundZone?.SurfaceOverride;
                if (ActiveZone != null) OnZoneEntered?.Invoke(ActiveZone);
            }
        }
    }
}
