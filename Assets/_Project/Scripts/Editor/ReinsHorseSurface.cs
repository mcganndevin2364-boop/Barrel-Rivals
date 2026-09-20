using System;
using System.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

namespace BarrelRivals.Editor
{
    /// <summary>Editor-only neutral skin sampling in Reference horse local metres, including interpolated rig weights.</summary>
    public sealed class ReinsHorseSurface
    {
        public readonly Transform[] Bones;
        public readonly Bounds Bounds;
        readonly Vector3[] vertices;
        readonly int[] triangles;
        readonly BoneWeight[] weights;
        public readonly struct Hit
        {
            public readonly Vector3 Point;
            public readonly BoneWeight Weight;
            public Hit(Vector3 point,BoneWeight weight){Point=point;Weight=weight;}
        }
        public ReinsHorseSurface(Transform model)
        {
            var body=model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name.StartsWith("HorseBody",StringComparison.Ordinal));
            Bones=body.bones;weights=body.sharedMesh.boneWeights;
            var baked=new Mesh();
            try
            {
                // Unity's FBX conversion hierarchy has large scale. Bake with scale compensation,
                // apply the renderer matrix once, then remove the scene/model origin and rotation.
                body.BakeMesh(baked,true);vertices=baked.vertices;triangles=baked.triangles;
                var matrix=model.worldToLocalMatrix*body.transform.localToWorldMatrix;
                for(int i=0;i<vertices.Length;i++)vertices[i]=matrix.MultiplyPoint3x4(vertices[i]);
            }
            finally{Object.DestroyImmediate(baked);}
            var bounds=new Bounds(vertices[0],Vector3.zero);
            foreach(var point in vertices)bounds.Encapsulate(point);Bounds=bounds;
            if(Mathf.Abs(bounds.size.y-2.12f)>.02f)throw new InvalidOperationException("Fit the horse in explicit neutral Idle and local metres: "+bounds);
        }
        public Hit Top(float x,float z)=>Ray(new Vector3(x,3,z),Vector3.down);
        public Hit Side(float y,float z)=>Ray(new Vector3(2,y,z),Vector3.left);
        public Hit Ray(Vector3 origin,Vector3 direction)
        {
            float nearest=float.PositiveInfinity,bestU=0,bestV=0;int face=-1;
            for(int i=0;i<triangles.Length;i+=3)
            {
                var a=vertices[triangles[i]];var e1=vertices[triangles[i+1]]-a;var e2=vertices[triangles[i+2]]-a;
                var p=Vector3.Cross(direction,e2);float det=Vector3.Dot(e1,p);if(Mathf.Abs(det)<.0000001f)continue;
                float inverse=1/det;var offset=origin-a;float u=Vector3.Dot(offset,p)*inverse;if(u<0 || u>1)continue;
                var q=Vector3.Cross(offset,e1);float v=Vector3.Dot(direction,q)*inverse;if(v<0 || u+v>1)continue;
                float t=Vector3.Dot(e2,q)*inverse;if(t<0 || t>=nearest)continue;
                nearest=t;bestU=u;bestV=v;face=i;
            }
            if(face<0)throw new InvalidOperationException("Horse fitting ray missed body at "+origin+" toward "+direction);
            var influence=new float[Bones.Length];
            Add(influence,weights[triangles[face]],1-bestU-bestV);Add(influence,weights[triangles[face+1]],bestU);Add(influence,weights[triangles[face+2]],bestV);
            int first=0,second=1;
            for(int i=0;i<influence.Length;i++)if(influence[i]>influence[first])first=i;
            if(second==first)second=0;
            for(int i=0;i<influence.Length;i++)if(i!=first && influence[i]>influence[second])second=i;
            float sum=influence[first]+influence[second];
            if(sum<=0)throw new InvalidOperationException("Horse skin has no valid influence at fitted point.");
            return new Hit(origin+direction*nearest,new BoneWeight{boneIndex0=first,weight0=influence[first]/sum,boneIndex1=second,weight1=influence[second]/sum});
        }
        static void Add(float[] output,BoneWeight weight,float amount)
        {output[weight.boneIndex0]+=weight.weight0*amount;output[weight.boneIndex1]+=weight.weight1*amount;output[weight.boneIndex2]+=weight.weight2*amount;output[weight.boneIndex3]+=weight.weight3*amount;}
    }
}
