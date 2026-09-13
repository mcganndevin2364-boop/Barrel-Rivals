using UnityEngine;

namespace BarrelRacing.Data
{
    [CreateAssetMenu(fileName = "NewWeatherCondition", menuName = "BarrelRacing/Data/Weather Condition")]
    public class WeatherConditionData : ScriptableObject
    {
        public string WeatherId;
        public string DisplayName;
        public float BaseWindSpeedMps = 2.0f;
        public float WindGustinessMps = 1.0f;
        public float PrecipitationRateMmPerHour = 0f;
        public float AmbientFogDensity = 0.005f;
    }
}
