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
