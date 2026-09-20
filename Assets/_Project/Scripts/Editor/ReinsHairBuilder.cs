using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
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
            var nodes=model.GetComponentsInChildren<Transform>(true);
            var bones=surface.Bones.Concat(new[]{nodes.Single(t=>t.name=="Bone.003"),nodes.Single(t=>t.name=="Bone.004")}).Distinct().ToArray();
            int tailBase=Array.FindIndex(bones,b=>b.name=="Bone.003"),tailTip=Array.FindIndex(bones,b=>b.name=="Bone.004");
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
                if(renderer.name.StartsWith("HorseHair",StringComparison.Ordinal))renderer.gameObject.SetActive(false);
            var previous=model.Find(RendererName);if(previous)Object.DestroyImmediate(previous.gameObject);
            var shape=new Cards();var random=new System.Random(41803);
            float Random(float low,float high)=>Mathf.Lerp(low,high,(float)random.NextDouble());
            // Fit the live imported model, whose in-place orientation correction has a
            // different origin from a separately rotated FBX inspection. Follow actual skin weights.
            for(int row=0;row<2;row++)for(int i=0;i<30;i++)
            {
                float t=Mathf.Clamp01((i+Random(-.25f,.25f))/29f);
                float rootZ=origin+Mathf.Lerp(.08f,1.02f,t),length=Random(.26f,.38f)*(1-.22f*t);
                if(row==0)length*=.80f;
                int layer=row;float width=Random(.062f,.083f);
                shape.AddFitted((u,side)=>{
                    float z=rootZ+(side-.5f)*width*Mathf.Lerp(.76f,1,u)-.11f*length*u;
                    var crest=surface.Top((layer-.5f)*.008f,z);float drop=length*u;
                    if(u==0)return new ReinsHorseSurface.Hit(crest.Point+Vector3.up*(.007f+layer*.003f),crest.Weight);
                    var flank=surface.Side(crest.Point.y-drop,z);
                    var outer=flank.Point+Vector3.right*(.009f+layer*.007f);
                    return new ReinsHorseSurface.Hit(Vector3.Lerp(crest.Point+Vector3.up*.008f,outer,Mathf.Clamp01(drop/.025f)),flank.Weight);
                },i%8,10,0);
            }
            // The poll/forehead is also sampled on actual skin; ear-base influences are retained.
            for(int i=0;i<10;i++)
            {
                float side=Random(-.027f,.027f),rootZ=origin+Random(1.065f,1.10f),endZ=origin+Random(1.24f,1.31f),width=Random(.035f,.046f);
                shape.AddFitted((u,edge)=>{
                    var hit=surface.Top(side+(edge-.5f)*width,Mathf.Lerp(rootZ,endZ,u));
                    return new ReinsHorseSurface.Hit(hit.Point+Vector3.up*(.009f+.006f*Mathf.Sin(u*Mathf.PI)),hit.Weight);
                },i%8,8,1);
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
                    new Vector3(-Mathf.Sin(angle),0,Mathf.Cos(angle)),Random(.048f,.076f),i%8,12,tailBase,tailTip);
            }
            var mesh=shape.Mesh();mesh.name="Skinned mane forelock and tail";
            mesh.bindposes=Array.ConvertAll(bones,b=>b.worldToLocalMatrix*model.localToWorldMatrix);
            var saved=PersistentMeshAsset.Save(mesh,Root+"/Skinned hair.asset");
            var go=new GameObject(RendererName);go.transform.SetParent(model,false);
            var skin=go.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=saved;skin.bones=bones;skin.rootBone=model;
            skin.sharedMaterial=HairMaterial();skin.quality=SkinQuality.Bone2;skin.shadowCastingMode=ShadowCastingMode.On;
            skin.receiveShadows=true;skin.updateWhenOffscreen=false;
            var bounds=saved.bounds;bounds.Expand(.55f);skin.localBounds=bounds;
            AssetDatabase.SaveAssets();
            Debug.Log("BARREL_HAIR: "+shape.CardCount+" fitted skinned cards, "+saved.vertexCount+" vertices, "+saved.triangles.Length/3+" triangles, one material; neutral body "+surface.Bounds+"; tail anchor "+tailRoot);
        }
        private static Material HairMaterial()
        {
            const string texturePath=Root+"/Original natural strand atlas.png";
            if(!File.Exists(texturePath))throw new InvalidOperationException("The reviewed original natural hair atlas is missing.");
            var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.mipMapsPreserveCoverage=true;
            importer.alphaTestReferenceValue=.36f;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=1024;
            importer.npotScale=TextureImporterNPOTScale.None;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.anisoLevel=4;importer.SaveAndReimport();
            string path=Root+"/Dark strand hair.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Dark strand hair"};AssetDatabase.CreateAsset(material,path);}
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));material.SetColor("_BaseColor",Color.white);
            material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.36f);material.SetFloat("_Cull",0);
            material.SetFloat("_AlphaToMask",1);material.SetFloat("_Smoothness",.16f);material.SetFloat("_EnvironmentReflections",0);material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");material.SetFloat("_Metallic",0);
            material.EnableKeyword("_ALPHATEST_ON");material.SetOverrideTag("RenderType","TransparentCutout");material.renderQueue=2450;
            material.doubleSidedGI=true;EditorUtility.SetDirty(material);return material;
        }
        private sealed class Cards
        {
            readonly List<Vector3> vertices=new List<Vector3>();readonly List<Vector2> uv=new List<Vector2>();
            readonly List<Vector2> regions=new List<Vector2>();
            readonly List<BoneWeight> weights=new List<BoneWeight>();readonly List<int> indices=new List<int>();
            public int CardCount{get;private set;}
            public void Add(Vector3 p0,Vector3 p1,Vector3 p2,Vector3 p3,Vector3 across,float width,int bundle,int segments,int tailBase,int tailTip)
                =>AddFitted((t,side)=>{
                    float q=1-t;var point=q*q*q*p0+3*q*q*t*p1+3*q*t*t*p2+t*t*t*p3;
                    float tip=Mathf.SmoothStep(.10f,.98f,Mathf.Clamp01(t*2));
                    return new ReinsHorseSurface.Hit(point+across*((side-.5f)*width*Mathf.Lerp(.76f,1,t)),
                        new BoneWeight{boneIndex0=tailBase,weight0=1-tip,boneIndex1=tailTip,weight1=tip});
                },bundle,segments,2);
            public void AddFitted(Func<float,int,ReinsHorseSurface.Hit> point,int bundle,int segments,int region)
            {
                int start=vertices.Count;CardCount++;
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
                var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetUVs(1,regions);mesh.SetTriangles(indices,0);mesh.boneWeights=weights.ToArray();
                mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
            }
        }
    }
}
