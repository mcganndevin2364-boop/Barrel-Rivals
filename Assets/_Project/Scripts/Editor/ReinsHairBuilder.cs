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
        private static readonly Vector3[] Crest={
            new Vector3(0,1.833f,.774f),new Vector3(0,1.848f,.950f),new Vector3(0,1.874f,1.035f),
            new Vector3(0,1.898f,1.150f),new Vector3(0,1.934f,1.257f),new Vector3(0,1.969f,1.360f),
            new Vector3(0,2.013f,1.546f),new Vector3(0,2.026f,1.643f),new Vector3(0,2.033f,1.706f),
            new Vector3(0,2.045f,1.818f),new Vector3(0,2.036f,1.858f)
        };
        // Measured outer body envelope across each 70mm-wide strip. Following
        // only the crest with an arbitrary Bezier sweep buried the hair in the neck.
        private static readonly float[] ProfileZ={.774f,.950f,1.150f,1.360f,1.546f,1.643f,1.706f,1.818f};
        private static readonly float[] ProfileDrop={0,.025f,.05f,.10f,.15f,.20f,.25f,.30f,.35f,.40f};
        private static readonly float[,] ProfileX={
            {.0117f,.0639f,.1134f,.1933f,.2549f,.2982f,.3530f,.3689f,.3807f,.3893f},
            {.0234f,.0686f,.1068f,.1730f,.2392f,.2847f,.3281f,.3602f,.3796f,.3855f},
            {.0285f,.0698f,.0936f,.1346f,.1860f,.2286f,.2568f,.2879f,.3282f,.3503f},
            {.0239f,.0671f,.0920f,.1354f,.1512f,.1624f,.1767f,.1938f,.2136f,.2299f},
            {.0266f,.0767f,.1012f,.1420f,.1441f,.1567f,.1624f,.1711f,.1738f,.1737f},
            {.0310f,.0850f,.1142f,.1424f,.1695f,.1688f,.1584f,.1536f,.1536f,.1338f},
            {.0406f,.0883f,.1197f,.1439f,.1655f,.1671f,.1832f,.1750f,.1454f,.0807f},
            {.1304f,.1614f,.1504f,.1455f,.1538f,.1679f,.1839f,.1951f,.1929f,.1706f}
        };
        public static void Build(Transform horse)
        {
            var model=horse.Find("Reference horse");if(!model)throw new InvalidOperationException("Hair requires the rigged horse.");
            var nodes=model.GetComponentsInChildren<Transform>(true);
            Transform Bone(string name)=>nodes.First(t=>t.name==name);
            var bones=new[]{Bone("Bone.001"),Bone("Bone.002"),Bone("Bone.003"),Bone("Bone.004")};
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            // Old closed clumps had no skin weights and stayed frozen while the neck moved.
            // Deactivate them so thumbnail/ghost generation cannot accidentally revive them.
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
                if(renderer.name.StartsWith("HorseHair",StringComparison.Ordinal))renderer.gameObject.SetActive(false);
            var previous=model.Find(RendererName);if(previous)Object.DestroyImmediate(previous.gameObject);
            var shape=new Cards();var random=new System.Random(41803);
            float Random(float low,float high)=>Mathf.Lerp(low,high,(float)random.NextDouble());
            // Two overlapping rows follow measured dorsal skin points, with independent lengths and parting.
            for(int row=0;row<2;row++)for(int i=0;i<30;i++)
            {
                float t=Mathf.Clamp01((i+Random(-.25f,.25f))/29f);
                // Stop the neck curtain before the ear bases; the separate forelock covers the poll.
                float rootZ=Mathf.Lerp(.774f,1.78f,t),length=Random(.22f,.38f)*(1-.34f*t);
                if(row==0)length*=.80f;
                int strandRow=row;
                shape.AddProfile(u=>ManePoint(rootZ,length*u,strandRow),Vector3.forward,Random(.044f,.070f),i%8,10,
                    NeckWeight(Mathf.InverseLerp(.774f,1.858f,rootZ)));
            }
            // Short forelock lies between the ears; it follows the head rather than the torso.
            for(int i=0;i<10;i++)
            {
                float side=Random(-.055f,.055f);var root=new Vector3(side,2.052f,1.835f+Random(0,.045f));
                var end=new Vector3(side+Random(-.025f,.025f),1.86f+Random(-.02f,.06f),2.014f);
                shape.Add(root,root+new Vector3(0,.012f,.065f),end+new Vector3(0,.09f,0),end,
                    Vector3.right,.045f,i%8,8,new BoneWeight{boneIndex0=1,weight0=1});
            }
            // Layered tail curtains follow the two existing tail bones. No cloth solver or runtime mesh rebuild.
            for(int ring=0;ring<3;ring++)for(int i=0;i<16;i++)
            {
                float angle=(i/16f+ring*.023f)*Mathf.PI*2;
                var radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                var root=new Vector3(.025f,1.585f,-.620f)+radial*(.018f+ring*.006f);
                var end=new Vector3(.025f,Random(.23f,.42f),-.91f)+radial*Random(.045f,.14f);
                shape.Add(root,new Vector3(.03f,1.43f,-.79f)+radial*.040f,
                    Vector3.Lerp(root,end,.65f)+new Vector3(0,-.1f,-.06f),end,
                    new Vector3(-Mathf.Sin(angle),0,Mathf.Cos(angle)),Random(.042f,.072f),i%8,12,
                    new BoneWeight{boneIndex0=2,weight0=.18f,boneIndex1=3,weight1=.82f});
            }
            var mesh=shape.Mesh();mesh.name="Skinned mane forelock and tail";
            mesh.bindposes=Array.ConvertAll(bones,b=>b.worldToLocalMatrix*model.localToWorldMatrix);
            string meshPath=Root+"/Skinned hair.asset";
            var saved=PersistentMeshAsset.Save(mesh,meshPath);
            var go=new GameObject(RendererName);go.transform.SetParent(model,false);
            var skin=go.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=saved;skin.bones=bones;skin.rootBone=model;
            skin.sharedMaterial=HairMaterial();skin.quality=SkinQuality.Bone2;skin.shadowCastingMode=ShadowCastingMode.On;
            skin.receiveShadows=true;skin.updateWhenOffscreen=false;
            var bounds=saved.bounds;bounds.Expand(.55f);skin.localBounds=bounds;
            AssetDatabase.SaveAssets();
            Debug.Log("BARREL_HAIR: "+shape.CardCount+" skinned cards, "+saved.vertexCount+" vertices, "+saved.triangles.Length/3+" triangles, one material; original clumps disabled.");
        }
        private static Vector3 CrestPoint(float t)
        {
            float z=Mathf.Lerp(Crest[0].z,Crest[Crest.Length-1].z,t);
            for(int i=1;i<Crest.Length;i++)if(z<=Crest[i].z)return Vector3.Lerp(Crest[i-1],Crest[i],Mathf.InverseLerp(Crest[i-1].z,Crest[i].z,z));
            return Crest[Crest.Length-1];
        }
        private static BoneWeight NeckWeight(float t)
        {
            float head=Mathf.SmoothStep(0,.38f,Mathf.InverseLerp(.68f,1,t));
            return new BoneWeight{boneIndex0=0,weight0=1-head,boneIndex1=1,weight1=head};
        }
        private static Vector3 ManePoint(float z,float drop,int row)
        {
            int zi=1,di=1;
            while(zi<ProfileZ.Length-1 && z>ProfileZ[zi])zi++;
            while(di<ProfileDrop.Length-1 && drop>ProfileDrop[di])di++;
            float zt=Mathf.InverseLerp(ProfileZ[zi-1],ProfileZ[zi],z),dt=Mathf.InverseLerp(ProfileDrop[di-1],ProfileDrop[di],drop);
            float x=Mathf.Lerp(Mathf.Lerp(ProfileX[zi-1,di-1],ProfileX[zi-1,di],dt),
                Mathf.Lerp(ProfileX[zi,di-1],ProfileX[zi,di],dt),zt)+.014f+row*.006f;
            float crest=CrestPoint(Mathf.InverseLerp(.774f,1.858f,z)).y;
            var root=new Vector3((row-.5f)*.012f,crest+.025f+row*.004f,z);
            var surface=new Vector3(x,crest+.012f-drop,z-.233333f*drop);
            return Vector3.Lerp(root,surface,Mathf.Clamp01(drop/.025f));
        }
        private static Material HairMaterial()
        {
            string texturePath=Root+"/Original strand atlas.png";
            if(!File.Exists(texturePath))
            {
                // Eight longitudinal bundles, drawn from deterministic fibres. No reference-image pixels.
                const int width=256,height=512;var pixels=new Color32[width*height];var rng=new System.Random(942);
                for(int bundle=0;bundle<8;bundle++)for(int strand=0;strand<22;strand++)
                {
                    float x0=2+(float)rng.NextDouble()*28,phase=(float)rng.NextDouble()*6.28f;
                    float length=.65f+(float)rng.NextDouble()*.35f,halfWidth=.30f+(float)rng.NextDouble()*.46f;
                    float shade=.15f+(float)rng.NextDouble()*.18f;
                    for(int y=0;y<height;y++)
                    {
                        float t=y/(float)(height-1);if(t>length)break;
                        float center=bundle*32+x0+Mathf.Sin(t*7+phase)*t*1.0f;
                        float taper=Mathf.SmoothStep(0,1,Mathf.Clamp01((length-t)*18));
                        for(int x=Mathf.Max(bundle*32,Mathf.FloorToInt(center-2));x<=Mathf.Min(bundle*32+31,Mathf.CeilToInt(center+2));x++)
                        {
                            float coverage=Mathf.Clamp01((halfWidth-Mathf.Abs(x+.5f-center))*1.7f+.5f)*taper;
                            int index=y*width+x;if(coverage*255<=pixels[index].a)continue;
                            float glint=shade*(.86f+.14f*Mathf.Sin(t*16+phase));
                            pixels[index]=new Color(glint,glint*.66f,glint*.44f,coverage);
                        }
                    }
                }
                var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);texture.SetPixels32(pixels);texture.Apply();
                File.WriteAllBytes(texturePath,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(texturePath);
            }
            var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.mipMapsPreserveCoverage=true;
            importer.alphaTestReferenceValue=.36f;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=512;
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
            readonly List<BoneWeight> weights=new List<BoneWeight>();readonly List<int> indices=new List<int>();
            public int CardCount{get;private set;}
            public void Add(Vector3 p0,Vector3 p1,Vector3 p2,Vector3 p3,Vector3 across,float width,int bundle,int segments,BoneWeight weight)
                =>AddProfile(t=>{float q=1-t;return q*q*q*p0+3*q*q*t*p1+3*q*t*t*p2+t*t*t*p3;},across,width,bundle,segments,weight);
            public void AddProfile(Func<float,Vector3> point,Vector3 across,float width,int bundle,int segments,BoneWeight weight)
            {
                int start=vertices.Count;CardCount++;
                for(int i=0;i<=segments;i++)
                {
                    float t=i/(float)segments;var center=point(t);
                    float half=width*.5f*Mathf.Lerp(1,.78f,t);
                    for(int side=0;side<2;side++){vertices.Add(center+across*(side==0?-half:half));uv.Add(new Vector2((bundle+(side==0?.02f:.98f))/8f,t));var skin=weight;
                        if(weight.boneIndex0==2){skin.weight1=Mathf.SmoothStep(.10f,.98f,Mathf.Clamp01(t*2));skin.weight0=1-skin.weight1;}
                        weights.Add(skin);}
                    if(i==0)continue;int a=start+(i-1)*2;indices.Add(a);indices.Add(a+2);indices.Add(a+1);indices.Add(a+1);indices.Add(a+2);indices.Add(a+3);
                }
            }
            public Mesh Mesh()
            {
                var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.boneWeights=weights.ToArray();
                mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
            }
        }
    }
}
