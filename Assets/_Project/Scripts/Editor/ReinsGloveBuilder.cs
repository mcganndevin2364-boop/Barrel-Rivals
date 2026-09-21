using System;
using System.IO;
using UnityEngine;

namespace BarrelRivals.Editor
{
    /// <summary>Imports offline posed, connected CC0-derived glove surfaces in metres.</summary>
    public static class ReinsGloveBuilder
    {
        public const string Root="Assets/_Project/Art/Reins/Gloves";
        public static Mesh Riding(int side)=>Read("Closed riding glove",side>0);
        public static Mesh Inspection()=>Read("Open riding glove",false);

        private static Mesh Read(string name,bool mirror)
        {
            var data=JsonUtility.FromJson<Surface>(File.ReadAllText(Root+"/"+name+".json"));
            if(data.vertices==null || data.normals==null || data.uv==null || data.triangles==null ||
                data.vertices.Length<100 || data.normals.Length!=data.vertices.Length || data.uv.Length!=data.vertices.Length || data.triangles.Length%3!=0)
                throw new InvalidDataException("Invalid glove surface: "+name);
            for(int i=0;i<data.vertices.Length;i++)
            {
                var p=data.vertices[i];var n=data.normals[i];var uv=data.uv[i];
                if(!float.IsFinite(p.x+p.y+p.z+n.x+n.y+n.z+uv.x+uv.y) || Mathf.Abs(n.sqrMagnitude-1)>.02f)
                    throw new InvalidDataException("Invalid glove vertex or normal: "+name);
                if(mirror){p.x=-p.x;n.x=-n.x;}
                data.vertices[i]=p;data.normals[i]=n;
            }
            foreach(int index in data.triangles)
                if(index<0 || index>=data.vertices.Length)throw new InvalidDataException("Invalid glove index: "+name);
            if(mirror)
                for(int i=0;i<data.triangles.Length;i+=3)
                {int swap=data.triangles[i+1];data.triangles[i+1]=data.triangles[i+2];data.triangles[i+2]=swap;}
            var mesh=new Mesh{name=name};
            mesh.vertices=data.vertices;mesh.normals=data.normals;mesh.uv=data.uv;mesh.triangles=data.triangles;
            mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
        }

        [Serializable] private sealed class Surface
        {public Vector3[] vertices,normals;public Vector2[] uv;public int[] triangles;}
    }
}
