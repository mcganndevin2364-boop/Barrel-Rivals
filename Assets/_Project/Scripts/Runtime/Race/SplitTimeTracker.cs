using System;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public sealed class SplitTimeTracker
    {
        public float ElapsedTime { get; private set; }
        public bool IsRunning { get; private set; }
        public float[] BarrelSplitTimes { get; } = new float[3];
        public float FinalTime { get; private set; }

        public event Action<int, float> OnSplitRecorded;
        public event Action<float> OnTimerFinished;

        public void StartTimer()
        {
            ElapsedTime = 0f;
            IsRunning = true;
            for (int i = 0; i < 3; i++) BarrelSplitTimes[i] = 0f;
            FinalTime = 0f;
        }

        public void Update(float deltaTime)
        {
            if (IsRunning) ElapsedTime += deltaTime;
        }

        public void RecordSplit(int barrelIndex)
        {
            if (barrelIndex >= 0 && barrelIndex < 3 && BarrelSplitTimes[barrelIndex] <= 0f)
            {
                BarrelSplitTimes[barrelIndex] = ElapsedTime;
                OnSplitRecorded?.Invoke(barrelIndex, ElapsedTime);
            }
        }

        public void FinishTimer()
        {
            if (!IsRunning) return;
            IsRunning = false;
            FinalTime = ElapsedTime;
            OnTimerFinished?.Invoke(FinalTime);
        }
    }
}
