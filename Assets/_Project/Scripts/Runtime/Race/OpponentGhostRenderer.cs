using System;
using System.Collections.Generic;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    [System.Serializable]
    public struct GhostPlaybackKeyframe
    {
        public float Timestamp;
        public Vector3 Position;
        public Quaternion Rotation;
        public float SpeedMps;
        public DriftTier Drift;

        public GhostPlaybackKeyframe(float timestamp, Vector3 pos, Quaternion rot, float speed, DriftTier drift)
        {
            Timestamp = timestamp; Position = pos; Rotation = rot; SpeedMps = speed; Drift = drift;
        }
    }

    [DisallowMultipleComponent]
    public sealed class OpponentGhostRenderer : MonoBehaviour
    {
        [SerializeField] private Renderer[] _ghostMeshRenderers;
        private readonly List<GhostPlaybackKeyframe> _keyframes = new List<GhostPlaybackKeyframe>(512);
        private bool _isPlaying = false;
        private float _clock = 0f;
        private int _index = 0;

        public void LoadGhostData(string name, IReadOnlyList<GhostPlaybackKeyframe> frames)
        {
            _keyframes.Clear();
            if (frames != null) _keyframes.AddRange(frames);
            _index = 0; _clock = 0f;
        }

        public void StartPlayback() { if (_keyframes.Count > 0) { _isPlaying = true; _clock = 0f; _index = 0; SetVisible(true); } }
        public void StopPlayback() { _isPlaying = false; SetVisible(false); }

        private void SetVisible(bool v) { if (_ghostMeshRenderers != null) for (int i = 0; i < _ghostMeshRenderers.Length; i++) if (_ghostMeshRenderers[i] != null) _ghostMeshRenderers[i].enabled = v; }

        private void LateUpdate()
        {
            if (!_isPlaying || _keyframes.Count < 2) return;
            _clock += Time.deltaTime;
            while (_index < _keyframes.Count - 2 && _keyframes[_index + 1].Timestamp < _clock) _index++;
            if (_index >= _keyframes.Count - 1) { StopPlayback(); return; }

            var a = _keyframes[_index]; var b = _keyframes[_index + 1];
            float t = Mathf.Clamp01((_clock - a.Timestamp) / Mathf.Max(0.0001f, b.Timestamp - a.Timestamp));
            transform.position = Vector3.Lerp(a.Position, b.Position, t);
            transform.rotation = Quaternion.Slerp(a.Rotation, b.Rotation, t);
        }
    }
}
