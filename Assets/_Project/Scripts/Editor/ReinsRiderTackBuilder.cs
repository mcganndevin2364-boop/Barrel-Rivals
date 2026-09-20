using System;
using System.Collections.Generic;
using System.IO;
using BarrelRivals.Practice;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace BarrelRivals.Editor
{
    /// <summary>Original modeled foreground tack study. No colliders or gameplay authority.</summary>
    public static class ReinsRiderTackBuilder
    {
        public const string Root = "Assets/_Project/Art/Reins/Premium/RiderTack";

        public static void Build(Transform horse)
        {
            var model = horse.Find("Reference horse");
            if (!model) throw new InvalidOperationException("Build reference horse before rider tack.");
            var source = horse.GetComponent<ReinsHorsePresentation>();
            Transform head = null;
            foreach (var bone in model.GetComponentsInChildren<Transform>(true))
                if (bone.name == "Bone.002") { head = bone; break; }
            if (!head || !source) throw new InvalidOperationException("Reference horse head/presentation is missing.");
            var prior = model.Find("Rider tack"); if (prior) Object.DestroyImmediate(prior.gameObject);
            // Bridle follows the head, so find/remove its earlier generated child independently.
            var oldBridle = head.Find("Western bridle"); if (oldBridle) Object.DestroyImmediate(oldBridle.gameObject);
            Directory.CreateDirectory(Root); AssetDatabase.Refresh();
            var leather = Leather("Oiled bridle leather", new Color(.30f,.20f,.14f));
            var glove = Leather("Worn chestnut gloves", new Color(.85f,.72f,.56f));
            var palm = Leather("Suede palm grip", new Color(.60f,.50f,.40f));
            var sleeve = Material("Charcoal denim sleeves", new Color(.034f,.047f,.054f), .12f);
            var stitch = Material("Waxed saddle stitching", new Color(.58f,.40f,.19f), .22f);
            var silver = Material("Brushed bit steel", new Color(.48f,.51f,.52f), .62f, .85f);
            var teal = Material("Turquoise rein braid", new Color(.035f,.34f,.31f), .31f);
            var cream = Material("Flax rein braid", new Color(.65f,.49f,.28f), .28f);
            var rig = Child(model, "Rider tack");
            var left = BuildHand(rig, -1, glove, palm, sleeve, stitch);
            var right = BuildHand(rig, 1, glove, palm, sleeve, stitch);
            var leftGrip = Child(left, "Closed left grip"); leftGrip.localPosition = new Vector3(.006f,-.023f,.137f);
            var rightGrip = Child(right, "Closed right grip"); rightGrip.localPosition = new Vector3(-.006f,-.023f,.137f);
            Transform leftBit, rightBit;
            BuildBridle(model, head, leather, silver, stitch, out leftBit, out rightBit);
            var reinMesh = SaveMesh("Braided rein", ReinsRiderTackPresentation.CreateReinTemplate());
            var materials = new[] { leather, teal, cream };
            var leftRein = MeshObject(rig, "Left braided rein", reinMesh, materials);
            var rightRein = MeshObject(rig, "Right braided rein", reinMesh, materials);
            var presenter = rig.gameObject.AddComponent<ReinsRiderTackPresentation>();
            presenter.Configure(source, left, right, leftGrip, rightGrip, leftBit, rightBit, leftRein, rightRein);
            // Persist each rein in its authored ready pose. The runtime clones these assets before deformation.
            var leftPosed = PoseRein(rig, leftGrip, leftBit, -1);
            var rightPosed = PoseRein(rig, rightGrip, rightBit, 1);
            leftRein.sharedMesh = SaveMesh("Left rein ready pose", leftPosed);
            rightRein.sharedMesh = SaveMesh("Right rein ready pose", rightPosed);
            AssetDatabase.SaveAssets();
        }

        private static Mesh PoseRein(Transform space, Transform grip, Transform bit, int side)
        {
            // Reuse the runtime's exact tube topology and build-time curve; no scene-only transient mesh.
            var mesh = ReinsRiderTackPresentation.CreateReinTemplate();
            var start = space.InverseTransformPoint(grip.position); var end = space.InverseTransformPoint(bit.position);
            var points = new Vector3[ReinsRiderTackPresentation.Segments + 1];
            var a = Vector3.Lerp(start,end,.3f) + new Vector3(side*.045f,-.17f,0);
            var b = Vector3.Lerp(start,end,.72f) + new Vector3(side*.025f,-.085f,0);
            for (int i=0;i<points.Length;i++) { float t=i/(float)(points.Length-1),q=1-t;points[i]=q*q*q*start+3*q*q*t*a+3*q*t*t*b+t*t*t*end; }
            var vertices=mesh.vertices;var normals=mesh.normals;
            WriteReinSurface(vertices,normals,points,0,8,0,.0085f,0);
            WriteReinSurface(vertices,normals,points,points.Length*8,5,.0084f,.0022f,0);
            WriteReinSurface(vertices,normals,points,points.Length*13,5,.0084f,.0022f,Mathf.PI);
            mesh.vertices=vertices;mesh.normals=normals;mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
        }

        private static void WriteReinSurface(Vector3[] vertices,Vector3[] normals,Vector3[] centers,int offset,int sides,float orbit,float radius,float phase)
        {
            for(int r=0;r<centers.Length;r++)
            {
                var forward=(centers[Mathf.Min(r+1,centers.Length-1)]-centers[Mathf.Max(0,r-1)]).normalized;
                var across=Vector3.Cross(Vector3.up,forward).normalized;var up=Vector3.Cross(forward,across).normalized;
                float braid=r/(float)(centers.Length-1)*Mathf.PI*28+phase;
                var center=centers[r]+(across*Mathf.Cos(braid)+up*Mathf.Sin(braid))*orbit;
                for(int j=0;j<sides;j++){float angle=j*Mathf.PI*2/sides;var n=across*Mathf.Cos(angle)+up*Mathf.Sin(angle);int i=offset+r*sides+j;vertices[i]=center+n*radius;normals[i]=n;}
            }
        }

        private static Transform BuildHand(Transform parent, int side, Material glove, Material palm, Material sleeve, Material thread)
        {
            var hand=Child(parent,side<0?"Left rider grip":"Right rider grip");
            hand.localPosition=new Vector3(side*.41f,1.84f,.28f);
            hand.localRotation=Quaternion.Euler(0,side*8,side*9);
            var shell=new Surface();var suede=new Surface();var clothing=new Surface();var stitching=new Surface();
            // A palm with narrower wrist, broad metacarpals and tapered knuckle plane, not a scaled primitive.
            shell.Loft(new[]{new Vector3(0,0,-.035f),new Vector3(0,0,0),new Vector3(0,.005f,.045f),new Vector3(0,.006f,.09f),new Vector3(0,0,.121f)},
                new[]{new Vector2(.037f,.027f),new Vector2(.042f,.032f),new Vector2(.054f,.035f),new Vector2(.053f,.036f),new Vector2(.045f,.029f)},18,true);
            // Four differently sized flexed fingers wrap the rein; the thumb opposes from the inner side.
            for(int finger=0;finger<4;finger++)
            {
                float x=(finger-1.5f)*.024f, length=finger==0?.043f:finger==3?.033f:.05f;
                float radius=finger==3?.0105f:.012f;
                var path=new[]{new Vector3(x,.003f,.108f),new Vector3(x,.013f,.125f),new Vector3(x,.008f,.14f+length*.4f),new Vector3(x,-.011f,.155f+length*.25f),new Vector3(x,-.037f,.15f),new Vector3(x,-.046f,.125f)};
                shell.Tube(path,new[]{radius*1.06f,radius*1.05f,radius,radius*.95f,radius*.86f,radius*.62f},10,true);
                // Raised stitched knuckle panels and seams stay shallow to avoid toy-like spheres.
                var ridge=new[]{new Vector3(x-.008f,.024f,.124f),new Vector3(x,.027f,.131f),new Vector3(x+.008f,.024f,.124f)};
                suede.Tube(ridge,.0024f,6,true);
            }
            float inner=-side;
            shell.Tube(new[]{new Vector3(inner*.043f,-.006f,.031f),new Vector3(inner*.063f,-.006f,.061f),new Vector3(inner*.058f,-.019f,.09f),new Vector3(inner*.034f,-.024f,.118f),new Vector3(inner*.007f,-.021f,.132f)},
                new[]{.024f,.021f,.019f,.017f,.011f},12,true);
            // Saddle glove cuff, a fitted forearm sleeve and a low-contrast palm wear insert.
            suede.Loft(new[]{new Vector3(0,0,-.051f),new Vector3(0,0,-.033f),new Vector3(0,0,-.014f)},
                new[]{new Vector2(.043f,.032f),new Vector2(.046f,.034f),new Vector2(.041f,.031f)},18,true);
            clothing.Loft(new[]{new Vector3(side*.10f,-.14f,-.38f),new Vector3(side*.063f,-.065f,-.23f),new Vector3(side*.023f,-.019f,-.10f),new Vector3(0,0,-.034f)},
                new[]{new Vector2(.077f,.058f),new Vector2(.071f,.053f),new Vector2(.052f,.042f),new Vector2(.039f,.029f)},18,true);
            suede.Loft(new[]{new Vector3(0,-.024f,.021f),new Vector3(0,-.026f,.05f),new Vector3(0,-.026f,.091f)},
                new[]{new Vector2(.030f,.008f),new Vector2(.043f,.010f),new Vector2(.036f,.007f)},14,true);
            // Sparse actual thread stitches along the back/cuff, submillimetre radius.
            for(int edge=-1;edge<=1;edge+=2)for(int s=0;s<9;s++)
            {
                float z=.014f+s*.009f,x=edge*(.040f+Mathf.Sin(s/8f*Mathf.PI)*.007f);
                stitching.Tube(new[]{new Vector3(x,.022f,z),new Vector3(x,.023f,z+.0045f)},.0009f,4,true);
            }
            var sideLabel=side<0?"Left":"Right";
            MeshObject(hand,"Glove shell",SaveMesh(sideLabel+" glove shell",shell.Mesh()),new[]{glove});
            MeshObject(hand,"Glove grip panels",SaveMesh(sideLabel+" glove grip",suede.Mesh()),new[]{palm});
            MeshObject(hand,"Denim forearm",SaveMesh(sideLabel+" sleeve",clothing.Mesh()),new[]{sleeve});
            MeshObject(hand,"Glove stitching",SaveMesh(sideLabel+" glove stitching",stitching.Mesh()),new[]{thread});
            return hand;
        }

        private static void BuildBridle(Transform model,Transform head,Material leather,Material steel,Material thread,out Transform leftBit,out Transform rightBit)
        {
            var bridle=Child(model,"Western bridle");var hide=new Surface();var hardware=new Surface();var seams=new Surface();
            // Authored in the normalized reference horse's metre space, then attached in head bind pose.
            hide.Strap(new[]{new Vector3(-.145f,1.95f,1.58f),new Vector3(-.17f,2.00f,1.53f),new Vector3(0,2.02f,1.50f),new Vector3(.17f,2.00f,1.53f),new Vector3(.145f,1.95f,1.58f)},.025f,.004f);
            hide.Strap(new[]{new Vector3(-.17f,1.95f,1.64f),new Vector3(0,1.99f,1.70f),new Vector3(.17f,1.95f,1.64f)},.021f,.004f);
            for(int side=-1;side<=1;side+=2)
            {
                hide.Strap(new[]{new Vector3(side*.145f,1.95f,1.58f),new Vector3(side*.182f,1.96f,1.77f),new Vector3(side*.145f,1.69f,2.08f)},.023f,.004f);
                for(int s=0;s<15;s++)
                {
                    float t=s/15f;var a=Vector3.Lerp(new Vector3(side*.187f,1.98f,1.74f),new Vector3(side*.149f,1.71f,2.06f),t);
                    seams.Tube(new[]{a,a+new Vector3(-side*.0008f,-.007f,.008f)},.0009f,4,true);
                }
                var ring=Ellipse(new Vector3(side*.151f,1.69f,2.10f),Vector3.up*.032f,Vector3.forward*.031f,22);
                hardware.Tube(ring,.005f,8,false);
                // Small square cheek buckle with rounded corners, using the same original tube construction.
                var buckle=Ellipse(new Vector3(side*.186f,1.94f,1.82f),Vector3.up*.023f,Vector3.forward*.017f,16);
                hardware.Tube(buckle,.0025f,6,false);
            }
            var nose=Ellipse(new Vector3(0,1.72f,2.075f),Vector3.right*.148f,new Vector3(0,.095f,-.045f),28);
            hide.Strap(nose,.031f,.0045f);
            hardware.Tube(new[]{new Vector3(-.153f,1.685f,2.105f),new Vector3(0,1.67f,2.09f),new Vector3(.153f,1.685f,2.105f)},.007f,8,true);
            MeshObject(bridle,"Fitted leather headstall",SaveMesh("Western leather headstall",hide.Mesh()),new[]{leather});
            MeshObject(bridle,"Bit rings and buckles",SaveMesh("Bridle metal fittings",hardware.Mesh()),new[]{steel});
            MeshObject(bridle,"Headstall stitching",SaveMesh("Headstall stitches",seams.Mesh()),new[]{thread});
            leftBit=Child(bridle,"Left bit anchor");leftBit.localPosition=new Vector3(-.177f,1.69f,2.10f);
            rightBit=Child(bridle,"Right bit anchor");rightBit.localPosition=new Vector3(.177f,1.69f,2.10f);
            bridle.SetParent(head,true);
        }

        private static Vector3[] Ellipse(Vector3 center,Vector3 a,Vector3 b,int count)
        {var points=new Vector3[count+1];for(int i=0;i<=count;i++){float t=i*Mathf.PI*2/count;points[i]=center+Mathf.Cos(t)*a+Mathf.Sin(t)*b;}return points;}
        private static Transform Child(Transform parent,string name)
        {var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}
        private static MeshFilter MeshObject(Transform parent,string name,Mesh mesh,Material[] materials)
        {var t=Child(parent,name);var filter=t.gameObject.AddComponent<MeshFilter>();filter.sharedMesh=mesh;var renderer=t.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterials=materials;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;return filter;}
        private static Material Material(string name,Color color,float smoothness,float metallic=0)
        {string path=Root+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m)return m;m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smoothness);m.SetFloat("_Metallic",metallic);AssetDatabase.CreateAsset(m,path);return m;}
        private static Material Leather(string name, Color tint)
            => ReinsPremiumArenaBuilder.Pbr(name,"Leather_Albedo_1K.jpg","Leather_NormalGL_1K.png","Leather_Roughness_1K.jpg",null,tint,0);
        private static Mesh SaveMesh(string name,Mesh mesh)
        {mesh.name=name;return PersistentMeshAsset.Save(mesh,Root+"/"+name+".asset");}

        private sealed class Surface
        {
            private readonly List<Vector3> vertices=new List<Vector3>();private readonly List<Vector2> uv=new List<Vector2>();private readonly List<int> triangles=new List<int>();
            public void Tube(Vector3[] points,float radius,int sides,bool cap)=>Tube(points,Array.ConvertAll(points,p=>radius),sides,cap);
            public void Tube(Vector3[] points,float[] radius,int sides,bool cap)=>Loft(points,Array.ConvertAll(radius,r=>new Vector2(r,r)),sides,cap);
            public void Strap(Vector3[] points,float width,float depth)=>Loft(points,Array.ConvertAll(points,p=>new Vector2(width*.5f,depth)),8,false);
            public void Loft(Vector3[] points,Vector2[] radius,int sides,bool cap)
            {
                int first=vertices.Count;
                for(int r=0;r<points.Length;r++)
                {
                    var tangent=(points[Mathf.Min(r+1,points.Length-1)]-points[Mathf.Max(0,r-1)]).normalized;
                    var across=Vector3.Cross(Vector3.up,tangent).normalized;if(across.sqrMagnitude<.001f)across=Vector3.right;
                    var up=Vector3.Cross(tangent,across).normalized;
                    for(int j=0;j<sides;j++){float a=j*Mathf.PI*2/sides;vertices.Add(points[r]+across*(Mathf.Cos(a)*radius[r].x)+up*(Mathf.Sin(a)*radius[r].y));uv.Add(new Vector2(j/(float)sides,r/(float)(points.Length-1)));}
                    if(r==0)continue;
                    for(int j=0;j<sides;j++){int a=first+(r-1)*sides+j,b=first+(r-1)*sides+(j+1)%sides,c=a+sides,d=b+sides;triangles.Add(a);triangles.Add(b);triangles.Add(c);triangles.Add(b);triangles.Add(d);triangles.Add(c);}
                }
                if(cap)
                {
                    int near=vertices.Count;vertices.Add(points[0]);uv.Add(Vector2.zero);int far=vertices.Count;vertices.Add(points[points.Length-1]);uv.Add(Vector2.one);
                    for(int j=0;j<sides;j++){triangles.Add(near);triangles.Add(first+(j+1)%sides);triangles.Add(first+j);int a=first+(points.Length-1)*sides+j,b=first+(points.Length-1)*sides+(j+1)%sides;triangles.Add(far);triangles.Add(a);triangles.Add(b);}
                }
            }
            public Mesh Mesh(){var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;}
        }
    }
}
