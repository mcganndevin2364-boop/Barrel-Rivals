using System.Collections.Generic;
using UnityEngine;

namespace BarrelRacing.Data
{
    [CreateAssetMenu(fileName = "NewTrack", menuName = "BarrelRacing/Data/Track Layout")]
    public class TrackData : ScriptableObject
    {
        public string TrackId;
        public string ArenaName;
        public Vector3 Barrel1Position = new Vector3(-8.84f, 0, 18.28f);
        public Vector3 Barrel2Position = new Vector3(8.84f, 0, 18.28f);
        public Vector3 Barrel3Position = new Vector3(0, 0, 32.0f);
        public Vector3 StartGatePosition = new Vector3(0, 0, 0);
        public TrackSurfaceData DefaultSurface;
        public List<TrackZoneData> TrackZones = new List<TrackZoneData>();
    }
}
