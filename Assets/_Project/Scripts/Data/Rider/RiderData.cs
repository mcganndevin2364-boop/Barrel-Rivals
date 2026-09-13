using UnityEngine;

namespace BarrelRacing.Data
{
    [CreateAssetMenu(fileName = "NewRider", menuName = "BarrelRacing/Data/Rider")]
    public class RiderData : ScriptableObject
    {
        public string RiderId;
        public string DisplayName;
        public RidingStyleData Style;
        [Range(1, 100)] public float ExperienceLevel = 1f;
        public float ReactionTimeBonus = 0f;
    }
}
