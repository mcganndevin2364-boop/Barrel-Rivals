using UnityEngine;

namespace BarrelRivals.Practice
{
    /// <summary>One first-person camera owner from Ready through finish. Never changes horse movement.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Camera)), DefaultExecutionOrder(300)]
    public sealed class RiderCameraRig : MonoBehaviour
    {
        public const string ReducedMotionPreference = "BarrelRivals.Reins.ReducedMotion.v1";
        [SerializeField] private ReinsHorsePresentation presentation;
        [SerializeField, Range(0, 12)] private float downwardPitch = 5;
        [SerializeField, Range(55, 80)] private float fieldOfView = 65;
        [SerializeField, Range(0, 6)] private float speedFieldOfView = 4;
        [SerializeField] private LayerMask obstructionMask = ~0;
        private readonly RaycastHit[] hits = new RaycastHit[16];
        private Camera view;
        private Transform ignoredRoot;
        private bool snap = true, preferenceLoaded;
        private float motionAmount;
        public bool ReducedMotion { get; private set; }

        public void Configure(ReinsHorsePresentation source, Transform horseRoot)
        {
            presentation = source; ignoredRoot = horseRoot; view = GetComponent<Camera>();
            view.nearClipPlane = .08f;
            if (!preferenceLoaded)
            { ReducedMotion = PlayerPrefs.GetInt(ReducedMotionPreference, 0) != 0; preferenceLoaded = true; }
            ResetView();
        }

        public void SetReducedMotion(bool reduced)
        {
            ReducedMotion = reduced;
            PlayerPrefs.SetInt(ReducedMotionPreference, reduced ? 1 : 0); PlayerPrefs.Save();
            ResetView();
        }

        public void ResetView() { snap = true; motionAmount = 0; }
        private void LateUpdate() => Render(Mathf.Min(Time.unscaledDeltaTime, .05f));

        /// <summary>For a paused inspection/capture after updating the accepted pose.</summary>
        public void RenderImmediate() => Render(0);

        /// <summary>Advance the normal presentation envelope by an explicit replay/capture step.</summary>
        public void RenderForCapture(float elapsedSeconds) => Render(Mathf.Clamp(elapsedSeconds,0,.05f));

        private void Render(float dt)
        {
            if (!presentation || !presentation.HasSample) return;
            if (!view) view = GetComponent<Camera>();
            float speed = Mathf.Clamp(presentation.Speed, 0, 14);
            float strength = ReducedMotion ? 0 : Mathf.Clamp01(speed / 5);
            motionAmount = snap || ReducedMotion ? strength : Mathf.Lerp(motionAmount, strength, 1 - Mathf.Exp(-10 * dt));
            Vector3 offset = presentation.SeatOffset;
            // Follow the visible horse's cycle, never a competing render-time oscillator.
            offset.y += Mathf.Sin(presentation.GaitPhaseRadians) * .012f * motionAmount;
            Vector3 target = presentation.RenderPosition + presentation.RenderRotation * offset;
            // Only the view is displaced by obstruction. Self colliders are never used as camera blockers.
            Vector3 origin = presentation.RenderPosition + Vector3.up * 1.7f;
            Vector3 ray = target - origin;
            float distance = ray.magnitude;
            if (distance > .01f)
            {
                int count = Physics.SphereCastNonAlloc(origin, .12f, ray / distance, hits, distance,
                    obstructionMask, QueryTriggerInteraction.Ignore);
                float allowed = distance;
                for (int i = 0; i < count; i++)
                    if (hits[i].collider && (!ignoredRoot || !hits[i].collider.transform.IsChildOf(ignoredRoot)))
                        allowed = Mathf.Min(allowed, Mathf.Max(.05f, hits[i].distance - .03f));
                target = origin + ray / distance * allowed;
            }
            // Interpolated yaw remains direct. There is no delayed steering, target lock, or automatic barrel aim.
            Quaternion rotation = presentation.RenderRotation * Quaternion.Euler(downwardPitch, 0, 0);
            transform.SetPositionAndRotation(target, rotation);
            float fov = fieldOfView + (ReducedMotion ? 0 : speedFieldOfView * speed / 14);
            view.fieldOfView = snap || ReducedMotion ? fov : Mathf.Lerp(view.fieldOfView, fov, 1 - Mathf.Exp(-5 * dt));
            snap = false;
        }
    }
}
