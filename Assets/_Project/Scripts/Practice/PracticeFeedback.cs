using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BarrelRivals.Core;
using UnityEngine;

[assembly: InternalsVisibleTo("BarrelRivals.Editor.Tests")]

namespace BarrelRivals.Practice
{
    /// <summary>Optional presentation only. Observing a run never advances or changes its rules.</summary>
    [DisallowMultipleComponent]
    public sealed class PracticeFeedback : MonoBehaviour
    {
        internal const string SoundPreference = "BarrelRivals.Practice.Sound.v1";
        internal const string HapticsPreference = "BarrelRivals.Practice.Haptics.v1";
        private static readonly float[] HoofSpacing = { .82f, .9f, 1.18f, 2.5f };
        private readonly PracticeFeedbackTracker _tracker = new PracticeFeedbackTracker();
        private readonly AudioSource[] _voices = new AudioSource[5];
        private readonly AudioClip[] _clips = new AudioClip[10];
        private readonly PracticeHaptics _haptics = new PracticeHaptics();
        private bool _initialized, _sound = true, _vibration = true, _suspended;
        private bool _applicationPaused, _applicationUnfocused;
        private PracticeRun _run;
        private PracticePose _lastPose;
        private long _lastTime;
        private float _distance;
        private int _hoof, _skillVoice;
        private double _lastPulse = -1;

        public bool SoundEnabled
        {
            get { Initialize(); return _sound; }
            set
            {
                Initialize(); if (_sound == value) return;
                _sound = value; SavePreference(SoundPreference, value);
                if (!value) StopVoices();
            }
        }
        public bool HapticsEnabled
        {
            get { Initialize(); return _vibration; }
            set
            {
                Initialize(); if (_vibration == value) return;
                _vibration = value; SavePreference(HapticsPreference, value);
                _haptics.SetEnabled(value && !_suspended);
            }
        }

        private void Awake() => Initialize();

        private void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            _sound = ReadPreference(SoundPreference);
            _vibration = ReadPreference(HapticsPreference);
            // Fixed voices and clips: no per-frame arrays, generated clips or overlapping voice growth.
            for (int i = 0; i < _voices.Length; i++)
            {
                var voice = gameObject.AddComponent<AudioSource>();
                voice.playOnAwake = false; voice.spatialBlend = 0; voice.dopplerLevel = 0;
                voice.priority = i < 3 ? 180 : 80; _voices[i] = voice;
            }
            _clips[0] = MakeClip("Hoof on dirt A", SoundKind.Hoof, .13f, 95, 13);
            _clips[1] = MakeClip("Hoof on dirt B", SoundKind.Hoof, .13f, 112, 47);
            _clips[2] = MakeClip("Leather and tack", SoundKind.Tack, .18f, 1250, 31);
            _clips[3] = MakeClip("Dirt turn", SoundKind.Dirt, .28f, 180, 57);
            _clips[4] = MakeClip("Barrel contact", SoundKind.Knock, .28f, 190, 71);
            _clips[5] = MakeClip("Skill needs practice", SoundKind.Grade, .13f, 220, 0);
            _clips[6] = MakeClip("Skill good", SoundKind.Grade, .15f, 440, 0);
            _clips[7] = MakeClip("Skill great", SoundKind.Grade, .18f, 554, 0);
            _clips[8] = MakeClip("Skill perfect", SoundKind.Grade, .21f, 659, 0);
            _clips[9] = MakeClip("Skill window", SoundKind.Window, .12f, 740, 0);
            _haptics.Initialize(); _haptics.SetEnabled(_vibration);
        }

        public void ResetFeedback()
        {
            Initialize(); StopVoices(); _tracker.Reset(); _run = null;
            _distance = 0; _hoof = 0; _skillVoice = 0; _lastPulse = -1;
        }

