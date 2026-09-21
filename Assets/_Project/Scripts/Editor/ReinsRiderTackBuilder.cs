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
            var sleeve = Material("Charcoal denim sleeves", new Color(.034f,.047f,.054f), .12f);
            var stitch = Material("Waxed saddle stitching", new Color(.58f,.40f,.19f), .22f);
            var silver = Material("Brushed bit steel", new Color(.48f,.51f,.52f), .62f, .85f);
            var teal = Material("Turquoise rein braid", new Color(.035f,.34f,.31f), .31f);
            var cream = Material("Flax rein braid", new Color(.65f,.49f,.28f), .28f);
            var fixedHand = FixedHandMaterial(sleeve, stitch);
            var rig = Child(model, "Rider tack");
            var left = BuildHand(rig, -1, glove, fixedHand);
            var right = BuildHand(rig, 1, glove, fixedHand);
            var leftGrip = Child(left, "Closed left grip"); leftGrip.localPosition = new Vector3(-.010f,-.031f,.075f);
            var rightGrip = Child(right, "Closed right grip"); rightGrip.localPosition = new Vector3(.010f,-.031f,.075f);
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

        private static Transform BuildHand(Transform parent, int side, Material glove, Material fixedParts)
        {
            var hand=Child(parent,side<0?"Left rider grip":"Right rider grip");
            hand.localPosition=new Vector3(side*.41f,1.84f,.28f);
            hand.localRotation=Quaternion.Euler(0,side*8,side*9);
            var clothing=new Surface();var stitching=new Surface();
            // Retain the original fixed sleeve/seam fallback; the full rider hides
            // it and supplies its own skinned shirt. Glove dyes remain independent.
            clothing.Loft(new[]{new Vector3(side*.10f,-.14f,-.38f),new Vector3(side*.063f,-.065f,-.23f),new Vector3(side*.023f,-.019f,-.10f),new Vector3(0,0,-.034f)},
                new[]{new Vector2(.077f,.058f),new Vector2(.071f,.053f),new Vector2(.052f,.042f),new Vector2(.039f,.029f)},18,true);
            // Sparse actual thread stitches along the back/cuff, submillimetre radius.
            for(int edge=-1;edge<=1;edge+=2)for(int s=0;s<9;s++)
            {
                float z=.014f+s*.009f,x=edge*(.040f+Mathf.Sin(s/8f*Mathf.PI)*.007f);
                stitching.Tube(new[]{new Vector3(x,.022f,z),new Vector3(x,.023f,z+.0045f)},.0009f,4,true);
            }
            var sideLabel=side<0?"Left":"Right";
            // One connected hand-derived shell replaces the disconnected palm,
            // finger tubes and thumb pieces. Right-hand mirroring preserves winding.
            MeshObject(hand,"Glove shell",SaveMesh(sideLabel+" glove shell",ReinsGloveBuilder.Riding(side)),new[]{glove});
            // Fixed sleeve/thread colors must not inherit glove dyes. Two constant swatches
            // share one opaque material; the open inspection glove remains a separate build.
            MeshObject(hand,"Sleeve and glove stitching",SaveMesh(sideLabel+" sleeve and stitching",JoinHandSurfaces(clothing.Mesh(),stitching.Mesh(),true)),new[]{fixedParts});
            return hand;
        }

        private static Mesh JoinHandSurfaces(Mesh first,Mesh second,bool fixedSwatches)
        {
            var combined=new Mesh();
            try
            {
                if(fixedSwatches)
                {
                    // Each disconnected part samples a texel centre. No UV interpolation
                    // crosses between colors, and this untextured material needs no normal map.
                    first.uv=ConstantUv(first.vertexCount,new Vector2(.25f,.5f));
                    second.uv=ConstantUv(second.vertexCount,new Vector2(.75f,.5f));
                }
                combined.CombineMeshes(new[]{new CombineInstance{mesh=first},new CombineInstance{mesh=second}},true,false);
                combined.RecalculateBounds();
                return combined;
            }
            catch { Object.DestroyImmediate(combined);throw; }
            finally { Object.DestroyImmediate(first);Object.DestroyImmediate(second); }
        }

        private static Vector2[] ConstantUv(int count,Vector2 value)
        {var uv=new Vector2[count];for(int i=0;i<count;i++)uv[i]=value;return uv;}

        private static Material FixedHandMaterial(Material sleeve,Material thread)
        {
            // These source materials are constant opaque colors. Fail rather than silently
            // discarding future authored maps if that contract changes.
            foreach(var source in new[]{sleeve,thread})
                foreach(var property in new[]{"_BaseMap","_BumpMap","_MetallicGlossMap"})
                    if(source.GetTexture(property))throw new InvalidOperationException("Fixed hand swatches require untextured source material: "+source.name);
            var albedo=SaveHandSwatches("Fixed hand color swatches",new[]{sleeve.GetColor("_BaseColor"),thread.GetColor("_BaseColor")});
            var surface=SaveHandSwatches("Fixed hand surface swatches",new[]{
                new Color(sleeve.GetFloat("_Metallic"),0,0,sleeve.GetFloat("_Smoothness")),
                new Color(thread.GetFloat("_Metallic"),0,0,thread.GetFloat("_Smoothness"))});
            string path=Root+"/Fixed sleeve and glove stitching.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Fixed sleeve and glove stitching"};AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",Color.white);material.SetTexture("_BaseMap",albedo);
            material.SetTextureScale("_BaseMap",Vector2.one);material.SetTextureOffset("_BaseMap",Vector2.zero);
            material.SetTexture("_MetallicGlossMap",surface);material.SetFloat("_Metallic",1);material.SetFloat("_Smoothness",1);
            material.SetFloat("_WorkflowMode",1);material.SetFloat("_SmoothnessTextureChannel",0);
            material.SetFloat("_Surface",0);material.SetFloat("_AlphaClip",0);material.SetFloat("_ZWrite",1);
            material.SetFloat("_SrcBlend",(float)BlendMode.One);material.SetFloat("_DstBlend",(float)BlendMode.Zero);
            material.SetTexture("_BumpMap",null);material.DisableKeyword("_NORMALMAP");
            material.DisableKeyword("_SPECULAR_SETUP");material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");material.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");material.SetOverrideTag("RenderType","Opaque");material.renderQueue=-1;
            EditorUtility.SetDirty(material);return material;
        }

        private static Texture2D SaveHandSwatches(string name,Color[] pixels)
        {
            string path=Root+"/"+name+".asset";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            // Linear textures store the source shader constants directly, with 8-bit precision.
            // Two point-filtered texels need no mip chain and cannot bleed across material roles.
            if(!texture){texture=new Texture2D(2,1,TextureFormat.RGBA32,false,true){name=name};AssetDatabase.CreateAsset(texture,path);}
            if(texture.width!=2 || texture.height!=1 || texture.format!=TextureFormat.RGBA32 || texture.mipmapCount!=1 || !texture.isReadable)
                throw new InvalidOperationException("Unexpected generated hand swatch format: "+path);
            texture.filterMode=FilterMode.Point;texture.wrapMode=TextureWrapMode.Clamp;texture.anisoLevel=0;
            texture.SetPixels(pixels);texture.Apply(false,false);EditorUtility.SetDirty(texture);return texture;
        }

        private static void BuildBridle(Transform model,Transform head,Material leather,Material steel,Material thread,out Transform leftBit,out Transform rightBit)
        {
            var bridle=Child(model,"Western bridle");var hide=new ReinsHeadstallStrip();var hardware=new Surface();var seams=new Surface();
            var skin=new ReinsHorseSurface(model);
            var poll=model.InverseTransformPoint(head.position);
            // FBX bone +Y points down the face. Derive its frame in the live model space,
            // including the imported-root orientation/origin used by this art scene.
            var face=model.InverseTransformVector(head.TransformVector(Vector3.up)).normalized;
            var across=Vector3.ProjectOnPlane(Vector3.right,face).normalized;
            var front=Vector3.Cross(face,across).normalized;
            var tip=skin.Ray(poll+face*.9f,-face).Point;
            float faceLength=Vector3.Dot(tip-poll,face);
            if(faceLength<.40f || faceLength>.85f)throw new InvalidOperationException("Unexpected neutral head frame for fitted bridle: "+faceLength);
            var mouth=poll+face*(faceLength*.72f);
            Vector3 Fit(Vector3 center,Vector3 outward,float gap=.007f)
                =>skin.Ray(center+outward*.8f,-outward).Point+outward*gap;
            Vector3 CrownNormal(float t){float a=Mathf.PI*(1-t);return new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);}
            Vector3 Crown(float t,float edge)=>Fit(poll+new Vector3(0,-.045f,-.16f+edge*.0125f),CrownNormal(t));
            Vector3 BrowNormal(float t){float a=Mathf.PI+.20f-(Mathf.PI+.40f)*t;return across*Mathf.Cos(a)+front*Mathf.Sin(a);}
            Vector3 Brow(float t,float edge)=>Fit(poll+face*(-.025f+edge*.0105f),BrowNormal(t));
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
                var eyeBounds=new Bounds();bool foundEye=false;
                foreach(var eye in model.GetComponentsInChildren<MeshFilter>(true))
                {
                    if(!eye.name.StartsWith("HorseEye",StringComparison.Ordinal) || !eye.sharedMesh)continue;
                    var matrix=model.worldToLocalMatrix*eye.transform.localToWorldMatrix;
                    if(side*matrix.MultiplyPoint3x4(eye.sharedMesh.bounds.center).x<=0)continue;
                    var eyeVertices=eye.sharedMesh.vertices;
                    eyeBounds=new Bounds(matrix.MultiplyPoint3x4(eyeVertices[0]),Vector3.zero);
                    foreach(var vertex in eyeVertices)eyeBounds.Encapsulate(matrix.MultiplyPoint3x4(vertex));
                    foundEye=true;break;
                }
                if(!foundEye)throw new InvalidOperationException("Fitted cheek strap requires the live eye mesh on side "+side);
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
            Debug.Log("BARREL_BRIDLE_FIT: head="+poll.ToString("F4")+", face length="+faceLength.ToString("F4")+", left bit="+bitPositions[0].ToString("F4")+", right bit="+bitPositions[1].ToString("F4")+"; three existing material batches");
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

        internal sealed class Surface
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
