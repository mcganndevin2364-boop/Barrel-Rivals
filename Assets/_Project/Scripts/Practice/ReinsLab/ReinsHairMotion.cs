using System;
using UnityEngine;

namespace BarrelRivals.Practice
{
    /// <summary>
    /// Original cosmetic strand motion. Only independent added hair bones are driven;
    /// the imported deform bones, horse root and accepted simulation are never changed.
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(180)]
    public sealed class ReinsHairMotion : MonoBehaviour
    {
        [Serializable]
        public struct Binding
        {
            public Transform bone;
            // Axes are expressed in the helper's own neutral local coordinates.
            // Positive lift must point the strands away from the fitted body surface.
            public Vector3 liftAxis, swayAxis;
            public float idleDegrees, strideDegrees, turnDegrees, phaseRadians;
            [SerializeField, HideInInspector] internal Quaternion restLocalRotation;
        }

        [SerializeField] private ReinsHorsePresentation source;
        [SerializeField] private Animator animator;
        [SerializeField] private Binding[] bindings = Array.Empty<Binding>();
        private RiderCameraRig comfort;
        private bool preferenceLoaded, reducedMotion;

        public bool ReducedMotion => comfort ? comfort.ReducedMotion : reducedMotion;

        /// <summary>
        /// Authoring-time setup. Capture neutral helper rotations before any presentation
        /// is applied; these serialized poses survive cloning an already moving player.
        /// The component belongs beneath Reference horse so the stable retains it.
        /// </summary>
        public void Configure(ReinsHorsePresentation presentation, Animator characterAnimator, Binding[] boneBindings)
        {
            RestoreNeutral();
            source = presentation;
            animator = characterAnimator;
            bindings = boneBindings == null ? Array.Empty<Binding>() : (Binding[])boneBindings.Clone();
            for (int i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                if (!binding.bone) continue;
                binding.restLocalRotation = binding.bone.localRotation;
                binding.liftAxis = SafeAxis(binding.liftAxis);
                binding.swayAxis = SafeAxis(binding.swayAxis);
                binding.idleDegrees = Mathf.Clamp(FiniteOrZero(binding.idleDegrees), 0, 2);
                binding.strideDegrees = Mathf.Clamp(FiniteOrZero(binding.strideDegrees), 0, 8);
                binding.turnDegrees = Mathf.Clamp(FiniteOrZero(binding.turnDegrees), 0, 6);
                binding.phaseRadians = Mathf.Repeat(FiniteOrZero(binding.phaseRadians), Mathf.PI * 2);
                bindings[i] = binding;
            }
        }

        /// <summary>Follow the live camera comfort setting; no per-frame searches or preference reads.</summary>
        public void BindComfort(RiderCameraRig rig)
        {
            LoadPreference();
            comfort = rig;
            RenderImmediate();
        }

        /// <summary>Explicit local override. BindComfort can later restore following the camera.</summary>
        public void SetReducedMotion(bool reduced)
        {
            comfort = null;
            preferenceLoaded = true;
            reducedMotion = reduced;
            RenderImmediate();
        }

        private void Awake() => LoadPreference();
        private void LateUpdate() => RenderImmediate();
        private void OnDisable() => RestoreNeutral();

        /// <summary>
        /// Evaluate after Animator.Update in a paused inspection or replay capture.
        /// There is no wall-clock timer or integration state: the same accepted pose and
        /// rendered animation phase always produce the same helper rotations.
        /// </summary>
        public void RenderImmediate()
        {
            LoadPreference();
            if (ReducedMotion || !TryReadPhase(out float phase))
            {
                RestoreNeutral();
                return;
            }

            // StableBuilder removes the race presenter but retains the Animator.
            // Unity's destroyed-object check is required here, rather than ?? fallback.
            bool riding = source && source.HasSample;
            float effort = riding ? Mathf.Clamp01(FiniteOrZero(source.Speed) / 8) : 0;
            float turn = riding ? Mathf.Clamp(FiniteOrZero(source.Turn) / 1.5f, -1, 1) : 0;
            for (int i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                if (!binding.bone) continue;
                float cycle = phase - binding.phaseRadians;
                float liftPulse = .5f + .35f * Mathf.Sin(cycle) + .15f * Mathf.Sin(cycle * 2 - .4f);
                float idle = binding.idleDegrees * (.5f + .5f * Mathf.Sin(cycle));
                float lift = Mathf.Clamp(idle + binding.strideDegrees * effort * liftPulse, 0, 8);
                float sway = Mathf.Clamp(binding.idleDegrees * .3f * Mathf.Sin(cycle - .6f)
                    + binding.strideDegrees * effort * .25f * Mathf.Sin(cycle - .7f)
                    - binding.turnDegrees * turn * effort, -6, 6);
                // Never accumulate offsets or change the helper's fitted position/scale.
                var liftRotation = binding.liftAxis.sqrMagnitude > 0
                    ? Quaternion.AngleAxis(lift, binding.liftAxis) : Quaternion.identity;
                var swayRotation = binding.swayAxis.sqrMagnitude > 0
                    ? Quaternion.AngleAxis(sway, binding.swayAxis) : Quaternion.identity;
                var offset = liftRotation * swayRotation;
                binding.bone.localRotation = binding.restLocalRotation
                    * Quaternion.RotateTowards(Quaternion.identity, offset, 10);
            }
        }

        private bool TryReadPhase(out float phase)
        {
            phase = 0;
            if (!animator || !animator.runtimeAnimatorController || !animator.isInitialized || animator.layerCount == 0)
                return false;
            // A capture may explicitly advance a disabled Animator. Its valid state
            // clock is still usable; do not gate this fallback on animator.enabled.
            float cycle = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            if (float.IsNaN(cycle) || float.IsInfinity(cycle)) return false;
            phase = source && source.HasSample && animator.isActiveAndEnabled
                ? source.GaitPhaseRadians : Mathf.Repeat(cycle, 1) * Mathf.PI * 2;
            return true;
        }

        private void LoadPreference()
        {
            if (preferenceLoaded) return;
            reducedMotion = PlayerPrefs.GetInt(RiderCameraRig.ReducedMotionPreference, 0) != 0;
            preferenceLoaded = true;
        }

        private void RestoreNeutral()
        {
            for (int i = 0; i < bindings.Length; i++)
                if (bindings[i].bone) bindings[i].bone.localRotation = bindings[i].restLocalRotation;
        }

        private static Vector3 SafeAxis(Vector3 axis)
        {
            axis = new Vector3(FiniteOrZero(axis.x), FiniteOrZero(axis.y), FiniteOrZero(axis.z));
            float length = axis.magnitude;
            return length > .0001f && !float.IsInfinity(length) ? axis / length : Vector3.zero;
        }

        private static float FiniteOrZero(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0 : value;
    }
}
