using System;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public enum CameraViewMode { DynamicChase, PocketCloseup, SprintHeroLow, SpectatorWide, PodiumCelebration }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class CameraDirector : MonoBehaviour
    {
        [SerializeField] private Transform _targetTransform;
        [SerializeField] private Vector3 _chaseOffset = new Vector3(0.0f, 2.3f, -4.6f);
        [SerializeField] private Vector3 _lookAtOffset = new Vector3(0.0f, 1.4f, 1.8f);
        [SerializeField, Range(2.0f, 25.0f)] private float _positionDamping = 12.0f;
        [SerializeField, Range(2.0f, 25.0f)] private float _rotationDamping = 14.0f;
        [SerializeField, Range(50f, 75f)] private float _baseFOV = 62.0f;
        [SerializeField, Range(75f, 95f)] private float _maxBoostFOV = 82.0f;

        private Camera _unityCamera;
        private CameraViewMode _currentMode = CameraViewMode.DynamicChase;
        private Vector3 _smoothTargetPosition;
        private Quaternion _smoothTargetRotation;
        private float _currentFOV;
        private float _currentSpeedRatio = 0f;
        private float _shakeTrauma = 0f;
        private float _perlinSeed;

        public void SetTarget(Transform target) => _targetTransform = target;
        public void SetViewMode(CameraViewMode mode) => _currentMode = mode;
        public void UpdateSpeedRatio(float ratio) => _currentSpeedRatio = Mathf.Max(0f, ratio);
        public void AddTrauma(float amount) => _shakeTrauma = Mathf.Clamp01(_shakeTrauma + amount);

        private void Awake()
        {
            _unityCamera = GetComponent<Camera>();
            _currentFOV = _baseFOV;
            _perlinSeed = UnityEngine.Random.value * 100f;
        }

        private void LateUpdate()
        {
            if (_targetTransform == null) return;
            float dt = Time.deltaTime;

            Vector3 idealPos = _targetTransform.position + _targetTransform.TransformDirection(_chaseOffset);
            Vector3 lookTarget = _targetTransform.position + _targetTransform.TransformDirection(_lookAtOffset);

            _smoothTargetPosition = Vector3.Lerp(transform.position, idealPos, dt * _positionDamping);
            Quaternion targetRot = Quaternion.LookRotation(lookTarget - _smoothTargetPosition, Vector3.up);
            _smoothTargetRotation = Quaternion.Slerp(transform.rotation, targetRot, dt * _rotationDamping);

            float targetFOV = Mathf.Lerp(_baseFOV, _maxBoostFOV, Mathf.Clamp01((_currentSpeedRatio - 0.75f) / 0.5f));
            _currentFOV = Mathf.Lerp(_currentFOV, targetFOV, dt * 5.0f);
            _unityCamera.fieldOfView = _currentFOV;

            Vector3 shakeOffset = Vector3.zero;
            if (_shakeTrauma > 0.001f)
            {
                float intensity = _shakeTrauma * _shakeTrauma;
                float t = Time.time * 25.0f;
                shakeOffset = new Vector3((Mathf.PerlinNoise(_perlinSeed, t) - 0.5f) * 2f, (Mathf.PerlinNoise(_perlinSeed + 5f, t) - 0.5f) * 2f, 0f) * (0.35f * intensity);
                _shakeTrauma = Mathf.Max(0f, _shakeTrauma - (1.6f * dt));
            }

            transform.position = _smoothTargetPosition + shakeOffset;
            transform.rotation = _smoothTargetRotation;
        }
    }
}
