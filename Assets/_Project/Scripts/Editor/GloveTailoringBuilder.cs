using System;
using System.Collections.Generic;
using UnityEngine;
using Object=UnityEngine.Object;

namespace BarrelRivals.Editor
{
    /// <summary>Original sewn leather details, fitted to the actual glove at authoring time.</summary>
    public static class GloveTailoringBuilder
    {
        // Vertex alpha selects fixed thread, not transparency. One opaque material
        // draws leather panels and thread without another runtime renderer or texture.
        public static Mesh Finish(Mesh shell, bool mirror=false)
        {
            var b=new Construction(shell,mirror);
            try { return b.Build(); }
            finally { Object.DestroyImmediate(shell); }
        }

        private sealed class Construction
        {
            readonly Vector3[] source,normal;readonly Vector2[] tex;readonly int[] indices;readonly bool mirror;
            readonly List<Vector3> vertices=new List<Vector3>(),normals=new List<Vector3>();
            readonly List<Vector2> uv=new List<Vector2>();readonly List<Color> colors=new List<Color>();
            readonly List<int> triangles=new List<int>();
            public Construction(Mesh shell,bool mirrored)
            {
                source=shell.vertices;normal=shell.normals;tex=shell.uv;indices=shell.triangles;mirror=mirrored;
                // Author both poses in left-hand coordinates, mirror every channel at the end.
                if(mirror) { for(int i=0;i<source.Length;i++){source[i].x=-source[i].x;normal[i].x=-normal[i].x;}
                    for(int i=0;i<indices.Length;i+=3)(indices[i+1],indices[i+2])=(indices[i+2],indices[i+1]); }
                vertices.AddRange(source);normals.AddRange(normal);uv.AddRange(tex);triangles.AddRange(indices);
                foreach(var p in source)colors.Add(new Color(1,1,1,0));
            }
            public Mesh Build()
            {
                // A rounded dorsal reinforcement, three raised pintucks and an overlapped
                // wrist closure. These sit above the source; the rein aperture is unchanged.
                var back=new[]{new Vector2(-.017f,-.006f),new Vector2(-.026f,.012f),new Vector2(-.024f,.044f),
                    new Vector2(-.014f,.063f),new Vector2(.009f,.064f),new Vector2(.025f,.045f),
                    new Vector2(.026f,.015f),new Vector2(.018f,-.006f)};
                Panel(back,.0011f,.83f);
                StitchLoop(back,.0020f,.88f);
                foreach(float x in new[]{-.012f,0,.012f})
                {
                    var points=new List<Sample>();
                    for(int i=0;i<=9;i++)points.Add(Project(new Vector2(x,.016f+i*.004f),.00165f));
                    Tube(points,.00072f,new Color(.60f,.60f,.60f,0));
                }
                var cuff=new[]{new Vector2(-.022f,-.041f),new Vector2(-.026f,-.029f),new Vector2(-.021f,-.016f),
                    new Vector2(.020f,-.016f),new Vector2(.027f,-.027f),new Vector2(.023f,-.041f)};
                for(int i=0;i<cuff.Length;i++)cuff[i].x=cuff[i].x*.78f+.002f;
                Panel(cuff,.0018f,.66f);StitchLoop(cuff,.00265f,.88f);
                // Small embossed diamond, original geometric mark with no reference branding.
                var diamond=new[]{new Vector2(0,-.032f),new Vector2(-.004f,-.028f),new Vector2(0,-.024f),new Vector2(.004f,-.028f),new Vector2(0,-.032f)};
                var mark=new List<Sample>();foreach(var p in diamond)mark.Add(Project(p,.0024f));
                Tube(mark,.0005f,new Color(.38f,.38f,.38f,0));
                if(mirror)
                {
                    for(int i=0;i<vertices.Count;i++){var p=vertices[i];p.x=-p.x;vertices[i]=p;var n=normals[i];n.x=-n.x;normals[i]=n;}
                    for(int i=0;i<triangles.Count;i+=3)(triangles[i+1],triangles[i+2])=(triangles[i+2],triangles[i+1]);
                }
                var mesh=new Mesh{name="Tailored western glove"};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);
                mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
            }
            readonly struct Sample
            {
                public readonly Vector3 p,n;public readonly Vector2 uv;
                public Sample(Vector3 position,Vector3 direction,Vector2 tex){p=position;n=direction;uv=tex;}
            }
            Sample Project(Vector2 point,float lift)
            {
                // Vertical barycentric intersection with the dorsal face. Do not use guessed
                // world coordinates or a live collider that could affect gameplay.
                float highest=float.NegativeInfinity;Sample result=default;
                for(int i=0;i<indices.Length;i+=3)
                {
                    int ia=indices[i],ib=indices[i+1],ic=indices[i+2];var a=source[ia];var b=source[ib];var c=source[ic];
                    float det=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);if(Mathf.Abs(det)<1e-10f)continue;
                    float u=((b.z-c.z)*(point.x-c.x)+(c.x-b.x)*(point.y-c.z))/det;
                    float v=((c.z-a.z)*(point.x-c.x)+(a.x-c.x)*(point.y-c.z))/det,w=1-u-v;
                    if(u<-.00001f || v<-.00001f || w<-.00001f)continue;
                    var p=a*u+b*v+c*w;var n=(normal[ia]*u+normal[ib]*v+normal[ic]*w).normalized;
                    if(p.y<=highest || n.y<.25f)continue;
                    highest=p.y;result=new Sample(p+n*lift,n,tex[ia]*u+tex[ib]*v+tex[ic]*w);
                }
                if(float.IsNegativeInfinity(highest))throw new InvalidOperationException("Glove detail outside dorsal surface: "+point);
                return result;
            }
            int Add(Sample s,Color color){int i=vertices.Count;vertices.Add(s.p);normals.Add(s.n);uv.Add(s.uv);colors.Add(color);return i;}
            void Tri(int a,int b,int c)
            {
                if(Vector3.Dot(Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]),normals[a]+normals[b]+normals[c])<0)(b,c)=(c,b);
                triangles.Add(a);triangles.Add(b);triangles.Add(c);
            }
            void Panel(Vector2[] contour,float lift,float tint)
            {
                Vector2 center=Vector2.zero;foreach(var p in contour)center+=p;center/=contour.Length;
                var boundary=new List<Vector2>();
                for(int e=0;e<contour.Length;e++)
                {var a=contour[e];var b=contour[(e+1)%contour.Length];int steps=Mathf.CeilToInt(Vector2.Distance(a,b)/.006f);for(int i=0;i<steps;i++)boundary.Add(Vector2.Lerp(a,b,i/(float)steps));}
                var color=new Color(tint,tint,tint,0);int pivot=Add(Project(center,lift),color),start=vertices.Count,n=boundary.Count;
                const int rings=6;
                for(int ring=1;ring<=rings;ring++)for(int j=0;j<n;j++)
                {
                    // Taper the panel edge back to the shell so it has thickness without a floating gap.
                    float height=ring==rings?.00015f:lift;
                    Add(Project(Vector2.Lerp(center,boundary[j],ring/(float)rings),height),color);
                }
                for(int j=0;j<n;j++)Tri(pivot,start+j,start+(j+1)%n);
                for(int ring=1;ring<rings;ring++)for(int j=0;j<n;j++)
                {int a=start+(ring-1)*n+j,b=start+(ring-1)*n+(j+1)%n;Tri(a,a+n,b);Tri(b,a+n,b+n);}
            }
            void StitchLoop(Vector2[] contour,float lift,float inset)
            {
                Vector2 center=Vector2.zero;foreach(var p in contour)center+=p;center/=contour.Length;
                for(int e=0;e<contour.Length;e++)
                {
                    var a=Vector2.Lerp(center,contour[e],inset);var b=Vector2.Lerp(center,contour[(e+1)%contour.Length],inset);
                    int count=Mathf.Max(1,Mathf.RoundToInt(Vector2.Distance(a,b)/.0042f));
                    for(int i=0;i<count;i++)Tube(new List<Sample>{Project(Vector2.Lerp(a,b,(i+.18f)/count),lift),Project(Vector2.Lerp(a,b,(i+.69f)/count),lift)},.00038f,Color.white);
                }
            }
            void Tube(List<Sample> path,float radius,Color color)
            {
                int first=vertices.Count;const int sides=4;
                for(int r=0;r<path.Count;r++)
                {
                    var forward=(path[Math.Min(r+1,path.Count-1)].p-path[Math.Max(0,r-1)].p).normalized;
                    var across=Vector3.Cross(forward,path[r].n).normalized;var up=Vector3.Cross(across,forward).normalized;
                    for(int j=0;j<sides;j++){float angle=j*Mathf.PI*2/sides;var n=across*Mathf.Cos(angle)+up*Mathf.Sin(angle);Add(new Sample(path[r].p+n*radius,n,path[r].uv),color);}
                    if(r==0)continue;
                    for(int j=0;j<sides;j++){int a=first+(r-1)*sides+j,b=first+(r-1)*sides+(j+1)%sides;Tri(a,b,a+sides);Tri(b,b+sides,a+sides);}
                }
                int near=Add(new Sample(path[0].p,(path[0].p-path[1].p).normalized,path[0].uv),color);
                int far=Add(new Sample(path[path.Count-1].p,(path[path.Count-1].p-path[path.Count-2].p).normalized,path[path.Count-1].uv),color);
                for(int j=0;j<sides;j++){Tri(near,first+(j+1)%sides,first+j);int last=first+(path.Count-1)*sides;Tri(far,last+j,last+(j+1)%sides);}
            }
        }
    }
}
