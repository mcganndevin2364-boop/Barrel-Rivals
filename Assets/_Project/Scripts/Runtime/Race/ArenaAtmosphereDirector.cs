using System;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public enum ArenaAtmospherePreset { OakRidgeMorning, DustyGulchSunset, LoneStarNoon, RoyalStampedeNight, TripleCrownTwilight }

    [DisallowMultipleComponent]
    public sealed class ArenaAtmosphereDirector : MonoBehaviour
    {
        [SerializeField] private Light _directionalSunLight;
        [SerializeField] private Light[] _stadiumFloodlights;
        [SerializeField] private ParticleSystem _victoryConfetti;

        public void ApplyTierAtmosphere(int tierIndex)
        {
            if (_directionalSunLight != null) _directionalSunLight.intensity = tierIndex >= 3 ? 0.4f : 1.2f;
            if (_stadiumFloodlights != null) for (int i = 0; i < _stadiumFloodlights.Length; i++) if (_stadiumFloodlights[i] != null) _stadiumFloodlights[i].enabled = tierIndex >= 3;
        }

        public void TriggerVictoryCelebration() { if (_victoryConfetti != null) _victoryConfetti.Play(); }
    }
}
