using UnityEngine;

namespace BarrelRacing.Data
{
    [CreateAssetMenu(fileName = "NewTrackSurface", menuName = "BarrelRacing/Data/Track Surface")]
    public class TrackSurfaceData : ScriptableObject
    {
        public string SurfaceId;
        public SurfaceType Type;
        public float GripCoefficient = 1.0f;
        public float RollingResistance = 0.05f;
        public Color DirtParticleColor = new Color(0.6f, 0.4f, 0.2f);
    }
}
