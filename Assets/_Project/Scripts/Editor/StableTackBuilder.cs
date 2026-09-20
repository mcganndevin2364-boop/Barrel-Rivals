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
            var fit=new NeutralBack(model);
            var pad=new Shape();var trim=new Shape();var leather=new Shape();var fittings=new Shape();
            // Author around the neutral body center. The live imported-root rotation has a
            // different forward origin from standalone FBX audits, so measure that offset.
            var forwardOffset=Vector3.forward*fit.ForwardOrigin;
            Vector3 Blanket(float u,float v,float gap)
            {
                float corner=Mathf.Pow(Mathf.Abs(u),6);
                float z=Mathf.Lerp(-.805f+corner*.065f,.025f-corner*.055f,v);
                float angle=u*1.25f*(.91f+.09f*Mathf.Sin(v*Mathf.PI));
                return fit.Wrap(angle,z,gap);
            }
            pad.Panel((u,v)=>Blanket(u,v,.030f),(u,v)=>Blanket(u,v,.013f),20,16);
            var padRenderer=Part(root,"Woven saddle pad",Save("Curved western pad",pad.Mesh(forwardOffset)),desert);
            // Narrow woven border and two quiet accent bands; no billboard-like striped overhang.
            foreach(float v in new[]{.055f,.945f})
                trim.Panel((u,t)=>Blanket(u*.94f,v+(t-.5f)*.012f,.032f),null,24,1);
            for(int side=-1;side<=1;side+=2)
                trim.Tube(Samples(25,t=>Blanket(side*.955f,Mathf.Lerp(.045f,.955f,t),.033f)),.0022f,5);
            Vector3 Skirt(float u,float v,float gap)
            {
                float corner=Mathf.Pow(Mathf.Abs(u),4);
                float z=Mathf.Lerp(-.740f+corner*.075f,-.050f-corner*.075f,v);
                return fit.Wrap(u*1.105f*(.88f+.12f*Mathf.Sin(v*Mathf.PI)),z,gap);
            }
            leather.Panel((u,v)=>Skirt(u,v,.055f),(u,v)=>Skirt(u,v,.039f),20,14);
            for(int side=-1;side<=1;side+=2)
                trim.Tube(Samples(25,t=>Skirt(side*.945f,Mathf.Lerp(.035f,.965f,t),.0575f)),.0017f,5);
            // The seat is a shallow rounded bowl, separate from the curved cantle and front swell.
            Vector3 Seat(float u,float v)
            {
                float z=Mathf.Lerp(-.670f,-.150f,v),halfWidth=.155f+.033f*Mathf.Sin(v*Mathf.PI);
                float y=fit.Wrap(0,z,0).y+.062f+.027f*u*u+.019f*Mathf.Pow(v*2-1,2);
                return new Vector3(u*halfWidth,y,z);
            }
            leather.Panel(Seat,(u,v)=>Seat(u,v)-Vector3.up*.026f,18,12);
            Vector3 Cantle(float u,float v)
            {
                var lower=new Vector3(u*.177f,fit.Wrap(0,-.630f,0).y+.075f+.020f*u*u,-.630f+.035f*u*u);
                var upper=new Vector3(u*.225f,1.922f+.093f*(1-u*u),-.700f+.062f*u*u);
                return Vector3.Lerp(lower,upper,v)+Vector3.back*(.022f*Mathf.Sin(v*Mathf.PI));
            }
            leather.Panel(Cantle,(u,v)=>Cantle(u,v)+new Vector3(0,-.003f,.030f),20,6);
            leather.Tube(Samples(31,t=>Cantle(t*2-1,1)+Vector3.forward*.009f),.012f,8);
            trim.Tube(Samples(29,t=>Cantle((t*2-1)*.93f,.87f)-Vector3.forward*.002f),.0018f,5);
            leather.Ellipsoid(new Vector3(0,1.885f,-.133f),new Vector3(.226f,.083f,.079f),20,10);
            // Short wrapped horn with a flattened cap, rather than a tall cylindrical handle.
            leather.Tube(Samples(9,t=>new Vector3(0,Mathf.Lerp(1.935f,2.033f,t),Mathf.Lerp(-.131f,-.116f,t))),.024f,12);
            leather.Ellipsoid(new Vector3(0,2.035f,-.115f),new Vector3(.052f,.016f,.047f),16,8);
            for(int side=-1;side<=1;side+=2)
            {
                Vector3 Fender(float u,float v)
                {
                    float y=Mathf.Lerp(1.635f,1.025f,v),z=-.413f+.035f*v;
                    float halfWidth=Mathf.Lerp(.083f,.035f,v)+.023f*Mathf.Sin(v*Mathf.PI);
                    float x=side*(Mathf.Lerp(.31f,.425f,Mathf.SmoothStep(0,1,v/.35f))+.005f*Mathf.Sin(v*Mathf.PI));
                    return new Vector3(x,y,z-side*u*halfWidth);
                }
                leather.Panel(Fender,(u,v)=>Fender(u,v)+Vector3.left*(side*.015f),8,12);
                foreach(int edge in new[]{-1,1})
                    trim.Tube(Samples(19,t=>Fender(edge*.79f,Mathf.Lerp(.035f,.97f,t))+Vector3.right*(side*.002f)),.0016f,5);
                // Rounded stirrup sides and a flat lower tread leave a real opening for the boot.
                var loop=Samples(33,t=>{
                    float angle=t*Mathf.PI*2;
                    return new Vector3(side*.423f,Mathf.Max(.818f,.935f+.120f*Mathf.Cos(angle)),-.377f+.091f*Mathf.Sin(angle));
                });
                leather.Tube(loop,.016f,8);
                fittings.Tube(Samples(8,t=>new Vector3(side*.423f,.817f,Mathf.Lerp(-.448f,-.306f,t))),.014f,8);
                // Rigging D-ring and short latigo ties sit beside the skirt, not at the horse's spine.
                var ringCenter=Skirt(side,.58f,.072f);
                fittings.Tube(Samples(25,t=>ringCenter+new Vector3(0,Mathf.Cos(t*Mathf.PI*2)*.038f,Mathf.Sin(t*Mathf.PI*2)*.030f)),.0055f,8);
                leather.Tube(Samples(7,t=>ringCenter+new Vector3(side*.008f,-.19f*t,.035f*t*t)),.009f,7);
            }
            // The woven cinch wraps under measured belly geometry; it is not a floating straight strap.
            trim.Panel((u,v)=>fit.Wrap(Mathf.Lerp(1.15f,Mathf.PI*2-1.15f,v),-.353f+u*.039f,.025f),
                (u,v)=>fit.Wrap(Mathf.Lerp(1.15f,Mathf.PI*2-1.15f,v),-.353f+u*.039f,.013f),4,30);
            Part(root,"Woven blanket stripes",Save("Blanket stripes",trim.Mesh(forwardOffset)),thread);
            var saddleRenderer=Part(root,"Contoured western leather",Save("Western saddle leather",leather.Mesh(forwardOffset)),ranch);
            Part(root,"Saddle hardware",Save("Saddle fittings",fittings.Mesh(forwardOffset)),steel);
            var horn=new GameObject("Saddle horn anchor").transform;horn.SetParent(root,false);
            horn.localPosition=new Vector3(0,2.039f,-.115f)+forwardOffset;
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
            var nodes=model.GetComponentsInChildren<Transform>(true);
            var horn=nodes.Single(t=>t.name=="Saddle horn anchor");
            var fit=new NeutralBack(model);
            foreach(var filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if(filter.name!="Left braided rein" && filter.name!="Right braided rein")continue;
                int side=filter.name.StartsWith("Left")?-1:1;
                var bit=nodes.Single(t=>t.name==(side<0?"Left bit anchor":"Right bit anchor"));
                var start=model.InverseTransformPoint(horn.TransformPoint(new Vector3(side*.040f,0,0)));
                var end=model.InverseTransformPoint(bit.position);
                var cord=new Shape();var points=new Vector3[41];var clearance=new float[points.Length];
                for(int i=0;i<points.Length;i++) {
                    float t=i/40f;var point=Vector3.Lerp(start,end,t)+new Vector3(side*.08f*Mathf.Sin(t*Mathf.PI),-.33f*Mathf.Sin(t*Mathf.PI),0);
                    // Fit both sides independently in the same neutral, baked metre space.
                    // A 28 mm centre offset leaves 17 mm clearance around the 11 mm cord.
                    clearance[i]=side*point.x;
                    if(i>0 && i<points.Length-1 && fit.TrySide(point.y,point.z,side,out float surfaceX))
                        clearance[i]=Mathf.Max(clearance[i],side*surfaceX+.028f);
                    point.x=side*clearance[i];points[i]=point;
                }
                // Relax the drape outside its body constraints. Endpoints never move; this
                // rounds the approach to the neck instead of making an angular clipped polyline.
                for(int pass=0;pass<80;pass++)
                    for(int i=1;i<points.Length-1;i++) {
                        var point=points[i];
                        point.x=side*Mathf.Max(clearance[i],(side*points[i-1].x+side*points[i+1].x)*.5f);
                        points[i]=point;
                    }
                for(int i=0;i<points.Length;i++)
                    points[i]=filter.transform.InverseTransformPoint(model.TransformPoint(points[i]));
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
        private static Vector3[] Samples(int count,Func<float,Vector3> point)
        {var points=new Vector3[count];for(int i=0;i<count;i++)points[i]=point(i/(float)(count-1));return points;}
        private sealed class NeutralBack
        {
            public float ForwardOrigin { get; }
            private readonly Vector3[] vertices;
            private readonly int[] triangles;
            private readonly Dictionary<Vector2,Vector3> cache=new Dictionary<Vector2,Vector3>();
            public NeutralBack(Transform model)
            {
                var body=model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r=>r.name.StartsWith("HorseBody",StringComparison.Ordinal));
                var mesh=new Mesh();
                try
                {
                    // ReinsReferenceArtBuilder explicitly samples neutral Idle before fitting tack.
                    // Match the verified Unity6.6 gait geometry path: BakeMesh(true) compensates
                    // this legacy renderer's unusual imported scale before the hierarchy is applied once.
                    body.BakeMesh(mesh,true);vertices=mesh.vertices;triangles=mesh.triangles;
                    var transform=model.worldToLocalMatrix*body.transform.localToWorldMatrix;
                    for(int i=0;i<vertices.Length;i++)vertices[i]=transform.MultiplyPoint3x4(vertices[i]);
                    var bounds=new Bounds(vertices[0],Vector3.zero);
                    foreach(var point in vertices)bounds.Encapsulate(point);
                    if(Mathf.Abs(bounds.size.y-2.12f)>.02f)
                        throw new InvalidOperationException("Saddle fitting must use neutral metre-space body geometry; measured height="+bounds.size.y);
                    ForwardOrigin=bounds.center.z;
                    Debug.Log("BARREL_SADDLE_FIT: neutral body Reference-horse bounds min="+bounds.min.ToString("F4")+", max="+bounds.max.ToString("F4"));
                }
                finally{Object.DestroyImmediate(mesh);}
            }
            public Vector3 Wrap(float angle,float z,float gap)
            {
                var key=new Vector2(angle,z);var direction=new Vector3(Mathf.Sin(angle),Mathf.Cos(angle),0);
                if(!cache.TryGetValue(key,out var hit))
                {
                    var origin=new Vector3(0,1.35f,z+ForwardOrigin);float nearest=float.PositiveInfinity;
                    for(int i=0;i<triangles.Length;i+=3)
                    {
                        var a=vertices[triangles[i]];var edge1=vertices[triangles[i+1]]-a;var edge2=vertices[triangles[i+2]]-a;
                        var cross=Vector3.Cross(direction,edge2);float determinant=Vector3.Dot(edge1,cross);
                        if(Mathf.Abs(determinant)<.0000001f)continue;
                        float inverse=1/determinant;var from=origin-a;
                        float u=Vector3.Dot(from,cross)*inverse;if(u<0 || u>1)continue;
                        var q=Vector3.Cross(from,edge1);float v=Vector3.Dot(direction,q)*inverse;
                        if(v<0 || u+v>1)continue;
                        float distance=Vector3.Dot(edge2,q)*inverse;
                        if(distance>.0001f && distance<nearest)nearest=distance;
                    }
                    if(float.IsInfinity(nearest) || nearest>1.0f)
                        throw new InvalidOperationException("Saddle fit missed the neutral torso at z="+z+", angle="+angle);
                    hit=origin+direction*nearest;cache.Add(key,hit);
                }
                return hit-Vector3.forward*ForwardOrigin+direction*gap;
            }
            public bool TrySide(float y,float z,int side,out float x)
            {
                // Cast from outside toward the actual signed body flank. Unlike a centre
                // ray this also handles an offset/asymmetric head cross-section correctly.
                var origin=new Vector3(side*2f,y,z);var direction=Vector3.left*side;
                float nearest=float.PositiveInfinity;
                for(int i=0;i<triangles.Length;i+=3)
                {
                    var a=vertices[triangles[i]];var edge1=vertices[triangles[i+1]]-a;var edge2=vertices[triangles[i+2]]-a;
                    var cross=Vector3.Cross(direction,edge2);float determinant=Vector3.Dot(edge1,cross);
                    if(Mathf.Abs(determinant)<.0000001f)continue;
                    float inverse=1/determinant;var from=origin-a;
                    float u=Vector3.Dot(from,cross)*inverse;if(u<0 || u>1)continue;
                    var q=Vector3.Cross(from,edge1);float v=Vector3.Dot(direction,q)*inverse;
                    if(v<0 || u+v>1)continue;
                    float distance=Vector3.Dot(edge2,q)*inverse;
                    if(distance>=0 && distance<nearest)nearest=distance;
                }
                x=0;if(float.IsInfinity(nearest))return false;
                x=origin.x+direction.x*nearest;return true;
            }
        }
        private sealed class Shape
        {
            private readonly List<Vector3> v=new List<Vector3>();private readonly List<Vector2> uv=new List<Vector2>();private readonly List<int> triangles=new List<int>();
            public void Panel(Func<float,float,Vector3> top,Func<float,float,Vector3> bottom,int nu,int nv)
            {
                int start=v.Count,layers=bottom==null?1:2,count=(nu+1)*(nv+1);
                for(int layer=0;layer<layers;layer++)for(int y=0;y<=nv;y++)for(int x=0;x<=nu;x++)
                {
                    float u=x/(float)nu*2-1,w=y/(float)nv;
                    var point=layer==0?top(u,w):bottom(u,w);v.Add(point);
                    uv.Add(new Vector2(x/(float)nu*2,y/(float)nv*2));
                }
                for(int layer=0;layer<layers;layer++)for(int y=0;y<nv;y++)for(int x=0;x<nu;x++)
                {
                    int a=start+layer*count+y*(nu+1)+x,b=a+1,c=a+nu+1,d=c+1;
                    if(layer==0){Tri(a,c,b);Tri(b,c,d);}else{Tri(a,b,c);Tri(b,d,c);}
                }
                if(layers==1)return;
                for(int x=0;x<nu;x++){Edge(start+x,start+x+1,count);Edge(start+nv*(nu+1)+x+1,start+nv*(nu+1)+x,count);}
                for(int y=0;y<nv;y++){Edge(start+(y+1)*(nu+1),start+y*(nu+1),count);Edge(start+y*(nu+1)+nu,start+(y+1)*(nu+1)+nu,count);}
            }
            public void Ellipsoid(Vector3 center,Vector3 radius,int longitude,int latitude)
            {
                int start=v.Count;
                for(int y=0;y<=latitude;y++)for(int x=0;x<=longitude;x++)
                {
                    float a=x*Mathf.PI*2/longitude,b=y*Mathf.PI/latitude;
                    v.Add(center+Vector3.Scale(radius,new Vector3(Mathf.Sin(b)*Mathf.Cos(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a))));
                    uv.Add(new Vector2(x/(float)longitude,y/(float)latitude));
                }
                for(int y=0;y<latitude;y++)for(int x=0;x<longitude;x++)
                {int a=start+y*(longitude+1)+x,b=a+1,c=a+longitude+1,d=c+1;Tri(a,b,c);Tri(b,d,c);}
            }
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
            public Mesh Mesh(Vector3 offset=default){var m=new Mesh();m.SetVertices(offset==Vector3.zero?v:v.ConvertAll(p=>p+offset));m.SetUVs(0,uv);m.SetTriangles(triangles,0);m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();return m;}
        }
    }
}
