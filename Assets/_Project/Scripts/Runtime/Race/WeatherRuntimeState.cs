using System;
using UnityEngine;
using BarrelRacing.Data;

namespace BarrelRacing.Runtime.Race
{
    public sealed class WeatherRuntimeState
    {
        public WeatherConditionData ActiveCondition { get; private set; }
        public float CurrentWindSpeedMps { get; private set; }
        public float CurrentWindAngleDegrees { get; private set; }
        public float WindGustFactor { get; private set; }
        public float AmbientTemperatureCelsius { get; private set; }
        public float PrecipitationRateMmPerHour { get; private set; }

        public event Action<WeatherRuntimeState> OnWeatherUpdated;

        public void Initialize(WeatherConditionData initialCondition, float initialTemperature = 22f)
        {
            ActiveCondition = initialCondition;
            AmbientTemperatureCelsius = Mathf.Clamp(initialTemperature, -20f, 50f);
            CurrentWindSpeedMps = initialCondition != null ? initialCondition.BaseWindSpeedMps : 0f;
            CurrentWindAngleDegrees = 0f;
            WindGustFactor = 1f;
            PrecipitationRateMmPerHour = initialCondition != null ? initialCondition.PrecipitationRateMmPerHour : 0f;
            OnWeatherUpdated?.Invoke(this);
        }

        public void StepWeather(float deltaTime, DeterministicRng rng)
        {
            if (ActiveCondition == null) return;
            float targetWind = ActiveCondition.BaseWindSpeedMps + rng.Range(-ActiveCondition.WindGustinessMps, ActiveCondition.WindGustinessMps);
            CurrentWindSpeedMps = Mathf.MoveTowards(CurrentWindSpeedMps, Mathf.Max(0f, targetWind), deltaTime * 2f);
            WindGustFactor = ActiveCondition.BaseWindSpeedMps > 0.01f ? CurrentWindSpeedMps / ActiveCondition.BaseWindSpeedMps : 1f;
            float angleDelta = rng.Range(-15f, 15f) * deltaTime;
            CurrentWindAngleDegrees = (CurrentWindAngleDegrees + angleDelta + 360f) % 360f;
            OnWeatherUpdated?.Invoke(this);
        }
    }
}
