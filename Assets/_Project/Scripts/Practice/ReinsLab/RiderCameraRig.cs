using UnityEngine;
using BarrelRivals.Core;
using BarrelRivals.Core.Reins;

namespace BarrelRivals.Practice
{
    /// <summary>One first-person camera owner from Ready through finish. Never changes horse movement.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Camera)), DefaultExecutionOrder(300)]
    public sealed class RiderCameraRig : MonoBehaviour
    {
        public const string ReducedMotionPreference = "BarrelRivals.Reins.ReducedMotion.v1";
        [SerializeField] private ReinsHorsePresentation presentation;
        [SerializeField, Range(0, 25)] private float downwardPitch = 5;
        [SerializeField, Range(55, 80)] private float fieldOfView = 65;
        [SerializeField, Range(0, 6)] private float speedFieldOfView = 4;
        [SerializeField] private LayerMask obstructionMask = ~0;
        [SerializeField, Range(0, 60)] private float maximumCourseLook;
        [SerializeField] private bool preserveLandscapeFraming;
        private ReinsLabController course;
        private float courseLookYaw, courseLookPitch, renderedBaseFov;
        public float CourseLookYaw => courseLookYaw;
        public float CourseLookPitch => courseLookPitch;
        public float DesiredCourseLookYaw { get; private set; }
        private readonly RaycastHit[] hits = new RaycastHit[16];
        private Camera view;
        private Transform ignoredRoot;
        private bool snap = true, preferenceLoaded;
        private float motionAmount;
        public bool ReducedMotion { get; private set; }

        public void Configure(ReinsHorsePresentation source, Transform horseRoot, ReinsLabController courseSource = null)
        {
            presentation = source; course = courseSource; ignoredRoot = horseRoot; view = GetComponent<Camera>();
            view.nearClipPlane = .08f;
            if (!preferenceLoaded)
            { ReducedMotion = PlayerPrefs.GetInt(ReducedMotionPreference, 0) != 0; preferenceLoaded = true; }
            ResetView();
        }

        public void SetReducedMotion(bool reduced)
        {
            ReducedMotion = reduced;
            PlayerPrefs.SetInt(ReducedMotionPreference, reduced ? 1 : 0); PlayerPrefs.Save();
            ResetMotion();
        }

        public void ResetView() { ResetMotion(); courseLookYaw = courseLookPitch = DesiredCourseLookYaw = 0; }
        private void ResetMotion() { snap = true; motionAmount = 0; }
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
            // Follow the changed body posture. Reduced motion keeps its mean height,
            // while normal view uses the rendered torso's small stride displacement.
            offset += presentation.StableTorsoMotion;
            offset += (presentation.TorsoMotion-presentation.StableTorsoMotion)*motionAmount;
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
            // Horse yaw stays direct. A bounded rider glance changes only the view;
            // it never steers the horse or redirects the held rein inputs.
            UpdateCourseLook(target, dt);
            Quaternion rotation = presentation.RenderRotation * Quaternion.Euler(0, courseLookYaw, 0) * Quaternion.Euler(downwardPitch + courseLookPitch, 0, 0);
            transform.SetPositionAndRotation(target, rotation);
            float fov = fieldOfView + (ReducedMotion ? 0 : speedFieldOfView * speed / 14);
            renderedBaseFov = snap || ReducedMotion ? fov : Mathf.Lerp(renderedBaseFov, fov, 1 - Mathf.Exp(-5 * dt));
            // Keep the same horizontal composition on 4:3; resizing must not add a
            // transient zoom or remove the barrel from the side of the rider's view.
            float framing = preserveLandscapeFraming ? Mathf.Max(1, (16f / 9) / Mathf.Max(.5f, view.aspect)) : 1;
            view.fieldOfView = 2 * Mathf.Atan(Mathf.Tan(renderedBaseFov * Mathf.Deg2Rad * .5f) * framing) * Mathf.Rad2Deg;
            snap = false;
        }

        private void UpdateCourseLook(Vector3 eye, float dt)
        {
            DesiredCourseLookYaw = 0;
            float pitch = 0;
            if (maximumCourseLook <= 0) { courseLookYaw = courseLookPitch = 0; return; }
            var run = course ? course.Run : null;
            if (run != null && run.Phase == ReinsPhase.Racing && run.BarrelIndex < 3)
            {
                var barrel = StandardCourse.Barrel(run.BarrelIndex);
                var direction = Quaternion.Inverse(presentation.RenderRotation) *
                    (new Vector3((float)barrel.X, eye.y, (float)barrel.Z) - eye);
                float bearing = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                float nearby = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(14, 5, direction.magnitude));
                // Deliberately riding away must not twist the view backwards.
                float ahead = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(120, 160, Mathf.Abs(bearing)));
                DesiredCourseLookYaw = Mathf.Clamp(bearing, -maximumCourseLook, maximumCourseLook) * nearby * ahead;
                // Reserve lower-screen space for the real rein HUD. Only a close
                // drum needs downward gaze; open portions retain the horizon.
                float depth = Mathf.Max(.1f, direction.magnitude * Mathf.Cos((bearing - courseLookYaw) * Mathf.Deg2Rad)
                    - (float)ReinsCourseJudge.BarrelRadius);
                float floorAngle = Mathf.Atan2(Mathf.Max(0, eye.y), depth) * Mathf.Rad2Deg;
                pitch = Mathf.Clamp(floorAngle - downwardPitch - 13, 0, 18) * nearby * ahead;
            }
            float eased = Mathf.Lerp(courseLookYaw, DesiredCourseLookYaw, 1 - Mathf.Exp(-8 * dt));
            courseLookYaw = Mathf.MoveTowards(courseLookYaw, eased, 110 * dt);
            // A side glance also lowers the gaze slightly toward the footing. This
            // keeps the near drum above the touch controls, without zoom/roll.
            courseLookPitch = Mathf.MoveTowards(courseLookPitch, Mathf.Lerp(courseLookPitch, pitch, 1 - Mathf.Exp(-8 * dt)), 40 * dt);
        }
    }
}
