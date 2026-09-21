using System;
using System.Collections.Generic;
using System.IO;
using BarrelRivals.Core.Stable;
using BarrelRivals.Practice;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace BarrelRivals.Editor
{
    /// <summary>CC0-derived glove inspection and original shared palettes. No gameplay or ownership authority.</summary>
    public static class StableWardrobeArtBuilder
    {
        public const string Root=StableTackBuilder.Root+"/Wardrobe";
        public static StableAppearance.Palette[] Palettes()
        {
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            string premium=ReinsPremiumArenaBuilder.Root+"/Materials/";
            var ranch=Required(premium+"Ranch saddle leather.mat");
            var glove=Required(premium+"Worn chestnut gloves.mat");
            var bridle=Required(premium+"Oiled bridle leather.mat");
            var result=new List<StableAppearance.Palette>();
            void Add(string id,Material material)=>result.Add(new StableAppearance.Palette{gearId=id,material=material});
            Add("saddle-ranch",ranch);Add("saddle-midnight",Required(premium+"Midnight saddle leather.mat"));
            Add("saddle-rodeo-gold",Variant("Rodeo gold saddle",ranch,new Color(1.65f,1.45f,.94f)));
            Add("saddle-turquoise-trail",Variant("Turquoise trail saddle",ranch,new Color(.22f,1.15f,1.6f)));
            Add("saddle-ember",Variant("Ember saddle",ranch,new Color(1.20f,.48f,.32f)));
            Add("saddle-champion",Variant("Champion saddle",ranch,new Color(.72f,.49f,.39f)));
            Add("saddle-outlaw",Variant("Outlaw saddle",ranch,new Color(.25f,.29f,.31f)));
            Add("saddle-sunset",Variant("Sunset saddle",ranch,new Color(1.50f,1.05f,.68f)));
            Add("pad-desert",Required(StableTackBuilder.Root+"/Desert woven wool.mat"));
            Add("pad-turquoise",Required(StableTackBuilder.Root+"/Turquoise woven wool.mat"));
            Add("reins-turquoise",Required(ReinsRiderTackBuilder.Root+"/Turquoise rein braid.mat"));
            Add("reins-crimson",Required(StableTackBuilder.Root+"/Crimson rein braid.mat"));
            Add("headstall-ranch",bridle);Add("headstall-midnight",Variant("Midnight headstall",bridle,new Color(.14f,.15f,.17f)));
            Add("gloves-classic",glove);Add("gloves-blackout",Variant("Blackout gloves",glove,new Color(.075f,.09f,.105f)));
            Add("gloves-whiskey",Variant("Whiskey gloves",glove,new Color(1.1f,.81f,.52f)));
            Add("gloves-rodeo-red",Variant("Rodeo red gloves",glove,new Color(1.0f,.23f,.13f)));
            Add("gloves-steelhide",Variant("Steelhide gloves",glove,new Color(.65f,.86f,1.15f)));
            Add("gloves-midnight",Variant("Midnight gloves",glove,new Color(.16f,.28f,.52f)));
            // The anatomical glove has a continuous outer surface. Keep leather
            // grain restrained at close range and cull its hidden interior.
            foreach(var item in StableCatalog.Gear)if(item.Slot==StableSlot.Gloves)
            {
                var material=result.Find(p=>p.gearId==item.Id).material;
                material.shader=Shader.Find("Barrel Rivals/Tailored Leather");
                material.SetFloat("_BumpScale",.07f);material.SetFloat("_Smoothness",.30f);
                material.SetColor("_ThreadColor",new Color(.52f,.36f,.18f));
                material.SetFloat("_Cull",2);EditorUtility.SetDirty(material);
            }
            foreach(var item in StableCatalog.Gear)
                if(!result.Exists(p=>p.gearId==item.Id))throw new InvalidOperationException("Missing visual for catalog item "+item.Id);
            return result.ToArray();
        }
        private static Material Required(string path)
        {var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m)throw new FileNotFoundException("Missing shared wardrobe material",path);return m;}
        private static Material Variant(string name,Material source,Color tint)
        {
            string path=Root+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material)return material;
            // All styles share photographic maps; do not duplicate a mask/normal texture for each color.
            material=new Material(source){name=name};material.SetColor("_BaseColor",tint);material.SetFloat("_Smoothness",.42f);
            AssetDatabase.CreateAsset(material,path);return material;
        }
        public static Transform BuildPreview()
        {
            var palettes=Palettes();var go=new GameObject("Rider glove preview");var root=go.transform;
            root.position=new Vector3(0,0,1.5f);
            Material glove=Array.Find(palettes,p=>p.gearId=="gloves-classic").material;
            var sleeve=Required(ReinsRiderTackBuilder.Root+"/Charcoal denim sleeves.mat");
            var denim=new Surface();
            // The same anatomical source supplies the open inspection and closed
            // racing poses. No separate tube fingers or floating seam geometry.
            denim.Loft(new[]{new Vector3(.13f,-.55f,-.08f),new Vector3(.045f,.13f,-.02f),new Vector3(0,.47f,0)},
                new[]{new Vector2(.225f,.18f),new Vector2(.17f,.12f),new Vector2(.12f,.071f)},24,true);
            var renderer=Part(root,"Inspect glove shell",Save("Open western glove",GloveTailoringBuilder.Finish(ReinsGloveBuilder.Inspection())),glove);
            renderer.transform.localPosition=new Vector3(0,.60f,0);
            renderer.transform.localRotation=Quaternion.Euler(-90,180,0);
            renderer.transform.localScale=Vector3.one*4f;
            Part(root,"Inspect denim sleeve",Save("Inspect forearm",denim.Mesh()),sleeve);
            var appearance=go.AddComponent<StableAppearance>();appearance.Configure(new[]{new StableAppearance.Binding{renderer=renderer,materialIndex=0,slot=StableSlot.Gloves}},palettes);
            appearance.Apply(StableProfile.Starter());root.gameObject.SetActive(false);return root;
        }
        private static Renderer Part(Transform parent,string name,Mesh mesh,Material material)
        {var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;return r;}
        private static Mesh Save(string name,Mesh mesh)
        {mesh.name=name;return PersistentMeshAsset.Save(mesh,Root+"/"+name+".asset");}
        private sealed class Surface
        {
            readonly List<Vector3> vertices=new List<Vector3>();readonly List<Vector2> uvs=new List<Vector2>();readonly List<int> indices=new List<int>();
            public void Tube(Vector3[] p,float radius,int sides)=>Loft(p,Array.ConvertAll(p,v=>new Vector2(radius,radius)),sides,true);
            public void Loft(Vector3[] p,Vector2[] size,int sides,bool caps)
            {
                int first=vertices.Count;
                for(int r=0;r<p.Length;r++) {
                    var forward=(p[Math.Min(r+1,p.Length-1)]-p[Math.Max(0,r-1)]).normalized;
                    var across=Vector3.Cross(forward,Vector3.forward).normalized;if(across.sqrMagnitude<.01f)across=Vector3.right;
                    var depth=Vector3.Cross(across,forward).normalized;
                    for(int j=0;j<sides;j++){float a=j*Mathf.PI*2/sides;vertices.Add(p[r]+across*Mathf.Cos(a)*size[r].x+depth*Mathf.Sin(a)*size[r].y);uvs.Add(new Vector2(j/(float)sides,r*.3f));}
                    if(r==0)continue;
                    for(int j=0;j<sides;j++){int a=first+(r-1)*sides+j,b=first+(r-1)*sides+(j+1)%sides;Tri(a,a+sides,b);Tri(b,a+sides,b+sides);}
                }
                if(caps){int near=vertices.Count;vertices.Add(p[0]);uvs.Add(Vector2.zero);int far=vertices.Count;vertices.Add(p[p.Length-1]);uvs.Add(Vector2.one);for(int j=0;j<sides;j++){Tri(near,first+j,first+(j+1)%sides);int last=first+(p.Length-1)*sides;Tri(far,last+(j+1)%sides,last+j);}}
            }
            void Tri(int a,int b,int c){indices.Add(a);indices.Add(b);indices.Add(c);}
            public Mesh Mesh(){var m=new Mesh();m.SetVertices(vertices);m.SetUVs(0,uvs);m.SetTriangles(indices,0);m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();return m;}
        }
    }
}
