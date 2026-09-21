using UnityEngine;

namespace BarrelRivals.Practice
{
    /// <summary>An accepted simulation sample. Presentation never writes it back into Core.</summary>
    public readonly struct HorsePresentationFrame
    {
        public readonly int Tick;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly float Speed, Turn, LeftRein, RightRein;
        public readonly bool Wrap, Drive;

        public HorsePresentationFrame(int tick, Vector3 position, Quaternion rotation, float speed,
            float turn, float leftRein, float rightRein, bool wrap, bool drive)
        {
            Tick = tick; Position = position; Rotation = rotation; Speed = speed; Turn = turn;
            LeftRein = leftRein; RightRein = rightRein; Wrap = wrap; Drive = drive;
        }
    }

    /// <summary>Interpolates only collider-free character art. The horse root keeps its exact Core pose.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(90)]
    public sealed class ReinsHorsePresentation : MonoBehaviour
    {
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Transform riderSeat;
        [SerializeField] private Transform interpolationRoot;
        [SerializeField] private Animator characterAnimator;
        [SerializeField] private Transform torsoBone;
        [SerializeField] private Vector3 torsoBindLocal;
        [SerializeField] private Vector3 fallbackSeat = new Vector3(0, 2.45f, -.55f);
        [SerializeField] private HorseRigBindings explicitRig;
        [SerializeField] private Vector3 neutralEyeInModel;
        private HorsePresentationFrame previous, current;
        private bool initialized, hasSample, resetAnimator;
        private float alpha = 1;
        private Vector3 seatOffset;
        private int parameterMask;
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int TurnId = Animator.StringToHash("Turn");
        private static readonly int LeftId = Animator.StringToHash("LeftRein");
        private static readonly int RightId = Animator.StringToHash("RightRein");
        private static readonly int WrapId = Animator.StringToHash("Wrap");
        private static readonly int DriveId = Animator.StringToHash("Drive");
        public Vector3 RenderPosition { get; private set; }
        public Quaternion RenderRotation { get; private set; } = Quaternion.identity;
        public Vector3 SeatOffset => seatOffset;
        /// <summary>Actual skeletal compression in the rider's local axes; never moves the Core root.</summary>
        public Vector3 TorsoMotion => explicitRig ? Quaternion.Inverse(RenderRotation) *
            (explicitRig.FollowSupportPoint(neutralEyeInModel)-explicitRig.ModelSpace.TransformPoint(neutralEyeInModel)) : torsoBone && torsoBone.parent
            ? Quaternion.Inverse(RenderRotation) * torsoBone.parent.TransformVector(torsoBone.localPosition-torsoBindLocal)
            : Vector3.zero;
        /// <summary>Mean pose of the authored Idle/Walk/Gallop blend, excluding repeated stride bob.</summary>
        public Vector3 StableTorsoMotion => explicitRig || !torsoBone ? Vector3.zero : Vector3.up *
            (Speed<=1.5f ? Mathf.Lerp(0,-.110f,Mathf.Clamp01(Speed/1.5f))
                : Mathf.Lerp(-.110f,-.120f,Mathf.InverseLerp(1.5f,8,Speed)));
        public float Speed => current.Speed;
        public float Turn => current.Turn;
        public float LeftRein => current.LeftRein;
        public float RightRein => current.RightRein;
        public bool HasSample => hasSample;
        public int Tick => current.Tick;
        /// <summary>The cycle actually being rendered by the rig, shared by camera, tack and secondary art.</summary>
        public float GaitPhaseRadians
        {
            get
            {
                if(!characterAnimator || !characterAnimator.isActiveAndEnabled || !characterAnimator.isInitialized
                    || !characterAnimator.runtimeAnimatorController || characterAnimator.layerCount==0)return 0;
                float cycle=characterAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                return float.IsNaN(cycle) || float.IsInfinity(cycle) ? 0 : Mathf.Repeat(cycle,1)*Mathf.PI*2;
            }
        }
        public float GaitStrength => hasSample ? Mathf.Clamp01(Mathf.Lerp(previous.Speed,current.Speed,alpha)/1.5f) : 0;

        /// <summary>Model must be a child containing art/bones only, with no gameplay colliders.</summary>
        public void Configure(Transform model, Transform seat = null, Animator animator = null)
        {
            modelRoot = model; riderSeat = seat; characterAnimator = animator;
            explicitRig = null;
            torsoBone = null;
            if(model) foreach(var bone in model.GetComponentsInChildren<Transform>(true))
                if(bone.name=="Bone") {torsoBone=bone;torsoBindLocal=bone.localPosition;break;}
            initialized = false;
        }

