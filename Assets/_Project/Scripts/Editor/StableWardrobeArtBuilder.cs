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
    /// <summary>Original inspectable glove and shared material variants. No gameplay or ownership authority.</summary>
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
            var thread=StableTackBuilder.Solid("Wardrobe flax stitching",new Color(.58f,.44f,.26f),.12f);
            var sleeve=Required(ReinsRiderTackBuilder.Root+"/Charcoal denim sleeves.mat");
            var shell=new Surface();var seams=new Surface();var denim=new Surface();
            // An open, relaxed left glove with a tapered wrist, broad knuckles and distinct finger lengths.
            shell.Loft(new[]{new Vector3(0,.47f,0),new Vector3(0,.60f,0),new Vector3(-.015f,.81f,0),new Vector3(-.02f,1.04f,.018f)},
                new[]{new Vector2(.125f,.07f),new Vector2(.14f,.085f),new Vector2(.205f,.096f),new Vector2(.19f,.088f)},24,true);
            for(int i=0;i<4;i++) {
                float x=-.173f+i*.112f;float length=i==0?.33f:i==1?.41f:i==2?.385f:.30f;
                float spread=(i-1.5f)*.037f;float radius=i==3?.046f:.052f;
                var path=new[]{new Vector3(x,1.0f,.02f),new Vector3(x+spread*.45f,1.14f,.035f),new Vector3(x+spread,1.02f+length*.66f,.052f),new Vector3(x+spread*.96f,1.02f+length*.86f,.073f),new Vector3(x+spread*.90f,1.02f+length,.085f),new Vector3(x+spread*.90f,1.038f+length,.086f)};
                shell.Loft(path,new[]{new Vector2(radius,radius*.82f),new Vector2(radius*.94f,radius*.78f),new Vector2(radius*.90f,radius*.76f),new Vector2(radius*.77f,radius*.69f),new Vector2(radius*.34f,radius*.31f),new Vector2(.0015f,.0015f)},14,true);
                for(int side=-1;side<=1;side+=2)for(int stitch=0;stitch<12;stitch++) {
                    float t=stitch/12f;var p=Vector3.Lerp(path[0],path[path.Length-1],t);p.x+=side*radius*.7f;p.z+=radius*.70f;
                    seams.Tube(new[]{p,p+new Vector3(0,.012f,.001f)},.0018f,5);
                }
            }
            shell.Loft(new[]{new Vector3(.12f,.76f,0),new Vector3(.255f,.86f,.01f),new Vector3(.33f,1.02f,.06f),new Vector3(.35f,1.12f,.10f),new Vector3(.354f,1.146f,.108f)},
                new[]{new Vector2(.085f,.07f),new Vector2(.069f,.062f),new Vector2(.055f,.05f),new Vector2(.030f,.032f),new Vector2(.002f,.002f)},16,true);
            // Double rolled cuff and stitched back panel remain part of the same wearable appearance.
            shell.Loft(new[]{new Vector3(0,.455f,0),new Vector3(0,.50f,0),new Vector3(0,.545f,0)},new[]{new Vector2(.147f,.094f),new Vector2(.149f,.092f),new Vector2(.14f,.085f)},24,true);
            for(int s=0;s<24;s++) {
                float a=s*Mathf.PI*2/24;var p=new Vector3(Mathf.Cos(a)*.145f,.515f,Mathf.Sin(a)*.09f);
                seams.Tube(new[]{p,p+new Vector3(-Mathf.Sin(a)*.011f,0,Mathf.Cos(a)*.007f)},.0018f,5);
            }
            for(int side=-1;side<=1;side+=2)for(int s=0;s<12;s++) {
                float t=s/12f;var p=new Vector3(side*(.11f+.05f*t),.64f+t*.33f,.087f);
                seams.Tube(new[]{p,p+new Vector3(side*.001f,.012f,.001f)},.0018f,5);
            }
            denim.Loft(new[]{new Vector3(.13f,-.55f,-.08f),new Vector3(.045f,.13f,-.02f),new Vector3(0,.47f,0)},
                new[]{new Vector2(.225f,.18f),new Vector2(.17f,.12f),new Vector2(.12f,.071f)},24,true);
            var renderer=Part(root,"Inspect glove shell",Save("Open western glove",shell.Mesh()),glove);
            Part(root,"Inspect glove seams",Save("Open glove stitching",seams.Mesh()),thread);
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
