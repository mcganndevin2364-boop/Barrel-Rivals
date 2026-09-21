using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BarrelRivals.Editor
{
    /// <summary>Original cattleman crown, rolled brim and fitted band on the existing head skin.</summary>
    public static class WesternHatBuilder
    {
        public const string MeshPath=ReinsRiderBodyBuilder.Root+"/Original cattleman hat.asset";
        public const string MaterialPath=ReinsRiderBodyBuilder.Root+"/Original cattleman felt.mat";
        public static void Apply(SkinnedMeshRenderer[] skins,Transform model)
        {
            var hat=skins.Single(s=>s.name.Contains("hat"));
            // Always measure the preserved source, including when updating an already
            // rebuilt saved scene. Measuring our output would grow the brim each run.
            var original=AssetDatabase.LoadAssetAtPath<GameObject>(ReinsRiderBodyBuilder.Root+"/WesternRider.fbx")
                .GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name.Contains("hat")).sharedMesh;
            var skin=skins.Single(s=>s.name=="Rider skin");
            var matrix=model.worldToLocalMatrix*hat.localToWorldMatrix;
            var old=original.vertices.Select(matrix.MultiplyPoint3x4).ToArray();
            var bounds=new Bounds(old[0],Vector3.zero);foreach(var p in old)bounds.Encapsulate(p);
            float top=skin.sharedMesh.vertices.Max(v=>model.InverseTransformPoint(skin.transform.TransformPoint(v)).y);
            var center=new Vector3(bounds.center.x,top-.065f,bounds.center.z);
            float rx=bounds.extents.x*.57f,rz=bounds.extents.z*.64f;
            var vertices=new List<Vector3>();var colors=new List<Color>();var uv=new List<Vector2>();var triangles=new List<int>();
            const int sides=40;
            var felt=new Color(.115f,.069f,.036f,0);var band=new Color(.031f,.015f,.007f,1);var thread=new Color(.24f,.16f,.085f,1);
            Vector3 Point(float xRadius,float zRadius,float height,float angle,float curl=0,float crease=0)
            {
                float x=Mathf.Cos(angle),z=Mathf.Sin(angle);
                float crownX=x*xRadius/rx,crownZ=z*zRadius/rz;
                float channel=Mathf.Exp(-crownX*crownX/.045f)+.42f*Mathf.Exp(-Mathf.Pow((Mathf.Abs(crownX)-.57f)/.20f,2));
                float y=height+curl*Mathf.Pow(Mathf.Abs(x),3)-crease*channel*(.7f+.3f*(1-Mathf.Min(1,crownZ*crownZ)));
                float pinch=1-.09f*Mathf.Max(0,z)*Mathf.Clamp01(height/.10f);
                return center+new Vector3(x*xRadius*pinch,y,z*zRadius);
            }
            int Vertex(Vector3 p,Color color,Vector2 tex)
            {int index=vertices.Count;vertices.Add(matrix.inverse.MultiplyPoint3x4(p));colors.Add(color);uv.Add(tex);return index;}
            int Ring(float xRadius,float zRadius,float height,Color color,float curl=0,float crease=0)
            {
                int first=vertices.Count;
                for(int i=0;i<sides;i++){float a=i*2*Mathf.PI/sides;Vertex(Point(xRadius,zRadius,height,a,curl,crease),color,new Vector2(i/(float)sides,height*6));}
                return first;
            }
            void Join(int first,int next)
            {
                for(int i=0;i<sides;i++){int j=(i+1)%sides;triangles.AddRange(new[]{first+i,next+j,first+j,first+i,next+i,next+j});}
            }
            var rings=new[]{Ring(rx,rz,-.004f,felt),Ring(bounds.extents.x*1.10f,bounds.extents.z*1.12f,-.004f,felt,.050f),
                Ring(bounds.extents.x*1.10f,bounds.extents.z*1.12f,0,felt,.050f),Ring(rx,rz,0,felt),
                Ring(rx,rz,.018f,felt),Ring(rx*.96f,rz*.97f,.073f,felt,0,.004f),
                Ring(rx*.87f,rz*.90f,.105f,felt,0,.020f),Ring(rx*.53f,rz*.56f,.118f,felt,0,.022f),
                Ring(rx*.12f,rz*.14f,.119f,felt,0,.022f)};
            for(int i=1;i<rings.Length;i++)Join(rings[i-1],rings[i]);
            int bottom=Vertex(center+Vector3.down*.004f,felt,Vector2.zero),cap=Vertex(center+Vector3.up*.097f,felt,Vector2.one);
            for(int i=0;i<sides;i++){int j=(i+1)%sides;triangles.AddRange(new[]{rings[0]+i,rings[0]+j,bottom,rings[8]+i,cap,rings[8]+j});}
            // Closed leather ribbon follows the lower crown profile, with actual small stitches.
            int[] ribbon={Ring(rx+.002f,rz+.002f,.006f,band),Ring(rx+.002f,rz+.002f,.027f,band),Ring(rx,rz,.027f,band),Ring(rx,rz,.006f,band)};
            for(int i=0;i<4;i++)Join(ribbon[i],ribbon[(i+1)%4]);
            for(int i=0;i<sides;i++)foreach(float y in new[]{.009f,.024f})
            {
                float a=(i+.2f)*2*Mathf.PI/sides,b=(i+.45f)*2*Mathf.PI/sides;int k=vertices.Count;
                Vertex(Point(rx+.003f,rz+.003f,y-.00045f,a),thread,Vector2.zero);Vertex(Point(rx+.003f,rz+.003f,y+.00045f,a),thread,Vector2.up);
                Vertex(Point(rx+.003f,rz+.003f,y+.00045f,b),thread,Vector2.one);Vertex(Point(rx+.003f,rz+.003f,y-.00045f,b),thread,Vector2.right);
                triangles.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});
            }
            var mesh=new Mesh{name="Original cattleman western hat"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetColors(colors);mesh.SetUVs(0,uv);
            mesh.bindposes=original.bindposes;
            int headIndex=Array.FindIndex(hat.bones,b=>b.name=="head");if(headIndex<0)throw new InvalidOperationException("Hat head binding missing");
            mesh.boneWeights=vertices.Select(v=>new BoneWeight{boneIndex0=headIndex,weight0=1}).ToArray();mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            hat.sharedMesh=PersistentMeshAsset.Save(mesh,MeshPath);hat.localBounds=hat.sharedMesh.bounds;
            var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if(!material){material=new Material(Shader.Find("Barrel Rivals/Horse Surface")){name="Original cattleman felt and band"};AssetDatabase.CreateAsset(material,MaterialPath);}
            material.SetColor("_Tint",Color.white);material.SetFloat("_Reflectance",.035f);material.SetFloat("_BareSmoothness",.26f);material.SetFloat("_CoatSmoothness",.10f);material.SetFloat("_MicroNormal",.02f);material.SetFloat("_CoatSheen",0);material.SetFloat("_CoatVariation",.055f);material.SetVector("_MicroScale",new Vector4(210,90,0,0));
            EditorUtility.SetDirty(material);hat.sharedMaterial=material;
        }
    }
}