        /// <summary>Explicit-rig integration keeps the same interpolation/Core boundary.
        /// Its gait driver owns Animator parameters; the camera follows measured support.</summary>
        public void ConfigureRig(Transform model, HorseRigBindings rig, Vector3 neutralEye)
        {
            Configure(model,null,rig.Animator);
            explicitRig=rig;neutralEyeInModel=neutralEye;
        }

        public void Initialize()
        {
            if (initialized) return;
            initialized = true;
            if (!modelRoot) modelRoot = transform.Find("Articulated horse");
            // Capture the authored seat in bind pose. Animated head/neck bones must not shake the camera.
            seatOffset = explicitRig ? transform.InverseTransformPoint(explicitRig.ModelSpace.TransformPoint(neutralEyeInModel))
                : riderSeat ? transform.InverseTransformPoint(riderSeat.position) : fallbackSeat;
            if (modelRoot && modelRoot != transform && modelRoot.IsChildOf(transform)
                && modelRoot.GetComponentsInChildren<Collider>(true).Length == 0 && !interpolationRoot)
            {
                interpolationRoot = new GameObject("Ride visual interpolation").transform;
                interpolationRoot.SetParent(transform, false);
                modelRoot.SetParent(interpolationRoot, true);
            }
            if (!characterAnimator && modelRoot) characterAnimator = modelRoot.GetComponentInChildren<Animator>(true);
            if (modelRoot)
                foreach (var animator in modelRoot.GetComponentsInChildren<Animator>(true))
                {
                    animator.applyRootMotion = false;
                    // First-person tack and sibling hair depend on these bones even
                    // when the imported body's renderer is culled. Advancing only
                    // normalized time would otherwise leave the actual rig frozen.
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }
            parameterMask = 0;
            if (!characterAnimator || !characterAnimator.runtimeAnimatorController) return;
            foreach (var parameter in characterAnimator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Float)
                {
                    if (parameter.nameHash == SpeedId) parameterMask |= 1;
                    if (parameter.nameHash == TurnId) parameterMask |= 2;
                    if (parameter.nameHash == LeftId) parameterMask |= 4;
                    if (parameter.nameHash == RightId) parameterMask |= 8;
                }
                if (parameter.type == AnimatorControllerParameterType.Bool)
                {
                    if (parameter.nameHash == WrapId) parameterMask |= 16;
                    if (parameter.nameHash == DriveId) parameterMask |= 32;
                }
            }
        }

        public void ResetFrame(HorsePresentationFrame frame)
        {
            Initialize(); previous = current = frame; hasSample = true; resetAnimator = true; alpha = 1;
            ApplyInterpolation(1);
        }

        public void PushFrame(HorsePresentationFrame frame)
        {
            if (!hasSample || frame.Tick < current.Tick || (frame.Position - current.Position).sqrMagnitude > 9)
            { ResetFrame(frame); return; }
            if (frame.Tick == current.Tick) return;
            previous = current; current = frame;
        }

        public void ApplyInterpolation(float fraction)
        {
            if (!hasSample) return;
            alpha = Mathf.Clamp01(fraction);
            RenderPosition = Vector3.Lerp(previous.Position, current.Position, alpha);
            RenderRotation = Quaternion.Slerp(previous.Rotation, current.Rotation, alpha);
            if (interpolationRoot) interpolationRoot.SetPositionAndRotation(RenderPosition, RenderRotation);
            if (!characterAnimator || !characterAnimator.runtimeAnimatorController || !characterAnimator.isActiveAndEnabled) return;
            if (resetAnimator && characterAnimator.isInitialized)
            {
                for (int layer = 0; layer < characterAnimator.layerCount; layer++) characterAnimator.Play(0, layer, 0);
                resetAnimator = false;
            }
            if(explicitRig)return;
            // Optional controller parameters are cached once, so unconfigured imported rigs remain valid.
            if ((parameterMask & 1) != 0) characterAnimator.SetFloat(SpeedId, current.Speed);
            // Above the full-gallop threshold, match its authored 8m/s stance travel.
            // Blended walk/gallop, turning and braking still need stride-warping review.
            // This is presentation only; no root motion, contact grading or time credit.
            if ((parameterMask & 1) != 0) characterAnimator.speed=Mathf.Clamp(current.Speed/8,1,1.75f);
            if ((parameterMask & 2) != 0) characterAnimator.SetFloat(TurnId, current.Turn);
            if ((parameterMask & 4) != 0) characterAnimator.SetFloat(LeftId, current.LeftRein);
            if ((parameterMask & 8) != 0) characterAnimator.SetFloat(RightId, current.RightRein);
            if ((parameterMask & 16) != 0) characterAnimator.SetBool(WrapId, current.Wrap);
            if ((parameterMask & 32) != 0) characterAnimator.SetBool(DriveId, current.Drive);
        }

        private void LateUpdate() => ApplyInterpolation(alpha);
    }
}
