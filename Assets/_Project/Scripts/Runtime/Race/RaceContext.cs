using UnityEngine;
using BarrelRacing.Data;

namespace BarrelRacing.Runtime.Race
{
    public sealed class RaceContext
    {
        public HorseBreedData Breed;
        public RiderData Rider;
        public TrackData Track;
        public WeatherConditionData Weather;
        public SurfaceRuntimeState SurfaceState;
        public float CurrentHorseSpeed;
        public int CurrentBarrelTarget;
        public float RunTime;
        public bool IsPlayerTurn;
    }
}
