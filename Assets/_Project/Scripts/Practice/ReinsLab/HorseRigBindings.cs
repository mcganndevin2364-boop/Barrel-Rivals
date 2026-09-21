using System;
using UnityEngine;

namespace BarrelRivals.Practice
{
    /// <summary>Explicit presentation-only roles; never moves the race simulation root.</summary>
    public sealed class HorseRigBindings : MonoBehaviour
    {
        [SerializeField] Transform modelSpace, motionRoot, saddleSupport, head, tailBase, tailTip;
        [SerializeField] SkinnedMeshRenderer body, eyes, groom;
        [SerializeField] Animator animator;
        [SerializeField] Matrix4x4 neutralSupportInModel;
        public Transform ModelSpace => modelSpace;
        public Transform MotionRoot => motionRoot;
        public Transform SaddleSupport => saddleSupport;
        public Transform Head => head;
        public Transform TailBase => tailBase;
        public Transform TailTip => tailTip;
        public SkinnedMeshRenderer Body => body;
        public SkinnedMeshRenderer Eyes => eyes;
        public SkinnedMeshRenderer Groom => groom;
        public Animator Animator => animator;

        // Call only in the verified neutral fitting pose; the support matrix is the rest frame.
        public void Configure(Transform space, Transform root, Transform support, Transform headBone,
            Transform tail, Transform tip, SkinnedMeshRenderer bodyMesh, SkinnedMeshRenderer eyeMesh,
            SkinnedMeshRenderer groomMesh, Animator characterAnimator)
        {
            if (!space || !root || !support || !headBone || !tail || !tip || !bodyMesh || !eyeMesh || !groomMesh || !characterAnimator)
                throw new ArgumentException("All horse presentation roles must be bound explicitly.");
            foreach (var role in new Transform[]{root,support,headBone,tail,tip,bodyMesh.transform,eyeMesh.transform,groomMesh.transform,characterAnimator.transform})
                if (role != space && !role.IsChildOf(space)) throw new ArgumentException("Horse role is outside its model space.");
            modelSpace=space; motionRoot=root; saddleSupport=support; head=headBone; tailBase=tail; tailTip=tip;
            body=bodyMesh; eyes=eyeMesh; groom=groomMesh; animator=characterAnimator;
            neutralSupportInModel=space.worldToLocalMatrix*support.localToWorldMatrix;
            animator.applyRootMotion=false; animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        }

        // Includes inherited Root translation and all ancestor rotation, unlike a local-position delta.
        public Matrix4x4 SupportDelta => modelSpace.worldToLocalMatrix*saddleSupport.localToWorldMatrix*neutralSupportInModel.inverse;
        public Vector3 FollowSupportPoint(Vector3 neutralPoint) => modelSpace.TransformPoint(SupportDelta.MultiplyPoint3x4(neutralPoint));
    }
}
