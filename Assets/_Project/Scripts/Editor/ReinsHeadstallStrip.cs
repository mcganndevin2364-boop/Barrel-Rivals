using System;
using System.Collections.Generic;
using UnityEngine;

namespace BarrelRivals.Editor
{
    /// <summary>Editor-only thin leather strips with independently skin-fitted edges.</summary>
    internal sealed class ReinsHeadstallStrip
    {
        readonly List<Vector3> vertices=new List<Vector3>();
        readonly List<Vector2> uv=new List<Vector2>();
        readonly List<int> triangles=new List<int>();

        public void Add(Func<float,float,Vector3> edge,Func<float,Vector3> normal,int segments,float halfDepth)
        {
            int first=vertices.Count;
            for(int i=0;i<=segments;i++)
            {
                float t=i/(float)segments;var n=normal(t).normalized;
                var left=edge(t,-1);var right=edge(t,1);
                vertices.Add(left+n*halfDepth);vertices.Add(right+n*halfDepth);
                vertices.Add(left-n*halfDepth);vertices.Add(right-n*halfDepth);
                uv.Add(new Vector2(0,t*3));uv.Add(new Vector2(1,t*3));
                uv.Add(new Vector2(0,t*3));uv.Add(new Vector2(1,t*3));
                if(i==0)continue;
                int a=first+(i-1)*4,b=a+4;
                var outward=normal((i-.5f)/segments).normalized;
                Quad(a,a+1,b,b+1,outward);Quad(a+2,b+2,a+3,b+3,-outward);
                Quad(a,a+2,b,b+2,left-right);Quad(a+1,b+1,a+3,b+3,right-left);
            }
            int last=first+segments*4;
            Quad(first,first+2,first+1,first+3,vertices[first]-vertices[first+4]);
            Quad(last,last+1,last+2,last+3,vertices[last]-vertices[last-4]);
        }
        void Quad(int a,int b,int c,int d,Vector3 outward)
        {
            if(Vector3.Dot(Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]),outward)>=0)
            {triangles.Add(a);triangles.Add(b);triangles.Add(c);triangles.Add(b);triangles.Add(d);triangles.Add(c);}
            else
            {triangles.Add(a);triangles.Add(c);triangles.Add(b);triangles.Add(b);triangles.Add(c);triangles.Add(d);}
        }
        public Mesh Mesh()
        {
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
        }
    }
}
