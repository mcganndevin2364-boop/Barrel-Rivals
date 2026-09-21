using System;
using UnityEngine;

namespace BarrelRivals.Practice
{
    /// <summary>Seated cosmetic skeleton. Solves limbs against tack, never writes race state.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(240)]
    public sealed class ReinsRiderBodyPresentation : MonoBehaviour
    {
        [Serializable] public struct Limb
        {
            public Transform upper, lower, end, target;
            public Vector3 pole;
            public Quaternion endRotationOffset;
        }
        [SerializeField] private ReinsHorsePresentation source;
        [SerializeField] private Transform pelvis, seat, modelSpace;
        [SerializeField] private Transform[] bones;
        [SerializeField] private Quaternion[] restRotations;
        [SerializeField] private Vector3[] restPositions;
        [SerializeField] private Limb leftArm, rightArm, leftLeg, rightLeg;
        [SerializeField] private Vector3 pelvisInRoot;
        public float MaximumReachError { get; private set; }

        public void Configure(ReinsHorsePresentation presentation, Transform rootPelvis, Transform seatAnchor,
            Transform space, Transform[] rigBones, Limb la, Limb ra, Limb ll, Limb rl)
        {
            source=presentation;pelvis=rootPelvis;seat=seatAnchor;modelSpace=space;bones=rigBones;
            restRotations=new Quaternion[bones.Length];restPositions=new Vector3[bones.Length];
            for(int i=0;i<bones.Length;i++){restRotations[i]=bones[i].localRotation;restPositions[i]=bones[i].localPosition;}
            pelvisInRoot=transform.InverseTransformPoint(pelvis.position);
            leftArm=Prepare(la);rightArm=Prepare(ra);leftLeg=Prepare(ll);rightLeg=Prepare(rl);
        }

        private static Limb Prepare(Limb limb)
        {limb.endRotationOffset=Quaternion.Inverse(limb.target.rotation)*limb.end.rotation;return limb;}

        private void LateUpdate()=>RenderImmediate();
        public void RenderImmediate()
        {
            if(!source || !pelvis || !seat || !modelSpace || bones==null)return;
            for(int i=0;i<bones.Length;i++)
            {
                bones[i].localPosition=restPositions[i];bones[i].localRotation=restRotations[i];
            }
            // Tack inherits the horse's actual animated torso. Match that seat before
            // bending the rider; world-space targets keep feet and cuffs connected.
            transform.position=seat.position-transform.TransformVector(pelvisInRoot);
            float lean=Mathf.Lerp(8,19,Mathf.Clamp01(source.Speed/14));
            pelvis.rotation=Quaternion.AngleAxis(lean,modelSpace.right)*pelvis.rotation;
            MaximumReachError=0;
            Solve(leftLeg);Solve(rightLeg);Solve(leftArm);Solve(rightArm);
        }

        private void Solve(Limb limb)
        {
            if(!limb.upper || !limb.lower || !limb.end || !limb.target)return;
            Vector3 root=limb.upper.position,goal=limb.target.position;
            float a=Vector3.Distance(root,limb.lower.position),b=Vector3.Distance(limb.lower.position,limb.end.position);
            var delta=goal-root;float rawDistance=delta.magnitude;
            if(a<.001f || b<.001f || rawDistance<.001f)return;
            var forward=delta/rawDistance;
            float distance=Mathf.Clamp(rawDistance,Mathf.Abs(a-b)+.0001f,a+b-.0001f);
            var pole=modelSpace.TransformPoint(limb.pole)-root;
            var across=Vector3.ProjectOnPlane(pole,forward).normalized;
            if(across.sqrMagnitude<.5f)across=Vector3.ProjectOnPlane(modelSpace.forward,forward).normalized;
            if(across.sqrMagnitude<.5f)across=Vector3.ProjectOnPlane(modelSpace.right,forward).normalized;
            float along=(a*a-b*b+distance*distance)/(2*distance);
            var bend=root+forward*along+across*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            limb.upper.rotation=Quaternion.FromToRotation(limb.lower.position-root,bend-root)*limb.upper.rotation;
            limb.lower.rotation=Quaternion.FromToRotation(limb.end.position-limb.lower.position,goal-limb.lower.position)*limb.lower.rotation;
            limb.end.rotation=limb.target.rotation*limb.endRotationOffset;
            MaximumReachError=Mathf.Max(MaximumReachError,Vector3.Distance(limb.end.position,goal));
        }
    }
}
