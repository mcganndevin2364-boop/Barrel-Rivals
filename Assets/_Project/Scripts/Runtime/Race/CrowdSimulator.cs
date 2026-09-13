using System;
using UnityEngine;
using BarrelRacing.Data;

namespace BarrelRacing.Runtime.Race
{
    public sealed class CrowdSimulator
    {
        public CrowdData Data { get; private set; }
        public float CurrentExcitement { get; private set; }
        public float CurrentVolume { get; private set; }

        public event Action<float> OnExcitementLevelChanged;

        public void Initialize(CrowdData data)
        {
            Data = data;
            CurrentExcitement = data != null ? data.BaseExcitement : 0.2f;
            CurrentVolume = data != null ? data.BaseExcitement * data.MaxVolume : 0.2f;
        }

        public void InjectExcitement(float amount)
        {
            CurrentExcitement = Mathf.Clamp01(CurrentExcitement + Mathf.Max(0f, amount));
            CurrentVolume = Data != null ? CurrentExcitement * Data.MaxVolume : CurrentExcitement;
            OnExcitementLevelChanged?.Invoke(CurrentExcitement);
        }

        public void Update(float deltaTime)
        {
            if (Data == null) return;
            CurrentExcitement = Mathf.MoveTowards(CurrentExcitement, Data.BaseExcitement, deltaTime * Data.DecayRatePerSecond);
            CurrentVolume = CurrentExcitement * Data.MaxVolume;
        }
    }
}
