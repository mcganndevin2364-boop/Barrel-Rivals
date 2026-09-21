using System;
using UnityEngine;

namespace BarrelRivals.Practice
{
    /// <summary>Development flat-floor blend correction, using real rigid hoof vertices.
    /// Rotation-only presentation; never translates the actor or alters Core contact.</summary>
    [DisallowMultipleComponent,DefaultExecutionOrder(220)]
    public sealed class HeroHorseGrounding : MonoBehaviour
    {
        [Serializable] public struct Leg
        {
            public Transform upper,lower,pastern,hoof;
            public Vector3[] rigidVerticesInHoof;
            public Vector3 poleInModel;
        }
        public Transform model;
        public Leg[] legs;
        public float MinimumBefore { get; private set; }
        public float MinimumAfter { get; private set; }
        public float MaximumCorrection { get; private set; }
        void LateUpdate()=>RenderImmediate();

        public float Lowest(Leg leg)
        {
            var matrix=model.worldToLocalMatrix*leg.hoof.localToWorldMatrix;
            float y=float.PositiveInfinity;
            foreach(var vertex in leg.rigidVerticesInHoof)y=Mathf.Min(y,matrix.MultiplyPoint3x4(vertex).y);
            return y;
        }
        public void RenderImmediate()
        {
            if(!model || legs==null)return;
            MinimumBefore=MinimumAfter=float.PositiveInfinity;MaximumCorrection=0;
            foreach(var leg in legs)
            {
                float low=Lowest(leg);MinimumBefore=Mathf.Min(MinimumBefore,low);
                float correction=Mathf.Max(0,.004f-low);MaximumCorrection=Mathf.Max(MaximumCorrection,correction);
                if(correction>0)Solve(leg,leg.pastern.position+model.TransformVector(new Vector3(0,correction,0)));
                MinimumAfter=Mathf.Min(MinimumAfter,Lowest(leg));
            }
        }
        void Solve(Leg leg,Vector3 target)
        {
            var ankleRotation=leg.pastern.rotation;var hoofRotation=leg.hoof.rotation;
            var start=leg.upper.position;var delta=target-start;
            float a=Vector3.Distance(start,leg.lower.position),b=Vector3.Distance(leg.lower.position,leg.pastern.position);
            if(a<.001f || b<.001f || delta.sqrMagnitude<1e-8f)return;
            float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.00001f,a+b-.00001f);
            var direction=delta.normalized;
            var pole=Vector3.ProjectOnPlane(model.TransformDirection(leg.poleInModel),direction).normalized;
            if(pole.sqrMagnitude<.5f)pole=Vector3.ProjectOnPlane(model.right,direction).normalized;
            float along=(a*a-b*b+distance*distance)/(2*distance);
            var joint=start+direction*along+pole*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            leg.upper.rotation=Quaternion.FromToRotation(leg.lower.position-start,joint-start)*leg.upper.rotation;
            leg.lower.rotation=Quaternion.FromToRotation(leg.pastern.position-leg.lower.position,start+direction*distance-leg.lower.position)*leg.lower.rotation;
            leg.pastern.rotation=ankleRotation;leg.hoof.rotation=hoofRotation;
        }
    }
}