        /// <summary>Call after presenting the current state, including after accepted input. Repeated calls are safe.</summary>
        public void Observe(PracticeRun run)
        {
            Initialize();
            if (run == null) { ResetFeedback(); return; }
            if (!ReferenceEquals(_run, run))
            {
                ResetFeedback(); _run = run;
                _lastTime = run.NowMs; _lastPose = PracticePath.Sample(run);
            }
            var cues = _tracker.Observe(run);
            if (_suspended || !isActiveAndEnabled) return;
            if (run.Phase == PracticePhase.Cancelled) { StopVoices(); return; }
            if ((cues & PracticeFeedbackCue.Gate) != 0) _haptics.Prepare();
            if ((cues & PracticeFeedbackCue.Launch) != 0) Grade(run.LaunchGrade, false);
            if ((cues & PracticeFeedbackCue.Memory) != 0) PlaySkill(_clips[9], .16f, .8f);
            if ((cues & PracticeFeedbackCue.Drawing) != 0) PlaySkill(_clips[9], .19f, 1f);
            if ((cues & PracticeFeedbackCue.Trace) != 0) Grade(run.DrawingGrade.Grade, true);
            if ((cues & PracticeFeedbackCue.Turn) != 0) Play(_voices[2], _clips[3], .22f, 1);
            if ((cues & PracticeFeedbackCue.ExitReady) != 0)
            {
                _haptics.Prepare(); PlaySkill(_clips[9], .17f, 1.15f);
            }
            if ((cues & PracticeFeedbackCue.Exit) != 0) Grade(run.ExitGrade, true);
            if ((cues & PracticeFeedbackCue.Knock) != 0)
            {
                PlaySkill(_clips[4], .48f, 1); Pulse(true);
            }
            if ((cues & PracticeFeedbackCue.Finish) != 0)
            {
                StopMovement();
                PlaySkill(_clips[run.KnockCount == 0 ? 8 : 6], .24f, .8f);
            }
            ObserveMovement(run);
        }

        private void ObserveMovement(PracticeRun run)
        {
            var pose = PracticePath.Sample(run);
            long elapsed = run.NowMs - _lastTime;
            if (elapsed <= 0) return;
            double x = pose.X - _lastPose.X, z = pose.Z - _lastPose.Z;
            _lastTime = run.NowMs; _lastPose = pose;
            if (run.Phase == PracticePhase.Ready || run.Phase == PracticePhase.Gate ||
                run.Phase == PracticePhase.Complete || run.Phase == PracticePhase.Cancelled)
            { _distance = 0; return; }
            // Distance-driven cadence naturally slows in the drawing phase. Drop stale contacts after a stall.
            if (elapsed > 250) { _distance = 0; return; }
            _distance += (float)Math.Sqrt(x * x + z * z);
            if (_distance < HoofSpacing[_hoof & 3]) return;
            _distance = Math.Min(.25f, _distance - HoofSpacing[_hoof & 3]);
            Play(_voices[_hoof & 1], _clips[_hoof & 1], (_hoof & 3) == 2 ? .34f : .26f, 1);
            if ((_hoof & 3) == 0 && run.Phase != PracticePhase.Turn)
                Play(_voices[2], _clips[2], .1f, 1);
            _hoof++;
        }

