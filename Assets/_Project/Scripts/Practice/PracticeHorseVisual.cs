using UnityEngine;

namespace BarrelRivals.Practice
{
    /// <summary>Original articulated prototype art. Observes motion; never moves the race root or colliders.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class PracticeHorseVisual : MonoBehaviour
    {
        [SerializeField] private Transform body, head, tail;
        [SerializeField] private Transform[] upperLegs, lowerLegs, ears;
        // Serialized bind poses survive cloning a moving horse for the best-run replay.
        [SerializeField, HideInInspector] private Vector3 bodyRest;
        [SerializeField, HideInInspector] private Quaternion bodyRotationRest, headRest, tailRest;
        [SerializeField, HideInInspector] private Quaternion[] upperRest, lowerRest, earRest;
        [SerializeField, HideInInspector] private bool hasRestPose;
        private Vector3 previousPosition;
        private Quaternion previousRotation;
        private float stride, speed, lean;
        private bool initialized;
        public float Speed => speed;
        public float StridePhase => stride;

        public void Configure(Transform visualBody, Transform headJoint, Transform tailJoint,
            Transform[] hips, Transform[] knees, Transform[] earJoints)
        {
            body = visualBody; head = headJoint; tail = tailJoint;
            upperLegs = hips; lowerLegs = knees; ears = earJoints;
            CaptureRestPose();
            initialized = false;
        }

        private void OnEnable() { initialized = false; stride = speed = lean = 0; }
        private void LateUpdate()
        {
            if (!body || !head || !tail || upperLegs == null || lowerLegs == null || ears == null
                || upperLegs.Length != 4 || lowerLegs.Length != 4) return;
            if (!initialized)
            {
                if (!hasRestPose) CaptureRestPose();
                previousPosition = transform.position; previousRotation = transform.rotation; initialized = true;
            }
            float dt = Mathf.Min(Time.unscaledDeltaTime, .1f);
            if (dt <= .0001f) return;
            float distance = Vector3.Distance(transform.position, previousPosition);
            float turn = Mathf.DeltaAngle(previousRotation.eulerAngles.y, transform.eulerAngles.y) / dt;
            // Retry/replay seeks are discontinuities, not a huge stride or turn impulse.
            if (distance > 3 || Time.unscaledDeltaTime > .3f) { distance = 0; turn = 0; speed = 0; stride = 0; lean = 0; }
            float observedSpeed = distance / dt;
            speed = Mathf.Lerp(speed, observedSpeed, 1 - Mathf.Exp(-12 * dt));
            float moving = Mathf.SmoothStep(0, 1, speed / 2.2f);
            stride = Mathf.Repeat(stride + distance * 2.4f, Mathf.PI * 2);
            float idle = Time.unscaledTime;
            lean = Mathf.Lerp(lean, Mathf.Clamp(-turn * speed * .026f, -11, 11), 1 - Mathf.Exp(-7 * dt));
            body.localPosition = bodyRest + Vector3.up * (Mathf.Sin(stride * 2) * .027f * moving + Mathf.Sin(idle * 1.8f) * .006f * (1 - moving));
            body.localRotation = bodyRotationRest * Quaternion.Euler(Mathf.Sin(stride) * 1.2f * moving, 0, lean);

            for (int i = 0; i < 4; i++)
            {
                // Offset front and hind pairs; only the recovery half folds the knee.
                float phase = stride + (i == 0 ? 0 : i == 1 ? Mathf.PI : i == 2 ? Mathf.PI * .72f : Mathf.PI * 1.72f);
                float swing = Mathf.Sin(phase);
                float lift = Mathf.Pow(Mathf.Max(0, Mathf.Cos(phase)), 2);
                float upper = swing * (i < 2 ? 26 : 23) * moving;
                float knee = lift * (i < 2 ? 42 : -36) * moving;
                upperLegs[i].localRotation = upperRest[i] * Quaternion.Euler(upper, 0, 0);
                lowerLegs[i].localRotation = lowerRest[i] * Quaternion.Euler(knee, 0, 0);
            }
            head.localRotation = headRest * Quaternion.Euler(Mathf.Sin(stride + .7f) * 2.2f * moving + Mathf.Sin(idle * .9f) * .6f, lean * -.3f, 0);
            tail.localRotation = tailRest * Quaternion.Euler(Mathf.Sin(stride) * 3 * moving, Mathf.Sin(idle * 2.4f + stride * .5f) * 8, -lean * .6f);
            for (int i = 0; i < ears.Length; i++)
                ears[i].localRotation = earRest[i] * Quaternion.Euler(Mathf.Sin(idle * 1.3f + i * 2.8f) * 4, Mathf.Sin(idle * .65f + i) * 9, 0);
            previousPosition = transform.position; previousRotation = transform.rotation;
        }
        private void CaptureRestPose()
        {
            bodyRest = body.localPosition; bodyRotationRest = body.localRotation;
            headRest = head.localRotation; tailRest = tail.localRotation;
            upperRest = Rotations(upperLegs); lowerRest = Rotations(lowerLegs); earRest = Rotations(ears);
            hasRestPose = true;
        }
        private static Quaternion[] Rotations(Transform[] joints)
        {
            var result = new Quaternion[joints.Length];
            for (int i = 0; i < joints.Length; i++) result[i] = joints[i].localRotation;
            return result;
        }
    }
}
