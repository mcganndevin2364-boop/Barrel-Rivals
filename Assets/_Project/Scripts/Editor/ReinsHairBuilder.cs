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
            // A short crest undercoat and irregular longer outer locks avoid parallel roof tiles.
            // Fit the actual neutral mesh, then allow only the distal ends to leave its surface.
            for(int row=0;row<2;row++)for(int i=0;i<(row==0?34:26);i++)
            {
                int count=row==0?34:26;
                float t=Mathf.Clamp01((i+Random(-.38f,.38f))/(count-1f));
                float rootZ=origin+Mathf.Lerp(.08f,1.02f,t);
                float length=Random(row==0?.18f:.30f,row==0?.28f:.44f)*(1-.20f*t);
                float width=Random(row==0?.045f:.045f,row==0?.063f:.073f);
                float sweep=Random(.025f,.065f),loose=Random(row==0?.002f:.012f,row==0?.008f:.025f),wave=Random(0,Mathf.PI*2);
                int layer=row,helper=maneHelpers[Mathf.Min(5,Mathf.FloorToInt(t*6))];
                shape.AddFitted((u,side)=>{
                    float taper=1-.76f*Mathf.Pow(u,3.4f);
                    float z=rootZ+(side-.5f)*width*taper-sweep*Mathf.SmoothStep(0,1,u)
                        +Mathf.Sin(u*Mathf.PI*2+wave)*.004f*Mathf.Sin(u*Mathf.PI);
                    var crest=surface.Top((layer-.5f)*.006f,z);float drop=length*u*(.80f+.20f*u);
                    if(u==0)return new ReinsHorseSurface.Hit(crest.Point+Vector3.up*(.007f+layer*.003f),crest.Weight);
                    var flank=surface.Side(crest.Point.y-drop,z);
                    float separation=.008f+layer*.005f+loose*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,1,u));
                    var outer=flank.Point+Vector3.right*separation;
                    var point=Vector3.Lerp(crest.Point+Vector3.up*.008f,outer,Mathf.Clamp01(drop/.025f));
                    return new ReinsHorseSurface.Hit(point,WithHelper(flank.Weight,helper,u,.62f));
                },random.Next(8),10,0,row);
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
            if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=dense?"Dense undercoat hair":"Dark strand hair"};AssetDatabase.CreateAsset(material,path);}
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));material.SetColor("_BaseColor",dense?Color.white:new Color(.24f,.25f,.28f,1));
            material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.36f);material.SetFloat("_Cull",0);
            material.SetFloat("_AlphaToMask",1);material.SetFloat("_Smoothness",.30f);material.SetFloat("_EnvironmentReflections",1);material.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF");material.SetFloat("_Metallic",0);
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
            public void AddFitted(Func<float,int,ReinsHorseSurface.Hit> point,int bundle,int segments,int region,int layer)
            {
                int start=vertices.Count;CardCount++;var indices=batches[layer];
                for(int i=0;i<=segments;i++)
                {
                    float t=i/(float)segments;
                    for(int side=0;side<2;side++)
                    {
                        var hit=point(t,side);vertices.Add(hit.Point);weights.Add(hit.Weight);
                        uv.Add(new Vector2((bundle+(side==0?.04f:.96f))/8f,.976f-t*.946f));
                        // Authoring landmarks retained for reload/attachment QA; shaders use UV0 only.
                        regions.Add(new Vector2(region,t));
                    }
                    if(i==0)continue;int a=start+(i-1)*2;
                    // Mane and tail ribbons run downward; the forelock runs forward. Keep
                    // their geometric normals facing outward for correct two-sided Lit shading.
                    if(region==1){indices.Add(a);indices.Add(a+2);indices.Add(a+1);indices.Add(a+1);indices.Add(a+2);indices.Add(a+3);}
                    else{indices.Add(a);indices.Add(a+1);indices.Add(a+2);indices.Add(a+1);indices.Add(a+3);indices.Add(a+2);}
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
