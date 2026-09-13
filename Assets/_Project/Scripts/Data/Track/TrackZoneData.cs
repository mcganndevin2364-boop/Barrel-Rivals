using UnityEngine;

namespace BarrelRacing.Data
{
    [CreateAssetMenu(fileName = "NewTrackZone", menuName = "BarrelRacing/Data/Track Zone")]
    public class TrackZoneData : ScriptableObject
    {
        public string ZoneId;
        public Vector3 ZoneCenter;
        public float ZoneRadius = 10f;
        public TrackSurfaceData SurfaceOverride;

        public bool ContainsPoint(Vector3 point)
        {
            return Vector3.Distance(point, ZoneCenter) <= ZoneRadius;
        }

        public float CalculateProgress(Vector3 point)
        {
            float dist = Vector3.Distance(point, ZoneCenter);
            return Mathf.Clamp01(1.0f - (dist / ZoneRadius));
        }
    }
}
