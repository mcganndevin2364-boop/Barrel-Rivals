using UnityEngine;

namespace BarrelRacing.Data
{
    [CreateAssetMenu(fileName = "NewHorseBreed", menuName = "BarrelRacing/Data/Horse Breed")]
    public class HorseBreedData : ScriptableObject
    {
        public string BreedId;
        public string DisplayName;
        [Range(1, 100)] public float BaseSpeed = 50f;
        [Range(1, 100)] public float BaseAgility = 50f;
        [Range(1, 100)] public float BaseTemperament = 50f;
        [Range(1, 100)] public float BaseStamina = 50f;
        [Range(1, 100)] public float BaseAcceleration = 50f;
    }
}
