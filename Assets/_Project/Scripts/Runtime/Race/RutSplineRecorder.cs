using System;
using System.Collections.Generic;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public sealed class RutSplineRecorder
    {
        [Serializable]
        public struct Breadcrumb
        {
            public Vector3 Position;
            public Vector3 Forward;
            public float Speed;
            public float Timestamp;
        }

        private readonly List<Breadcrumb> _points = new List<Breadcrumb>(1024);
        private readonly float _minSampleDistanceSqr;
        private Vector3 _lastSamplePosition = Vector3.negativeInfinity;

        public IReadOnlyList<Breadcrumb> Points => _points;

        public RutSplineRecorder(float minSampleDistance = 0.5f)
        {
            _minSampleDistanceSqr = Mathf.Max(0.01f, minSampleDistance * minSampleDistance);
        }

        public void Reset()
        {
            _points.Clear();
            _lastSamplePosition = Vector3.negativeInfinity;
        }

        public bool RecordPoint(Vector3 position, Vector3 forward, float speed, float timestamp)
        {
            if (float.IsInfinity(_lastSamplePosition.x) || (position - _lastSamplePosition).sqrMagnitude >= _minSampleDistanceSqr)
            {
                _points.Add(new Breadcrumb { Position = position, Forward = forward, Speed = speed, Timestamp = timestamp });
                _lastSamplePosition = position;
                return true;
            }
            return false;
        }
    }
}
