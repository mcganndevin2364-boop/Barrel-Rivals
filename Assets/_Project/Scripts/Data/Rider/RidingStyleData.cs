using UnityEngine;

namespace BarrelRacing.Data
{
    [CreateAssetMenu(fileName = "NewRidingStyle", menuName = "BarrelRacing/Data/Riding Style")]
    public class RidingStyleData : ScriptableObject
    {
        public string StyleId;
        public string DisplayName;
        public float TurnTightnessBonus = 1.0f;
        public float WhipEfficiencyBonus = 1.0f;
        public float RecoverySpeedBonus = 1.0f;
    }
}
