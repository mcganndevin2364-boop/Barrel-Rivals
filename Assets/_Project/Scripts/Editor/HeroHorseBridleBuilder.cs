using System;
using System.Linq;
using BarrelRivals.Practice;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace BarrelRivals.Editor
{
    /// <summary>Fits original western headstall construction to the explicit candidate skin and eyes.</summary>
    public static class HeroHorseBridleBuilder
    {
        public static void Build(HorseRigBindings bindings,out Transform leftBit,out Transform rightBit)
        {
            var model=bindings.ModelSpace;var head=bindings.Head;
            var leather=AssetDatabase.LoadAssetAtPath<Material>(ReinsPremiumArenaBuilder.Root+"/Materials/Oiled bridle leather.mat");
            var steel=AssetDatabase.LoadAssetAtPath<Material>(ReinsRiderTackBuilder.Root+"/Brushed bit steel.mat");
            var thread=AssetDatabase.LoadAssetAtPath<Material>(ReinsRiderTackBuilder.Root+"/Waxed saddle stitching.mat");
            if(!leather || !steel || !thread)throw new InvalidOperationException("Original headstall materials are missing.");
            var bridle=Child(model,"Western bridle");var hide=new ReinsHeadstallStrip();var hardware=new ReinsRiderTackBuilder.Surface();var seams=new ReinsRiderTackBuilder.Surface();
            var skin=new ReinsHorseSurface(bindings.Body,model);
            var poll=model.InverseTransformPoint(head.position);
            var eyeMesh=new Mesh();bindings.Eyes.BakeMesh(eyeMesh,true);
            var eyeMatrix=model.worldToLocalMatrix*bindings.Eyes.localToWorldMatrix;
            var eyePoints=eyeMesh.vertices.Select(eyeMatrix.MultiplyPoint3x4).ToArray();Object.DestroyImmediate(eyeMesh);
            // FBX bone +Y points down the face. Derive its frame in the live model space,
            // including the imported-root orientation/origin used by this art scene.
            var face=model.InverseTransformVector(head.TransformVector(Vector3.up)).normalized;
            if(face.y>-.4f || face.z<.3f)throw new InvalidOperationException("Unexpected fitted Head axis: "+face);
            var across=Vector3.ProjectOnPlane(Vector3.right,face).normalized;
            var front=Vector3.Cross(face,across).normalized;
            var tip=skin.Ray(poll+face*.9f,-face).Point;
            float faceLength=Vector3.Dot(tip-poll,face);
            if(faceLength<.40f || faceLength>.85f)throw new InvalidOperationException("Unexpected neutral head frame for fitted bridle: "+faceLength);
            var mouth=poll+face*(faceLength*.72f);
            Vector3 Fit(Vector3 center,Vector3 outward,float gap=.007f)
                =>skin.Ray(center+outward*.8f,-outward).Point+outward*gap;
            Vector3 CrownNormal(float t){float a=Mathf.PI*(1-t);return new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);}
            Vector3 Crown(float t,float edge)=>Fit(poll+new Vector3(0,-.045f,-.065f+edge*.0125f),CrownNormal(t));
            Vector3 BrowNormal(float t){float a=Mathf.PI+.20f-(Mathf.PI+.40f)*t;return across*Mathf.Cos(a)+front*Mathf.Sin(a);}
            Vector3 Brow(float t,float edge)=>Fit(poll+face*(.025f+edge*.0105f),BrowNormal(t));
            // A western browband/snaffle headstall: crown behind the ears, brow above the
            // eyes, cheeks and mouth-side rings. No oversized floating cavesson/nose hoop.
            hide.Add(Crown,CrownNormal,24,.002f);hide.Add(Brow,BrowNormal,24,.002f);
            var bitPositions=new Vector3[2];var ringCenters=new Vector3[2];
            for(int side=-1;side<=1;side+=2)
            {
                var outward=Vector3.right*side;var ringCenter=Fit(mouth,outward,.012f);
                // Keep a planar compact ring outside the entire local lip envelope, not
                // just outside its centre point. Both sides are measured independently.
                for(int i=0;i<24;i++)
                {
                    float a=i*Mathf.PI*2/24;var point=mouth+face*(Mathf.Cos(a)*.027f)+front*(Mathf.Sin(a)*.027f);
                    ringCenter.x=side*Mathf.Max(side*ringCenter.x,side*Fit(point,outward,.008f).x);
                }
                hardware.Tube(Ellipse(ringCenter,face*.027f,front*.027f,24),.0035f,8,false);
                var crown=Crown(side<0?0:1,0);var brow=Brow(side<0?0:1,0);var ringTop=ringCenter-face*.027f;
                var sideEye=eyePoints.Where(p=>side*p.x>0).ToArray();
                if(sideEye.Length==0)throw new InvalidOperationException("Missing fitted eye on side "+side);
                var eyeBounds=new Bounds(sideEye[0],Vector3.zero);
                foreach(var point in sideEye)eyeBounds.Encapsulate(point);
                Vector3 Hermite(Vector3 a,Vector3 b,Vector3 da,Vector3 db,float t)
                {float t2=t*t,t3=t2*t;return (2*t3-3*t2+1)*a+(t3-2*t2+t)*da+(-2*t3+3*t2)*b+(t3-t2)*db;}
                Vector3 Guide(float t)
                {
                    var point=t<.24f
                        ?Hermite(crown,brow,brow-crown,Vector3.forward*.10f,t/.24f)
                        :Hermite(brow,ringTop,Vector3.forward*.26f,face*.40f,(t-.24f)/.76f);
                    // Route behind the measured eyelid/eye envelope. The 25 mm centre
                    // margin includes 11.5 mm half-width plus at least 10 mm clear skin.
                    // Hold full clearance around the eye, then fade over another 40 mm.
                    // Both fade boundaries have zero slope; there is no cutoff-position jump.
                    float dy=Mathf.Max(0,Mathf.Abs(point.y-eyeBounds.center.y)-eyeBounds.extents.y);
                    if(t>.24f && t<1 && dy<.065f)
                    {
                        float rear=eyeBounds.min.z-.025f;
                        float delta=point.z-rear;
                        float fade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.025f,.065f,dy));
                        point.z-=fade*.5f*(delta+Mathf.Sqrt(delta*delta+.000001f));
                    }
                    return point;
                }
                Vector3 Cheek(float t,float edge)
                {
                    var point=Guide(t);var tangent=(Guide(Mathf.Min(1,t+.001f))-Guide(Mathf.Max(0,t-.001f))).normalized;
                    var width=Vector3.Cross(outward,tangent).normalized;
                    var fitted=Fit(point+width*(edge*.0115f),outward);
                    // The last leather fold meets the upper ring instead of ending against skin.
                    float attach=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.90f,1,t));
                    fitted.x=side*Mathf.Max(side*fitted.x,Mathf.Lerp(side*fitted.x,side*ringTop.x,attach));
                    return fitted;
                }
                hide.Add(Cheek,t=>outward,36,.002f);
                for(int s=0;s<20;s++)
                {
                    float t=.035f+s*.045f;
                    for(int edge=-1;edge<=1;edge+=2)
                        seams.Tube(new[]{Cheek(t,edge*.70f)+outward*.0028f,Cheek(t+.012f,edge*.70f)+outward*.0028f},.0007f,4,true);
                }
                var buckle=Cheek(.43f,0)+outward*.005f;
                var along=(Guide(.44f)-Guide(.42f)).normalized;var cross=Vector3.Cross(outward,along).normalized;
                hardware.Tube(Ellipse(buckle,along*.020f,cross*.014f,16),.0023f,6,false);
                hardware.Tube(new[]{buckle-cross*.013f,buckle+cross*.013f},.0018f,6,true);
                bitPositions[side<0?0:1]=ringCenter-front*.027f;
                ringCenters[side<0?0:1]=ringCenter;
            }
            // Mouthpiece terminates at the fitted sides; it does not float across the nose.
            hardware.Tube(new[]{ringCenters[0],mouth,ringCenters[1]},.005f,8,true);
            MeshObject(bridle,"Fitted leather headstall",SaveMesh("Western leather headstall",hide.Mesh()),new[]{leather});
            MeshObject(bridle,"Bit rings and buckles",SaveMesh("Bridle metal fittings",hardware.Mesh()),new[]{steel});
            MeshObject(bridle,"Headstall stitching",SaveMesh("Headstall stitches",seams.Mesh()),new[]{thread});
            leftBit=Child(bridle,"Left bit anchor");leftBit.localPosition=bitPositions[0];
            rightBit=Child(bridle,"Right bit anchor");rightBit.localPosition=bitPositions[1];
            bridle.SetParent(head,true);
            Debug.Log("HERO_BRIDLE_FIT: head="+poll.ToString("F4")+", face length="+faceLength.ToString("F4")+", left bit="+bitPositions[0].ToString("F4")+", right bit="+bitPositions[1].ToString("F4")+"; three existing material batches");
        }

        private static Vector3[] Ellipse(Vector3 center,Vector3 a,Vector3 b,int count)
        {var points=new Vector3[count+1];for(int i=0;i<=count;i++){float t=i*Mathf.PI*2/count;points[i]=center+Mathf.Cos(t)*a+Mathf.Sin(t)*b;}return points;}
        private static Transform Child(Transform parent,string name)
        {var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}
        private static MeshFilter MeshObject(Transform parent,string name,Mesh mesh,Material[] materials)
        {var t=Child(parent,name);var filter=t.gameObject.AddComponent<MeshFilter>();filter.sharedMesh=mesh;var renderer=t.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterials=materials;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;return filter;}

        private static Mesh SaveMesh(string name,Mesh mesh)
        {mesh.name="Fitted "+name;return PersistentMeshAsset.Save(mesh,HeroHorseBenchmarkBuilder.Root+"/"+mesh.name+".asset");}
    }
}
