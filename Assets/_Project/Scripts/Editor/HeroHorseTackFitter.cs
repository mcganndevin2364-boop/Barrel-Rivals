using System;
using System.Collections.Generic;
using System.Linq;
using BarrelRivals.Practice;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace BarrelRivals.Editor
{
    /// <summary>Refits the existing original saddle construction, preserving its material slots.</summary>
    public static class HeroHorseTackFitter
    {
        public static void Build(HorseRigBindings target)
        {
            var legacySpace=new GameObject("Temporary original saddle fit");
            try
            {
                var legacy=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ReinsReferenceArtBuilder.HorsePath),legacySpace.transform);
                legacy.transform.localRotation=Quaternion.Euler(0,180,0)*legacy.transform.localRotation;
                var animator=legacy.GetComponentInChildren<Animator>();animator.enabled=false;
                var idle=AssetDatabase.LoadAllAssetsAtPath(ReinsReferenceArtBuilder.HorsePath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__",StringComparison.Ordinal)).Single(c=>c.name=="Idle"||c.name.EndsWith("|Idle",StringComparison.Ordinal));
                idle.SampleAnimation(animator.gameObject,0);
                var oldBody=legacy.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r=>r.name.StartsWith("HorseBody",StringComparison.Ordinal));
                using(var oldSkin=new Surface(oldBody,legacySpace.transform))
                using(var newSkin=new Surface(target.Body,target.ModelSpace))
                {
                    var saddle=new GameObject("Fitted western saddle").transform;saddle.SetParent(target.ModelSpace,false);
                    var cache=new Dictionary<Vector3,Vector3>();
                    Vector3 Fit(Vector3 p)
                    {
                        if(cache.TryGetValue(p,out var result))return result;
                        float z=-.32f+(p.z-oldSkin.Bounds.center.z+.39f)*.88f;
                        var from=new Vector3(0,1.35f,p.z);
                        float top=newSkin.Ray(new Vector3(0,3,z),Vector3.down).y;
                        float bottom=newSkin.Ray(new Vector3(0,0,z),Vector3.up).y;
                        var to=new Vector3(0,(top+bottom)*.5f,z);
                        var radial=p-from;radial.z=0;float length=radial.magnitude;
                        var direction=radial/length;
                        var oldSurface=oldSkin.Ray(from+direction*2,-direction);
                        var newSurface=newSkin.Ray(to+direction*2,-direction);
                        float clearance=length-Vector3.Dot(oldSurface-from,direction);
                        result=newSurface+direction*clearance;cache.Add(p,result);return result;
                    }
                    foreach(var item in new[]{
                        ("Curved western pad","Woven saddle pad","Assets/_Project/Art/Reins/Stable/Desert woven wool.mat"),
                        ("Blanket stripes","Pad binding and cinch","Assets/_Project/Art/Reins/Stable/Blanket ivory stitching.mat"),
                        ("Western saddle leather","Western saddle","Assets/_Project/Art/Reins/Premium/Materials/Ranch saddle leather.mat"),
                        ("Saddle fittings","Saddle hardware","Assets/_Project/Art/Reins/Stable/Saddle silver.mat")})
                    {
                        var original=AssetDatabase.LoadAssetAtPath<Mesh>(StableTackBuilder.Root+"/"+item.Item1+".asset");
                        var mesh=Object.Instantiate(original);mesh.name="Fitted "+item.Item1;
                        mesh.vertices=mesh.vertices.Select(Fit).ToArray();mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
                        var saved=PersistentMeshAsset.Save(mesh,HeroHorseBenchmarkBuilder.Root+"/"+mesh.name+".asset");
                        var go=new GameObject(item.Item2,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(saddle,false);go.GetComponent<MeshFilter>().sharedMesh=saved;
                        var material=AssetDatabase.LoadAssetAtPath<Material>(item.Item3);
                        if(!material)throw new InvalidOperationException("Missing original tack material "+item.Item3);
                        go.GetComponent<MeshRenderer>().sharedMaterial=material;
                    }
                    var horn=new GameObject("Fitted saddle horn").transform;horn.SetParent(saddle,false);
                    horn.localPosition=Fit(new Vector3(0,2.039f,-.115f+oldSkin.Bounds.center.z));
                    saddle.SetParent(target.SaddleSupport,true);
                    Debug.Log("HORSE_SADDLE_FITTED oldBounds="+oldSkin.Bounds+" newBounds="+newSkin.Bounds+" samples="+cache.Count);
                }
            }
            finally{Object.DestroyImmediate(legacySpace);}
        }

        sealed class Surface:IDisposable
        {
            readonly GameObject colliderObject;readonly Mesh mesh;readonly MeshCollider collider;
            public Bounds Bounds {get;}
            readonly Dictionary<(Vector3,Vector3),Vector3> samples=new Dictionary<(Vector3,Vector3),Vector3>();
            public Surface(SkinnedMeshRenderer body,Transform model)
            {
                mesh=new Mesh();body.BakeMesh(mesh,true);
                var matrix=model.worldToLocalMatrix*body.localToWorldMatrix;
                mesh.vertices=mesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray();mesh.RecalculateBounds();Bounds=mesh.bounds;
                colliderObject=new GameObject("Temporary neutral fitting surface"){hideFlags=HideFlags.HideAndDontSave};
                collider=colliderObject.AddComponent<MeshCollider>();collider.sharedMesh=mesh;Physics.SyncTransforms();
            }
            public Vector3 Ray(Vector3 origin,Vector3 direction)
            {
                var key=(origin,direction);if(samples.TryGetValue(key,out var point))return point;
                if(!collider.Raycast(new Ray(origin,direction),out var hit,5))throw new InvalidOperationException("Saddle fitting ray missed "+origin.ToString("F4")+" "+direction);
                samples.Add(key,hit.point);return hit.point;
            }
            public void Dispose(){Object.DestroyImmediate(colliderObject);Object.DestroyImmediate(mesh);}
        }
    }
}
