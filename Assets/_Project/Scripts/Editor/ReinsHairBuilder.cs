using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using BarrelRivals.Practice;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace BarrelRivals.Editor
{
    /// <summary>Original, bounded hair cards skinned to the existing horse rig. No simulation authority.</summary>
    public static class ReinsHairBuilder
    {
        public const string Root="Assets/_Project/Art/Reins/Hair";
        public const string RendererName="Horse strand hair";
        public static void Build(Transform horse)
        {
            var model=horse.Find("Reference horse");if(!model)throw new InvalidOperationException("Hair requires the rigged horse.");
            var surface=new ReinsHorseSurface(model);
            float origin=surface.Bounds.center.z;
            // Remove authored helpers before rebuilding so stale bones cannot enter the palette.
            foreach(var old in model.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Hair motion ",StringComparison.Ordinal)).ToArray())
                Object.DestroyImmediate(old.gameObject);
            var nodes=model.GetComponentsInChildren<Transform>(true);
            var bones=surface.Bones.Concat(new[]{nodes.Single(t=>t.name=="Bone.003"),nodes.Single(t=>t.name=="Bone.004")}).Distinct().ToArray();
            int tailBase=Array.FindIndex(bones,b=>b.name=="Bone.003"),tailTip=Array.FindIndex(bones,b=>b.name=="Bone.004");
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
                if(renderer.name.StartsWith("HorseHair",StringComparison.Ordinal))renderer.gameObject.SetActive(false);
            var previous=model.Find(RendererName);if(previous)Object.DestroyImmediate(previous.gameObject);
            var shape=new Cards();var random=new System.Random(41803);
            float Random(float low,float high)=>Mathf.Lerp(low,high,(float)random.NextDouble());
            // Independent helper pivots inherit the original rig. Surface weights remain exact
            // at roots; only distal strands blend to these small art-only rotations.
            var palette=bones.ToList();var motion=new List<ReinsHairMotion.Binding>();
            int[] maneHelpers=new int[6];
            for(int i=0;i<maneHelpers.Length;i++)
            {
                var crest=surface.Top(0,origin+Mathf.Lerp(.08f,1.02f,(i+.5f)/6));
                maneHelpers[i]=AddHelper(model,palette,motion,bones[crest.Weight.boneIndex0],crest.Point,
                    Vector3.forward,Vector3.up,.25f,3.6f,1.4f,i*.59f);
            }
            int[] tailHelpers=new int[2];
            for(int i=0;i<2;i++)tailHelpers[i]=AddHelper(model,palette,motion,bones[tailTip],
                model.InverseTransformPoint(bones[tailTip].position),Vector3.right,Vector3.forward,.6f,5.8f,3f,i*.85f);
            bones=palette.ToArray();
            // The dense layer keeps coverage; outer locks use a cupped, rolling cross-section.
            // Stagger their fitted crest roots and vary the flow along the neck, rather than
            // hanging every card in the same Z-width plane seen edge-on from the rider.
            for(int row=0;row<2;row++)for(int i=0;i<(row==0?34:26);i++)
            {
                int count=row==0?34:26;
                float t=Mathf.Clamp01((i+Random(-.38f,.38f))/(count-1f));
                float rootZ=origin+Mathf.Lerp(.08f,1.02f,t);
                float length=Random(row==0?.22f:.30f,row==0?.30f:.44f)*(1-.20f*t);
                // Atlas gutters and tapered roots cover only part of a card. The dense
                // roots must overlap after clipping, not merely touch as mesh quads.
                float width=Random(row==0?.090f:.060f,row==0?.115f:.085f);
                float sweep=Random(.025f,.065f),loose=Random(row==0?.002f:.012f,row==0?.008f:.025f),wave=Random(0,Mathf.PI*2);
                int layer=row,helper=maneHelpers[Mathf.Min(5,Mathf.FloorToInt(t*6))];
                // Keep the same random draw count so the unmodified forelock/tail retain
                // their previous authoring samples when only the mane is revised.
                int bundle=random.Next(8);
                float rootX=layer==0?-.055f:.009f+Mathf.Sin(wave+t*19)*.007f;
                rootZ+=Mathf.Sin(t*11+layer*.9f)*.011f;
                // One coherent groom flows toward the shoulder. Long-wave grouping
                // makes overlapping locks, instead of independent diagonal comb teeth.
                float flow=.8f*Mathf.Sin(t*7+.8f)+.2f*Mathf.Sin(wave);
                float endSweep=length*Mathf.Lerp(-.48f,-.19f,flow*.5f+.5f)+(sweep-.045f)*.3f;
                float bend=length*(.10f+.04f*Mathf.Sin(t*9+.7f));
                float roll=16*Mathf.Sin(t*9+.8f)+6*Mathf.Sin(wave);
                AddManeLock(shape,surface,rootX,rootZ,length,width,endSweep,bend,roll,loose,wave,helper,bundle,layer);
            }
            // The poll/forehead is also sampled on actual skin; ear-base influences are retained.
            for(int i=0;i<10;i++)
            {
                float side=Random(-.027f,.027f),rootZ=origin+Random(1.065f,1.10f),endZ=origin+Random(1.24f,1.31f),width=Random(.023f,.035f);
                shape.AddFitted((u,edge)=>{
                    var hit=surface.Top(side+(edge-.5f)*width*(1-.72f*u*u),Mathf.Lerp(rootZ,endZ,u));
                    return new ReinsHorseSurface.Hit(hit.Point+Vector3.up*(.009f+.006f*Mathf.Sin(u*Mathf.PI)),hit.Weight);
                },i%8,8,1,i<4?0:1);
            }
            // Tail roots use the rig landmark, independent of the scene's horse start position.
            // The lower-tail pivot is the anatomical dock; upper-tail origin is inside the rump.
            var tailRoot=model.InverseTransformPoint(bones[tailTip].position)+new Vector3(0,.015f,-.006f);
            for(int ring=0;ring<3;ring++)for(int i=0;i<16;i++)
            {
                float angle=(i/16f+ring*.023f)*Mathf.PI*2;
                var radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                var root=tailRoot+radial*(.018f+ring*.006f);
                var end=new Vector3(tailRoot.x,Random(.23f,.42f),tailRoot.z-.30f)+radial*Random(.045f,.12f);
                shape.Add(root,root+new Vector3(0,-.12f,-.10f)+radial*.025f,
                    Vector3.Lerp(root,end,.65f)+new Vector3(0,-.1f,-.06f),end,
                    new Vector3(-Mathf.Sin(angle),0,Mathf.Cos(angle)),Random(.055f,.078f),random.Next(8),12,tailBase,tailTip,tailHelpers[ring%2],ring==0?0:1);
            }
            var mesh=shape.Mesh();mesh.name="Skinned mane forelock and tail";
            mesh.bindposes=Array.ConvertAll(bones,b=>b.worldToLocalMatrix*model.localToWorldMatrix);
            var saved=PersistentMeshAsset.Save(mesh,Root+"/Skinned hair.asset");
            var go=new GameObject(RendererName);go.transform.SetParent(model,false);
            var skin=go.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=saved;skin.bones=bones;skin.rootBone=model;
            skin.sharedMaterials=new[]{HairMaterial(true),HairMaterial(false)};skin.quality=SkinQuality.Bone2;skin.shadowCastingMode=ShadowCastingMode.On;
            skin.receiveShadows=true;skin.updateWhenOffscreen=false;
            var bounds=saved.bounds;bounds.Expand(.55f);skin.localBounds=bounds;
            go.AddComponent<ReinsHairMotion>().Configure(horse.GetComponent<ReinsHorsePresentation>(),model.GetComponentInChildren<Animator>(),motion.ToArray());
            AssetDatabase.SaveAssets();
            Debug.Log("BARREL_HAIR: "+shape.CardCount+" fitted skinned cards, "+saved.vertexCount+" vertices, "+saved.triangles.Length/3+" triangles, two hair layers; neutral body "+surface.Bounds+"; tail anchor "+tailRoot);
        }
        private static void AddManeLock(Cards shape,ReinsHorseSurface surface,float rootX,float rootZ,
            float length,float width,float sweep,float bend,float roll,float loose,float wave,int helper,int bundle,int layer)
        {
            // Two rings cross the crown before draping down the flank. A direct chord
            // from a fitted root to the side cuts through the convex neck between vertices.
            const int segments=8;
            var centers=new Vector3[segments+1];var drops=new float[segments+1];
            for(int i=0;i<=segments;i++)
            {
                float u=i/(float)segments;
                float z=rootZ+sweep*Mathf.SmoothStep(0,1,u)+bend*Mathf.Sin(u*Mathf.PI);
                var crest=surface.Top(rootX,z);
                if(u<=.25f)
                    centers[i]=surface.Top(Mathf.Lerp(rootX,.09f,u/.25f),z).Point;
                else
                {
                    float drape=(u-.25f)/.75f;
                    float shoulderY=surface.Top(.09f,z).Point.y;
                    centers[i]=surface.Side(shoulderY-length*drape*(.80f+.20f*drape),z).Point;
                }
                drops[i]=crest.Point.y-centers[i].y;
            }
            shape.AddFitted((u,side)=>{
                int ring=Mathf.RoundToInt(u*segments);
                float taper=1-.76f*Mathf.Pow(u,3.4f),edge=(side-.5f)*width*taper;
                if(ring==0)
                {
                    // A shallow diagonal root line breaks the perfectly straight crest
                    // edge. Every vertex still takes its position and weights from skin.
                    float angle=layer==0?0:12*Mathf.Sin(wave)*Mathf.Deg2Rad;
                    var root=surface.Top(rootX+Mathf.Sin(angle)*edge,rootZ+Mathf.Cos(angle)*edge);
                    return new ReinsHorseSurface.Hit(root.Point+Vector3.up*(.007f+layer*.003f),root.Weight);
                }
                if(u<=.25f)
                {
                    // Sample every cross-section on the actual crown, including the
                    // center column. This is a surface arc, not an interpolation through skin.
                    var crown=surface.Top(Mathf.Lerp(rootX,.09f,u/.25f),centers[ring].z+edge);
                    var crownOffset=Vector3.up*(.009f+layer*.003f)+Vector3.right*(.005f*u/.25f);
                    return new ReinsHorseSurface.Hit(crown.Point+crownOffset,WithHelper(crown.Weight,helper,u,.62f));
                }
                var tangent=(centers[Mathf.Min(ring+1,segments)]-centers[ring-1]).normalized;
                var across=Vector3.Cross(tangent,Vector3.right).normalized;
                if(across.sqrMagnitude<.1f)across=Vector3.forward;
                float twist=roll*Mathf.SmoothStep(0,1,u)+10*Mathf.Sin(u*Mathf.PI)*Mathf.Sin(wave);
                across=Quaternion.AngleAxis(twist,tangent)*across;
                float z=centers[ring].z+across.z*edge;
                var crest=surface.Top(rootX,z);
                float drop=Mathf.Max(.003f,drops[ring]-across.y*edge*Mathf.SmoothStep(0,1,u/.35f));
                var flank=surface.Side(crest.Point.y-drop,z);
                float looseEnd=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,1,u));
                // The middle column bows slightly out of the card plane. Roll changes
                // which edge lifts; reprojection keeps both edges clear of the real neck.
                float cup=layer*.006f*Mathf.Sin(side*Mathf.PI)*Mathf.Sin(u*Mathf.PI);
                float clearance=Mathf.Clamp(.008f+layer*.006f+loose*.55f*looseEnd+across.x*edge+cup,.004f,.038f);
                var point=Vector3.Lerp(crest.Point+Vector3.up*.008f,flank.Point+Vector3.right*clearance,Mathf.Clamp01(drop/.025f));
                return new ReinsHorseSurface.Hit(point,WithHelper(flank.Weight,helper,u,.62f));
            },bundle,segments,0,layer,2);
        }
        private static int AddHelper(Transform model,List<Transform> bones,List<ReinsHairMotion.Binding> motion,
            Transform parent,Vector3 position,Vector3 lift,Vector3 sway,float idle,float stride,float turn,float phase)
        {
            var helper=new GameObject("Hair motion "+motion.Count).transform;
            helper.position=model.TransformPoint(position);helper.rotation=model.rotation;
            helper.SetParent(parent,true);
            int index=bones.Count;bones.Add(helper);
            motion.Add(new ReinsHairMotion.Binding{bone=helper,liftAxis=helper.InverseTransformDirection(model.TransformDirection(lift)),
                swayAxis=helper.InverseTransformDirection(model.TransformDirection(sway)),idleDegrees=idle,strideDegrees=stride,turnDegrees=turn,phaseRadians=phase});
            return index;
        }
        private static BoneWeight WithHelper(BoneWeight fitted,int helper,float progress,float strength)
        {
            float amount=strength*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.20f,1,progress));
            if(amount<.00001f)return fitted;
            // Retain the strongest two of the fitted pair plus the independent helper.
            // This preserves the mobile Bone2 contract and exact root attachments.
            float a=fitted.weight0*(1-amount),b=fitted.weight1*(1-amount);
            if(amount<=b)return fitted;
            float sum=a+amount;
            return new BoneWeight{boneIndex0=fitted.boneIndex0,weight0=a/sum,boneIndex1=helper,weight1=amount/sum};
        }
        private static Material HairMaterial(bool dense)
        {
            string texturePath=Root+(dense?"/Original natural strand atlas.png":"/Original separated strand atlas.png");
            if(!File.Exists(texturePath))throw new InvalidOperationException("The reviewed original natural hair atlas is missing.");
            var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.mipMapsPreserveCoverage=true;
            importer.alphaTestReferenceValue=.36f;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=1024;
            importer.npotScale=TextureImporterNPOTScale.None;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.anisoLevel=4;importer.SaveAndReimport();
            string path=Root+(dense?"/Dense undercoat hair.mat":"/Dark strand hair.mat");var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader=Shader.Find("Barrel Rivals/Horse Fiber");
            if(!shader)throw new InvalidOperationException("The horse fiber shader is missing.");
            if(!material){material=new Material(shader){name=dense?"Dense undercoat hair":"Dark strand hair"};AssetDatabase.CreateAsset(material,path);}
            material.shader=shader;
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));material.SetColor("_BaseColor",dense?Color.white:new Color(.38f,.32f,.28f,1));
            material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.36f);material.SetFloat("_Cull",0);
            material.SetFloat("_AlphaToMask",1);material.DisableKeyword("_ALPHATOMASK_ON");
            material.SetColor("_FiberTint",new Color(.80f,.80f,.78f));
            material.SetFloat("_PrimaryStrength",dense?.035f:.060f);material.SetFloat("_SecondaryStrength",dense?.015f:.028f);
            material.SetFloat("_PrimaryExponent",64);material.SetFloat("_SecondaryExponent",18);
            material.SetFloat("_PrimaryShift",.08f);material.SetFloat("_SecondaryShift",-.16f);
            material.SetFloat("_Scatter",dense?.18f:.28f);material.SetFloat("_RootShade",dense?.68f:.80f);
            material.EnableKeyword("_ALPHATEST_ON");material.SetOverrideTag("RenderType","TransparentCutout");material.renderQueue=2450;
            material.doubleSidedGI=true;EditorUtility.SetDirty(material);return material;
        }
        private sealed class Cards
        {
            readonly List<Vector3> vertices=new List<Vector3>();readonly List<Vector2> uv=new List<Vector2>();
            readonly List<Vector2> regions=new List<Vector2>();
            readonly List<BoneWeight> weights=new List<BoneWeight>();readonly List<int>[] batches={new List<int>(),new List<int>()};
            public int CardCount{get;private set;}
            public void Add(Vector3 p0,Vector3 p1,Vector3 p2,Vector3 p3,Vector3 across,float width,int bundle,int segments,int tailBase,int tailTip,int helper,int layer)
                =>AddFitted((t,side)=>{
                    float q=1-t;var point=q*q*q*p0+3*q*q*t*p1+3*q*t*t*p2+t*t*t*p3;
                    float tip=Mathf.SmoothStep(.10f,.98f,Mathf.Clamp01(t*2));
                    return new ReinsHorseSurface.Hit(point+across*((side-.5f)*width*(1-.75f*t*t)),
                        WithHelper(new BoneWeight{boneIndex0=tailTip,weight0=tip,boneIndex1=tailBase,weight1=1-tip},helper,t,.75f));
                },bundle,segments,2,layer);
            public void AddFitted(Func<float,float,ReinsHorseSurface.Hit> point,int bundle,int segments,int region,int layer,int acrossSegments=1)
            {
                int start=vertices.Count,stride=acrossSegments+1;CardCount++;var indices=batches[layer];
                for(int i=0;i<=segments;i++)
                {
                    float t=i/(float)segments;
                    for(int column=0;column<stride;column++)
                    {
                        float side=column/(float)acrossSegments;
                        var hit=point(t,side);vertices.Add(hit.Point);weights.Add(hit.Weight);
                        // Dense mane roots use the full lock interior. The atlas's
                        // narrow isolated tops made repeated bald notches at the crest.
                        float rootV=region==0 && layer==0?.80f:.976f;
                        uv.Add(new Vector2((bundle+Mathf.Lerp(.04f,.96f,side))/8f,Mathf.Lerp(rootV,.030f,t)));
                        // Authoring landmarks retained for reload/attachment QA; shaders use UV0 only.
                        regions.Add(new Vector2(region,t));
                    }
                    if(i==0)continue;
                    for(int column=0;column<acrossSegments;column++)
                    {
                        int a=start+(i-1)*stride+column;
                        // Mane/tail run downward; the forelock runs forward. Keep the
                        // original outward winding for each curved cross-section strip.
                        if(region==1){indices.Add(a);indices.Add(a+stride);indices.Add(a+1);indices.Add(a+1);indices.Add(a+stride);indices.Add(a+stride+1);}
                        else{indices.Add(a);indices.Add(a+1);indices.Add(a+stride);indices.Add(a+1);indices.Add(a+stride+1);indices.Add(a+stride);}
                    }
                }
            }
            public Mesh Mesh()
            {
                var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetUVs(1,regions);mesh.subMeshCount=2;for(int i=0;i<2;i++)mesh.SetTriangles(batches[i],i);mesh.boneWeights=weights.ToArray();
                mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
            }
        }
    }
}
