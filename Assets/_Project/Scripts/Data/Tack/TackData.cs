using UnityEngine;

namespace BarrelRacing.Data
{
    [CreateAssetMenu(fileName = "NewTackItem", menuName = "BarrelRacing/Data/Tack Item")]
    public class TackData : ScriptableObject
    {
        public string ItemId;
        public string DisplayName;
        public TackSlot Slot;
        public StatType BoostedStat;
        public float StatBonus = 5f;
        [Range(0f, 100f)] public float MaxDurability = 100f;
    }
}
