using System;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public enum ControlSchemeType { DualTouchZone, VirtualJoystick, TiltAccelerometer }

    public readonly struct RaceInputFrame
    {
        public readonly float Steering;
        public readonly float Throttle;
        public readonly bool IsDriftHeld;
        public readonly bool WhipTriggered;
        public readonly bool DriftReleaseTriggered;
        public readonly bool FastForwardTapped;

        public RaceInputFrame(float steering, float throttle, bool isDriftHeld, bool whipTriggered, bool driftReleaseTriggered, bool fastForwardTapped)
        {
            Steering = Mathf.Clamp(steering, -1.0f, 1.0f);
            Throttle = Mathf.Clamp01(throttle);
            IsDriftHeld = isDriftHeld;
            WhipTriggered = whipTriggered;
            DriftReleaseTriggered = driftReleaseTriggered;
            FastForwardTapped = fastForwardTapped;
        }

        public static RaceInputFrame Empty => new RaceInputFrame(0f, 0f, false, false, false, false);
    }

    [DisallowMultipleComponent]
    public sealed class InputManager : MonoBehaviour
    {
        private const float MinSwipeDistanceInches = 0.40f;
        private const float MaxSwipeDurationSec = 0.35f;
        private const float DoubleTapTimeWindowSec = 0.28f;
        private const float JoystickRadiusInches = 0.55f;

        [SerializeField] private ControlSchemeType _controlScheme = ControlSchemeType.DualTouchZone;
        [SerializeField, Range(0.5f, 2.5f)] private float _steeringSensitivity = 1.2f;
        [SerializeField, Range(0.0f, 0.25f)] private float _deadzone = 0.04f;
        [SerializeField] private bool _autoAccelerate = true;
        [SerializeField] private bool _enableKeyboardFallback = true;

        private bool _isInputLocked = false;
        private float _currentSteering = 0f;
        private float _currentThrottle = 0f;
        private bool _isDriftHeld = false;
        private bool _bufferedWhipTrigger = false;
        private bool _bufferedDriftReleaseTrigger = false;
        private bool _bufferedFastForwardTap = false;

        private int _leftFingerId = -1;
        private Vector2 _leftTouchStart;
        private int _rightFingerId = -1;
        private Vector2 _rightTouchStart;
        private float _rightTouchStartTime;
        private float _lastRightTapReleaseTime;

        private float _screenDpi;
        private float _joystickPixelRadius;
        private float _minSwipePixelDistance;

        public event Action OnWhipSwipeDetected;
        public event Action OnDriftReleaseDoubleTapped;
        public event Action OnScreenTapped;

        private void Awake()
        {
            _screenDpi = Screen.dpi > 0 ? Screen.dpi : 160f;
            _joystickPixelRadius = JoystickRadiusInches * _screenDpi;
            _minSwipePixelDistance = MinSwipeDistanceInches * _screenDpi;
        }

        public void SetInputLock(bool isLocked)
        {
            _isInputLocked = isLocked;
            if (isLocked)
            {
                _currentSteering = 0f; _currentThrottle = 0f; _isDriftHeld = false;
                _leftFingerId = -1; _rightFingerId = -1;
                _bufferedWhipTrigger = false; _bufferedDriftReleaseTrigger = false;
            }
        }

        private void Update()
        {
            if (_isInputLocked) return;
#if UNITY_EDITOR || UNITY_STANDALONE
            if (_enableKeyboardFallback) { PollKeyboard(); return; }
#endif
            PollTouches();
        }

        private void PollTouches()
        {
            float halfWidth = Screen.width * 0.5f;
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.position.x < halfWidth) ProcessLeftTouch(t);
                else ProcessRightTouch(t);
            }
            if (_autoAccelerate) _currentThrottle = 1.0f;
        }

        private void ProcessLeftTouch(Touch t)
        {
            if (t.phase == TouchPhase.Began) { _leftFingerId = t.fingerId; _leftTouchStart = t.position; }
            else if ((t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) && t.fingerId == _leftFingerId)
            {
                float dx = (t.position.x - _leftTouchStart.x) / _joystickPixelRadius;
                _currentSteering = Mathf.Abs(dx) < _deadzone ? 0f : Mathf.Clamp(dx * _steeringSensitivity, -1.0f, 1.0f);
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                if (t.fingerId == _leftFingerId) { _leftFingerId = -1; _currentSteering = 0f; }
            }
        }

        private void ProcessRightTouch(Touch t)
        {
            if (t.phase == TouchPhase.Began)
            {
                _rightFingerId = t.fingerId; _rightTouchStart = t.position; _rightTouchStartTime = Time.unscaledTime;
                _isDriftHeld = true; _bufferedFastForwardTap = true;
                OnScreenTapped?.Invoke();
                if (Time.unscaledTime - _lastRightTapReleaseTime <= DoubleTapTimeWindowSec)
                {
                    _bufferedDriftReleaseTrigger = true;
                    OnDriftReleaseDoubleTapped?.Invoke();
                }
            }
            else if (t.phase == TouchPhase.Moved && t.fingerId == _rightFingerId)
            {
                Vector2 swipe = t.position - _rightTouchStart;
                if (swipe.y > _minSwipePixelDistance && (Time.unscaledTime - _rightTouchStartTime) <= MaxSwipeDurationSec)
                {
                    _bufferedWhipTrigger = true; _rightFingerId = -1;
                    OnWhipSwipeDetected?.Invoke();
                }
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                if (t.fingerId == _rightFingerId) { _rightFingerId = -1; _isDriftHeld = false; _lastRightTapReleaseTime = Time.unscaledTime; }
            }
        }

        private void PollKeyboard()
        {
            _currentSteering = Mathf.Clamp(Input.GetAxisRaw("Horizontal") * _steeringSensitivity, -1.0f, 1.0f);
            _currentThrottle = _autoAccelerate ? 1.0f : Mathf.Clamp01(Input.GetAxisRaw("Vertical"));
            _isDriftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || Input.GetMouseButton(1);
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow)) { _bufferedWhipTrigger = true; OnWhipSwipeDetected?.Invoke(); }
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return)) { _bufferedDriftReleaseTrigger = true; OnDriftReleaseDoubleTapped?.Invoke(); }
            if (Input.GetMouseButtonDown(0)) { _bufferedFastForwardTap = true; OnScreenTapped?.Invoke(); }
        }

        public RaceInputFrame SampleAndConsumeInputFrame()
        {
            if (_isInputLocked) return RaceInputFrame.Empty;
            var frame = new RaceInputFrame(_currentSteering, _currentThrottle, _isDriftHeld, _bufferedWhipTrigger, _bufferedDriftReleaseTrigger, _bufferedFastForwardTap);
            _bufferedWhipTrigger = false; _bufferedDriftReleaseTrigger = false; _bufferedFastForwardTap = false;
            return frame;
        }
    }
}
