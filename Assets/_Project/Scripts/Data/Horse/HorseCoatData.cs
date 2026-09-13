using UnityEngine;

namespace BarrelRacing.Data
{
    [CreateAssetMenu(fileName = "NewHorseCoat", menuName = "BarrelRacing/Data/Horse Coat")]
    public class HorseCoatData : ScriptableObject
    {
        public string CoatId;
        public string DisplayName;
        public Color PrimaryColor = Color.white;
        public Color SecondaryColor = Color.gray;
        public Texture2D CoatPatternTexture;
    }
}
