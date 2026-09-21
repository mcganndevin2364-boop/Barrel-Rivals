using UnityEngine;
using UnityEngine.Rendering;

namespace BarrelRivals.Practice
{
    /// <summary>Explicit development-rig adapter. Owns private rein meshes, never race state.</summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(260)]
    public sealed class HeroHorseAttachments : MonoBehaviour
    {
        public HorseRigBindings horse;
        public ReinsRiderBodyPresentation rider;
        public Transform leftHand, rightHand, leftGrip, rightGrip, leftBit, rightBit;
        public MeshFilter leftRein, rightRein;
        public Renderer[] headSurfaces;
        public Vector3 leftRest, rightRest;
        [Range(0,1)] public float leftPull, rightPull;
        [Range(0,14)] public float reviewSpeed=1.5f;
        private ReinInstance left, right;

        public void PoseRig()
        {
            if(!horse || !rider)return;
            PoseHand(leftHand,leftRest,leftPull,-1);
            PoseHand(rightHand,rightRest,rightPull,1);
            rider.RenderAtSpeed(reviewSpeed);
        }

        private void PoseHand(Transform hand,Vector3 rest,float pull,int side)
        {
            hand.position=horse.FollowSupportPoint(rest+new Vector3(0,pull*.016f,-pull*.082f));
            hand.rotation=horse.ModelSpace.rotation*horse.SupportDelta.rotation*
                Quaternion.Euler(-pull*5,side*8,side*(9+pull*3));
        }

        public void SetRiderView(bool firstPerson)
        {
            foreach(var surface in headSurfaces)
                if(surface)surface.shadowCastingMode=firstPerson?ShadowCastingMode.ShadowsOnly:ShadowCastingMode.On;
        }

        public void RenderImmediate()
        {
            PoseRig();
            if(!horse || !leftRein || !rightRein || !leftGrip || !rightGrip || !leftBit || !rightBit)return;
            if(left==null)left=new ReinInstance(leftRein);
            if(right==null)right=new ReinInstance(rightRein);
            left.Update(leftGrip.position,leftBit.position,leftPull,-1);
            right.Update(rightGrip.position,rightBit.position,rightPull,1);
        }

        private void LateUpdate()=>RenderImmediate();
        private void OnDestroy(){left?.Dispose();right?.Dispose();}

        // Instance ownership applies in paused Editor captures too. Persistent mesh assets
        // and other characters' reins are never deformed or destroyed by this component.
        private sealed class ReinInstance
        {
            readonly MeshFilter filter;
            readonly Mesh source, mesh;
            readonly Vector3[] vertices, normals;
            readonly Vector4[] tangents;
            public ReinInstance(MeshFilter target)
            {
                filter=target;source=target.sharedMesh;
                mesh=Instantiate(source);mesh.name=target.name+" private deformation";mesh.MarkDynamic();
                vertices=new Vector3[mesh.vertexCount];normals=new Vector3[mesh.vertexCount];tangents=new Vector4[mesh.vertexCount];
                filter.sharedMesh=mesh;
            }
            public void Update(Vector3 start,Vector3 end,float pull,int side)
                =>ReinsRiderTackPresentation.UpdateReinGeometry(filter.transform,start,end,pull,side,mesh,vertices,normals,tangents);
            public void Dispose()
            {
                if(filter && filter.sharedMesh==mesh)filter.sharedMesh=source;
                if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);
            }
        }
    }
}
