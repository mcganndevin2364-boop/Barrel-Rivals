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
        [SerializeField] private Vector3 fallbackSeat = new Vector3(0, 2.45f, -.55f);
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
        public float Speed => current.Speed;
        public float Turn => current.Turn;
        public bool HasSample => hasSample;

        /// <summary>Model must be a child containing art/bones only, with no gameplay colliders.</summary>
        public void Configure(Transform model, Transform seat = null, Animator animator = null)
        {
            modelRoot = model; riderSeat = seat; characterAnimator = animator;
            initialized = false;
        }

        public void Initialize()
        {
            if (initialized) return;
            initialized = true;
            if (!modelRoot) modelRoot = transform.Find("Articulated horse");
            // Capture the authored seat in bind pose. Animated head/neck bones must not shake the camera.
            seatOffset = riderSeat ? transform.InverseTransformPoint(riderSeat.position) : fallbackSeat;
            if (modelRoot && modelRoot != transform && modelRoot.IsChildOf(transform)
                && modelRoot.GetComponentsInChildren<Collider>(true).Length == 0 && !interpolationRoot)
            {
                interpolationRoot = new GameObject("Ride visual interpolation").transform;
                interpolationRoot.SetParent(transform, false);
                modelRoot.SetParent(interpolationRoot, true);
            }
            if (!characterAnimator && modelRoot) characterAnimator = modelRoot.GetComponentInChildren<Animator>(true);
            if (modelRoot)
                foreach (var animator in modelRoot.GetComponentsInChildren<Animator>(true)) animator.applyRootMotion = false;
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
            // Optional controller parameters are cached once, so unconfigured imported rigs remain valid.
            if ((parameterMask & 1) != 0) characterAnimator.SetFloat(SpeedId, current.Speed);
            if ((parameterMask & 2) != 0) characterAnimator.SetFloat(TurnId, current.Turn);
            if ((parameterMask & 4) != 0) characterAnimator.SetFloat(LeftId, current.LeftRein);
            if ((parameterMask & 8) != 0) characterAnimator.SetFloat(RightId, current.RightRein);
            if ((parameterMask & 16) != 0) characterAnimator.SetBool(WrapId, current.Wrap);
            if ((parameterMask & 32) != 0) characterAnimator.SetBool(DriveId, current.Drive);
        }

        private void LateUpdate() => ApplyInterpolation(alpha);
    }
}
