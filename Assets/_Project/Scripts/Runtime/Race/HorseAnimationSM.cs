using System;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public enum HorsePodiumPose { None = 0, RearingVictory = 1, HeadTossProud = 2, ExhaustedPanting = 3 }

    [DisallowMultipleComponent]
    public sealed class HorseAnimationSM : MonoBehaviour
    {
        private static readonly int HashSpeedRatio = Animator.StringToHash("SpeedRatio");
        private static readonly int HashSteerAngle = Animator.StringToHash("SteerAngle");
        private static readonly int HashIsDrifting = Animator.StringToHash("IsDrifting");

        [SerializeField] private Animator _horseAnimator;
        [SerializeField] private Transform _skeletalRootBone;
        [SerializeField, Range(5f, 40f)] private float _maxBankAngleDeg = 24.0f;

        private float _currentBank = 0f;
        private float _targetBank = 0f;
        private float _speedRatio = 0f;
        private float _steer = 0f;

        public void UpdateAnimationState(float speedRatio, float steer, bool isDrifting, DriftTier driftTier, bool inPocket)
        {
            _speedRatio = speedRatio; _steer = steer;
            _targetBank = -steer * _maxBankAngleDeg * Mathf.Clamp01(speedRatio);
        }

        private void Update()
        {
            if (_horseAnimator != null)
            {
                _horseAnimator.SetFloat(HashSpeedRatio, _speedRatio);
                _horseAnimator.SetFloat(HashSteerAngle, _steer);
                _horseAnimator.speed = Mathf.Lerp(1.0f, 1.85f, Mathf.Clamp01(_speedRatio));
            }
        }

        private void LateUpdate()
        {
            if (_skeletalRootBone != null)
            {
                _currentBank = Mathf.Lerp(_currentBank, _targetBank, Time.deltaTime * 8.5f);
                _skeletalRootBone.localRotation *= Quaternion.Euler(0f, 0f, _currentBank);
            }
        }
    }
}
