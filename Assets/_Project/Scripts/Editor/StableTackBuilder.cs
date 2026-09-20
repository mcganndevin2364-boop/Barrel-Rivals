using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BarrelRivals.Core.Stable;
using BarrelRivals.Practice;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace BarrelRivals.Editor
{
    /// <summary>Original western saddle/pad geometry, shared by the stable and racing presentation.</summary>
    public static class StableTackBuilder
    {
        public const string Root="Assets/_Project/Art/Reins/Stable";
        public static void Build(Transform horse)
        {
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            var model=horse.Find("Reference horse");
            if(!model)throw new InvalidOperationException("Western tack requires the reference horse.");
            var torso=model.GetComponentsInChildren<Transform>(true).SingleOrDefault(t=>t.name=="Bone");
            if(!torso)throw new InvalidOperationException("Western tack requires the exact torso bone 'Bone'.");
            // The saddle now lives below the rig. Remove descendants as well as old direct children
            // so rebuilding either representation cannot leave duplicate tack or stale bindings.
            foreach(var previous in model.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Western saddle").ToArray())
                if(previous)Object.DestroyImmediate(previous.gameObject);
            var root=new GameObject("Western saddle").transform;root.SetParent(model,false);
            var ranch=ReinsPremiumArenaBuilder.Pbr("Ranch saddle leather","Leather_Albedo_1K.jpg","Leather_NormalGL_1K.png","Leather_Roughness_1K.jpg",null,new Color(.56f,.31f,.16f),0);
            ranch.SetColor("_BaseColor",new Color(1.15f,1.15f,1.15f));ranch.SetFloat("_Smoothness",.45f);EditorUtility.SetDirty(ranch);
            var midnight=ReinsPremiumArenaBuilder.Pbr("Midnight saddle leather","Leather_Albedo_1K.jpg","Leather_NormalGL_1K.png","Leather_Roughness_1K.jpg",null,new Color(.075f,.085f,.09f),0);
            var desert=Solid("Desert woven wool",new Color(.44f,.20f,.09f),.08f);
            var turquoise=Solid("Turquoise woven wool",new Color(.023f,.25f,.25f),.08f);
            var thread=Solid("Blanket ivory stitching",new Color(.68f,.56f,.34f),.08f);
            var steel=Solid("Saddle silver",new Color(.45f,.48f,.5f),.48f,.8f);
            var red=Solid("Crimson rein braid",new Color(.52f,.025f,.028f),.25f);
            var teal=AssetDatabase.LoadAssetAtPath<Material>(ReinsRiderTackBuilder.Root+"/Turquoise rein braid.mat");
            var pad=new Shape();pad.Sheet(-.58f,.58f,-1.05f,.31f,(x,z)=>1.67f-.66f*x*x,24,24,.03f);
            var padRenderer=Part(root,"Woven saddle pad",Save("Curved western pad",pad.Mesh()),desert);
            var trim=new Shape();
            foreach(float z in new[]{-.99f,-.94f,-.86f,.12f,.21f,.26f})
                trim.Sheet(-.583f,.583f,z,z+.021f,(x,v)=>1.706f-.66f*x*x,24,1,.001f);
            Part(root,"Woven blanket stripes",Save("Blanket stripes",trim.Mesh()),thread);
            var leather=new Shape();
            leather.Sheet(-.43f,.43f,-.89f,.23f,(x,z)=>1.745f-.54f*x*x,24,20,.035f);
            // Contoured seat rises into the front swell and back cantle.
            leather.Sheet(-.285f,.285f,-.76f,.14f,(x,z)=>1.80f+.42f*x*x+.20f*Mathf.Pow(Mathf.Abs((z+.31f)/.45f),4),24,28,.045f,(x,z)=>1.745f-.54f*x*x);
            for(int side=-1;side<=1;side+=2) {
                leather.Ribbon(new[]{new Vector3(side*.28f,1.73f,-.20f),new Vector3(side*.44f,1.49f,-.19f),new Vector3(side*.46f,1.15f,-.18f),new Vector3(side*.44f,.88f,-.14f)},.15f,.018f);
                leather.Tube(new[]{new Vector3(side*.43f,.96f,-.21f),new Vector3(side*.47f,.85f,-.19f),new Vector3(side*.47f,.80f,-.09f),new Vector3(side*.40f,.80f,-.05f),new Vector3(side*.39f,.91f,-.13f)},.026f,10);
            }
            leather.Tube(new[]{new Vector3(0,1.96f,.16f),new Vector3(0,2.08f,.15f),new Vector3(0,2.11f,.15f)},.042f,16);
            leather.Tube(new[]{new Vector3(-.066f,2.11f,.15f),new Vector3(.066f,2.11f,.15f)},.023f,12);
            var saddleRenderer=Part(root,"Contoured western leather",Save("Western saddle leather",leather.Mesh()),ranch);
            var fittings=new Shape();
            for(int side=-1;side<=1;side+=2) {
                fittings.Tube(new[]{new Vector3(side*.39f,.802f,-.05f),new Vector3(side*.48f,.802f,-.19f)},.018f,10);
                var circle=new Vector3[25];for(int i=0;i<25;i++){float a=i*Mathf.PI*2/24;circle[i]=new Vector3(side*.428f,1.64f+Mathf.Sin(a)*.045f,-.52f+Mathf.Cos(a)*.045f);}
                fittings.Tube(circle,.006f,8);
            }
            Part(root,"Saddle hardware",Save("Saddle fittings",fittings.Mesh()),steel);
            // Mesh coordinates were authored in normalized Reference-horse space. Preserve that
            // world bind pose while attaching to the torso, never the independently turning neck.
            root.SetParent(torso,true);
            var bindings=new List<StableAppearance.Binding> {
                new StableAppearance.Binding{renderer=saddleRenderer,materialIndex=0,slot=StableSlot.Saddle},
                new StableAppearance.Binding{renderer=padRenderer,materialIndex=0,slot=StableSlot.Pad}
            };
            foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
                if(renderer.name=="Left braided rein" || renderer.name=="Right braided rein")bindings.Add(new StableAppearance.Binding{renderer=renderer,materialIndex=1,slot=StableSlot.Reins});
                else if(renderer.name=="Fitted leather headstall")bindings.Add(new StableAppearance.Binding{renderer=renderer,materialIndex=0,slot=StableSlot.Headstall});
                else if(renderer.name=="Glove shell" || renderer.name=="Glove grip panels")bindings.Add(new StableAppearance.Binding{renderer=renderer,materialIndex=0,slot=StableSlot.Gloves});
            var appearance=horse.GetComponent<StableAppearance>();
            if(!appearance)appearance=horse.gameObject.AddComponent<StableAppearance>();
            appearance.Configure(bindings.ToArray(),StableWardrobeArtBuilder.Palettes());
            appearance.Apply(StableProfile.Starter());
        }
        public static void MakeStablePose(Transform horse)
        {
            var model=horse.Find("Reference horse");
            var tack=horse.GetComponentInChildren<ReinsRiderTackPresentation>();if(tack)Object.DestroyImmediate(tack);
            foreach(var t in model.GetComponentsInChildren<Transform>(true))if(t.name=="Left rider grip" || t.name=="Right rider grip")t.gameObject.SetActive(false);
            foreach(var filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if(filter.name!="Left braided rein" && filter.name!="Right braided rein")continue;
                int side=filter.name.StartsWith("Left")?-1:1;
                var cord=new Shape();var points=new Vector3[41];
                for(int i=0;i<points.Length;i++) {
                    float t=i/40f;points[i]=Vector3.Lerp(new Vector3(side*.055f,2.09f,.16f),new Vector3(side*.177f,1.69f,2.10f),t)+new Vector3(side*.08f*Mathf.Sin(t*Mathf.PI),-.33f*Mathf.Sin(t*Mathf.PI),0);
                }
                cord.Tube(points,.011f,12);filter.sharedMesh=Save(side<0?"Stowed left rein":"Stowed right rein",cord.Mesh());
                // Keep the palette's braid slot at index one in both scenes.
                var mesh=Object.Instantiate(filter.sharedMesh);var triangles=mesh.triangles;mesh.subMeshCount=3;mesh.SetTriangles(Array.Empty<int>(),0);mesh.SetTriangles(triangles,1);mesh.SetTriangles(Array.Empty<int>(),2);
                filter.sharedMesh=Save(side<0?"Stowed left rein slotted":"Stowed right rein slotted",mesh);
            }
        }
        private static StableAppearance.Palette Palette(string id,Material material)=>new StableAppearance.Palette{gearId=id,material=material};
        public static Material Solid(string name,Color color,float smoothness,float metallic=0)
        {string path=Root+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smoothness);m.SetFloat("_Metallic",metallic);AssetDatabase.CreateAsset(m,path);return m;}
        private static Renderer Part(Transform parent,string name,Mesh mesh,Material material)
        {var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<Renderer>();renderer.sharedMaterial=material;return renderer;}
        private static Mesh Save(string name,Mesh mesh)
        {mesh.name=name;return PersistentMeshAsset.Save(mesh,Root+"/"+name+".asset");}
        private sealed class Shape
        {
            private readonly List<Vector3> v=new List<Vector3>();private readonly List<Vector2> uv=new List<Vector2>();private readonly List<int> triangles=new List<int>();
            public void Sheet(float x0,float x1,float z0,float z1,Func<float,float,float> height,int nx,int nz,float thickness,Func<float,float,float> bottom=null)
            {
                int start=v.Count;
                for(int layer=0;layer<2;layer++)for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++) {
                    float px=Mathf.Lerp(x0,x1,x/(float)nx),pz=Mathf.Lerp(z0,z1,z/(float)nz);
                    v.Add(new Vector3(px,layer==1 && bottom!=null?bottom(px,pz):height(px,pz)-layer*thickness,pz));uv.Add(new Vector2(px*3,pz*3));
                }
                int count=(nx+1)*(nz+1);
                for(int layer=0;layer<2;layer++)for(int z=0;z<nz;z++)for(int x=0;x<nx;x++) {
                    int a=start+layer*count+z*(nx+1)+x,b=a+1,c=a+nx+1,d=c+1;
                    if(layer==0){Tri(a,c,b);Tri(b,c,d);}else{Tri(a,b,c);Tri(b,d,c);}
                }
                for(int x=0;x<nx;x++){Edge(start+x,start+x+1,count);Edge(start+nz*(nx+1)+x+1,start+nz*(nx+1)+x,count);}
                for(int z=0;z<nz;z++){Edge(start+(z+1)*(nx+1),start+z*(nx+1),count);Edge(start+z*(nx+1)+nx,start+(z+1)*(nx+1)+nx,count);}
            }
            private void Edge(int a,int b,int offset){Tri(a,b,a+offset);Tri(b,b+offset,a+offset);}
            private void Tri(int a,int b,int c){triangles.Add(a);triangles.Add(b);triangles.Add(c);}
            public void Ribbon(Vector3[] points,float width,float thickness) => Loft(points,width,thickness,8);
            public void Tube(Vector3[] points,float radius,int sides)=>Loft(points,radius,radius,sides);
            private void Loft(Vector3[] points,float width,float depth,int sides)
            {
                int start=v.Count;
                for(int r=0;r<points.Length;r++) {
                    var f=(points[Math.Min(r+1,points.Length-1)]-points[Math.Max(0,r-1)]).normalized;
                    var a=Vector3.Cross(Vector3.up,f).normalized;if(a.sqrMagnitude<.01f)a=Vector3.forward;var b=Vector3.Cross(f,a).normalized;
                    for(int j=0;j<sides;j++){float t=j*Mathf.PI*2/sides;v.Add(points[r]+a*(Mathf.Cos(t)*width)+b*(Mathf.Sin(t)*depth));uv.Add(new Vector2(j/(float)sides,r*.18f));}
                    if(r==0)continue;
                    for(int j=0;j<sides;j++){int p=start+(r-1)*sides+j,q=start+(r-1)*sides+(j+1)%sides;Tri(p,q,p+sides);Tri(q,q+sides,p+sides);}
                }
            }
            public Mesh Mesh(){var m=new Mesh();m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(triangles,0);m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();return m;}
        }
    }
}
