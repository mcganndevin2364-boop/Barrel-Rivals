using System;
using UnityEngine;
using BarrelRacing.Data;

namespace BarrelRacing.Runtime.Race
{
    public sealed class MomentumSystem
    {
        public const float MIN_FUNCTIONAL_SPEED = 8.0f;
        public const float TARGET_GALLOP_MAX_SPEED = 18.2f;

        public float CurrentSpeed { get; private set; }
        public float MaxSpeedLimit { get; private set; }
        public float AccelerationRate { get; private set; }
        public float HandlingStiffness { get; private set; }
        public Vector3 Velocity { get; private set; }

        public void Initialize(float baseSpeed, float baseAgility, float baseAcceleration)
        {
            MaxSpeedLimit = Mathf.Max(MIN_FUNCTIONAL_SPEED, baseSpeed);
            CurrentSpeed = MIN_FUNCTIONAL_SPEED;
            AccelerationRate = Mathf.Max(1.0f, baseAcceleration);
            HandlingStiffness = Mathf.Clamp(baseAgility, 1.0f, 10.0f);
            Velocity = Vector3.forward * CurrentSpeed;
        }

        public void StepMovement(float throttle, float steer, float turboMultiplier, float boostBonus, float deltaTime, Transform horseTransform)
        {
            float targetSpeed = Mathf.Min(MaxSpeedLimit * turboMultiplier + boostBonus, 25.0f);
            if (throttle > 0.05f)
            {
                CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, targetSpeed, AccelerationRate * deltaTime);
            }
            else
            {
                CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, MIN_FUNCTIONAL_SPEED, AccelerationRate * 0.5f * deltaTime);
            }

            CurrentSpeed = Mathf.Max(MIN_FUNCTIONAL_SPEED, CurrentSpeed);

            float turnRate = steer * (HandlingStiffness * 45f) * deltaTime;
            horseTransform.Rotate(0f, turnRate, 0f, Space.World);

            Velocity = horseTransform.forward * CurrentSpeed;
            horseTransform.position += Velocity * deltaTime;
        }
    }
}