        private void Grade(SkillGrade grade, bool haptic)
        {
            PlaySkill(_clips[5 + (int)grade], grade == SkillGrade.Bad ? .2f : .3f, 1);
            if (haptic && grade != SkillGrade.Bad) Pulse(false);
        }
        private void PlaySkill(AudioClip clip, float volume, float pitch)
        { Play(_voices[3 + (_skillVoice++ & 1)], clip, volume, pitch); }
        private void Play(AudioSource voice, AudioClip clip, float volume, float pitch)
        {
            if (!_sound || _suspended || !Application.isPlaying || !voice) return;
            voice.clip = clip; voice.volume = volume; voice.pitch = pitch; voice.Play();
        }
        private void Pulse(bool contact)
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (!_vibration || _suspended || !Application.isPlaying || now - _lastPulse < .15) return;
            _lastPulse = now; _haptics.Pulse(contact);
        }
        private void StopMovement() { for (int i = 0; i < 3; i++) if (_voices[i]) _voices[i].Stop(); }
        private void StopVoices() { foreach (var voice in _voices) if (voice) voice.Stop(); }
        private void OnApplicationPause(bool paused)
        { _applicationPaused = paused; Suspend(_applicationPaused || _applicationUnfocused); }
        private void OnApplicationFocus(bool focused)
        { _applicationUnfocused = !focused; Suspend(_applicationPaused || _applicationUnfocused); }
        private void Suspend(bool suspended)
        {
            _suspended = suspended;
            if (suspended) StopVoices();
            if (_initialized) _haptics.SetEnabled(_vibration && !suspended);
        }
        private void OnDisable() { StopVoices(); if (_initialized) _haptics.SetEnabled(false); }
        private void OnEnable() { if (_initialized) _haptics.SetEnabled(_vibration && !_suspended); }
        private void OnDestroy()
        {
            StopVoices(); _haptics.Dispose();
            foreach (var clip in _clips)
                if (clip) { if (Application.isPlaying) Destroy(clip); else DestroyImmediate(clip); }
            foreach (var voice in _voices)
                if (voice) { if (Application.isPlaying) Destroy(voice); else DestroyImmediate(voice); }
        }
        private static bool ReadPreference(string key)
        {
            try { return PlayerPrefs.GetInt(key, 1) != 0; }
            catch (PlayerPrefsException) { return true; }
        }
        private static void SavePreference(string key, bool value)
        {
            try { PlayerPrefs.SetInt(key, value ? 1 : 0); PlayerPrefs.Save(); }
            catch (PlayerPrefsException) { Debug.LogWarning("Practice feedback preference could not be saved; the current setting still applies."); }
        }

        private enum SoundKind { Hoof, Tack, Dirt, Knock, Grade, Window }
        private static AudioClip MakeClip(string name, SoundKind kind, float duration, float frequency, uint seed)
        {
            const int rate = 22050;
            int count = (int)(rate * duration); var samples = new float[count];
            float lowNoise = 0;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate, progress = i / (float)(count - 1);
                seed = seed * 1664525u + 1013904223u;
                float noise = ((seed >> 8) / 8388607.5f) - 1;
                lowNoise += .12f * (noise - lowNoise);
                float envelope = Mathf.Min(t / .004f, 1) * Mathf.Pow(1 - progress, 3);
                float sine = Mathf.Sin(2 * Mathf.PI * frequency * t);
                float value;
                switch (kind)
                {
                    case SoundKind.Hoof:
                        value = sine * .7f + lowNoise * .8f + noise * Mathf.Exp(-t * 120) * .2f; break;
                    case SoundKind.Tack:
                        value = (noise - lowNoise) * .22f + sine * Mathf.Exp(-t * 45) * .16f; break;
                    case SoundKind.Dirt:
                        value = lowNoise * 2 + noise * .1f; break;
                    case SoundKind.Knock:
                        value = sine * .5f + Mathf.Sin(2 * Mathf.PI * frequency * 2.7f * t) * .24f + lowNoise * .5f; break;
                    case SoundKind.Grade:
                        value = sine * .48f + Mathf.Sin(2 * Mathf.PI * frequency * 1.5f * t) * .18f; break;
                    default:
                        value = sine * .4f; break;
                }
                samples[i] = Mathf.Clamp(value * envelope, -.85f, .85f);
            }
            var clip = AudioClip.Create(name, count, 1, rate, false);
            clip.SetData(samples, 0); return clip;
        }
    }

    [Flags]
    internal enum PracticeFeedbackCue
    {
        None = 0, Gate = 1, Launch = 2, Memory = 4, Drawing = 8, Trace = 16,
        Turn = 32, ExitReady = 64, Exit = 128, Knock = 256, Finish = 512
    }

    /// <summary>Deduplicates accepted presentation events; no clocks, random streams or rule mutations.</summary>
    internal sealed class PracticeFeedbackTracker
    {
        private PracticeRun _run;
        private PracticePhase _phase;
        private bool _trace, _exit;
        private int _knocks;
        public void Reset() { _run = null; }
        public PracticeFeedbackCue Observe(PracticeRun run)
        {
            if (run == null) { Reset(); return PracticeFeedbackCue.None; }
            if (!ReferenceEquals(_run, run))
            {
                _run = run; _phase = run.Phase; _trace = run.TraceSubmitted;
                _exit = run.ExitAccepted; _knocks = run.KnockCount;
                return PracticeFeedbackCue.None; // Late attachment never replays historical feedback.
            }
            var cues = PracticeFeedbackCue.None;
            if (_phase != run.Phase)
            {
                switch (run.Phase)
                {
                    case PracticePhase.Gate: cues |= PracticeFeedbackCue.Gate; break;
                    case PracticePhase.Alley: cues |= PracticeFeedbackCue.Launch; break;
                    case PracticePhase.Preview: cues |= PracticeFeedbackCue.Memory; break;
                    case PracticePhase.Drawing: cues |= PracticeFeedbackCue.Drawing; break;
                    case PracticePhase.Turn: cues |= PracticeFeedbackCue.Turn; break;
                    case PracticePhase.Exit: cues |= PracticeFeedbackCue.ExitReady; break;
                    case PracticePhase.Complete: cues |= PracticeFeedbackCue.Finish; break;
                }
            }
            if (!_trace && run.TraceSubmitted) cues |= PracticeFeedbackCue.Trace;
            if (!_exit && run.ExitAccepted) cues |= PracticeFeedbackCue.Exit;
            if (_knocks < run.KnockCount) cues |= PracticeFeedbackCue.Knock;
            _phase = run.Phase; _trace = run.TraceSubmitted; _exit = run.ExitAccepted; _knocks = run.KnockCount;
            return run.Phase == PracticePhase.Cancelled ? PracticeFeedbackCue.None : cues;
        }
    }

    internal sealed class PracticeHaptics : IDisposable
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void BRFeedbackInitialize();
        [DllImport("__Internal")] private static extern void BRFeedbackEnabled(int enabled);
        [DllImport("__Internal")] private static extern void BRFeedbackPrepare();
        [DllImport("__Internal")] private static extern void BRFeedbackPulse(int contact);
        [DllImport("__Internal")] private static extern void BRFeedbackDispose();
