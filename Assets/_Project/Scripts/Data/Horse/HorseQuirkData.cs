using UnityEngine;

namespace BarrelRacing.Data
{
    [CreateAssetMenu(fileName = "NewHorseQuirk", menuName = "BarrelRacing/Data/Horse Quirk")]
    public class HorseQuirkData : ScriptableObject
    {
        public string QuirkId;
        public string DisplayName;
        [TextArea] public string Description;
        public StatType AffectedStat;
        public float StatModifier;
        public float TriggerChance = 1.0f;

        public virtual bool EvaluateTrigger(object context)
        {
            return true;
        }
    }
}