#elif UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaClass _bridge;
        private IntPtr _pulse, _enable;
        private readonly jvalue[] _argument = new jvalue[1];
#endif
        private bool _available = false;
        public void Initialize()
        {
#if UNITY_IOS && !UNITY_EDITOR
            BRFeedbackInitialize(); _available = true;
#elif UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                _bridge = new AndroidJavaClass("com.barrelrivals.practice.PracticeHaptics");
                _bridge.CallStatic("initialize");
                _pulse = AndroidJNI.GetStaticMethodID(_bridge.GetRawClass(), "pulse", "(I)V");
                _enable = AndroidJNI.GetStaticMethodID(_bridge.GetRawClass(), "setEnabled", "(I)V");
                _available = true;
            }
            catch (AndroidJavaException) { _bridge?.Dispose(); _bridge = null; }
#endif
        }
        public void SetEnabled(bool enabled)
        {
            if (!_available) return;
#if UNITY_IOS && !UNITY_EDITOR
            BRFeedbackEnabled(enabled ? 1 : 0);
#elif UNITY_ANDROID && !UNITY_EDITOR
            _argument[0].i = enabled ? 1 : 0;
            AndroidJNI.CallStaticVoidMethod(_bridge.GetRawClass(), _enable, _argument);
#endif
        }
        public void Prepare()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (_available) BRFeedbackPrepare();
#endif
        }
        public void Pulse(bool contact)
        {
            if (!_available) return;
#if UNITY_IOS && !UNITY_EDITOR
            BRFeedbackPulse(contact ? 1 : 0);
#elif UNITY_ANDROID && !UNITY_EDITOR
            _argument[0].i = contact ? 1 : 0;
            AndroidJNI.CallStaticVoidMethod(_bridge.GetRawClass(), _pulse, _argument);
#endif
        }
        public void Dispose()
        {
            if (!_available) return;
            SetEnabled(false);
#if UNITY_IOS && !UNITY_EDITOR
            BRFeedbackDispose();
#elif UNITY_ANDROID && !UNITY_EDITOR
            _bridge.Dispose(); _bridge = null;
#endif
            _available = false;
        }
    }
}
