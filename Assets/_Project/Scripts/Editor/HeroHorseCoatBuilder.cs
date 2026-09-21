using System;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace BarrelRivals.Editor
{
    /// <summary>Original rest-surface hair flow; tangents then follow normal GPU skinning.</summary>
    public static class HeroHorseCoatBuilder
    {
        public static void Apply(SkinnedMeshRenderer body,Transform model)
        {
            var source=body.sharedMesh;var sourceWeights=source.boneWeights;var mesh=Object.Instantiate(source);mesh.name="Horse coat with anatomical fiber flow";
            var vertices=mesh.vertices;var normals=mesh.normals;var tangents=new Vector4[vertices.Length];
            var matrix=model.worldToLocalMatrix*body.localToWorldMatrix;var normalMatrix=matrix.inverse.transpose;
            for(int i=0;i<vertices.Length;i++)
            {
                var p=matrix.MultiplyPoint3x4(vertices[i]);var n=normalMatrix.MultiplyVector(normals[i]).normalized;
                // Trunk coat lies down/back; neck transitions toward the face. A restrained
                // forehead whorl changes direction, not geometry, reflectance or rig weights.
                float head=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.58f,1.12f,p.z));
                var direction=Vector3.Lerp(new Vector3(p.x*.25f,-.35f,-1),new Vector3(0,-.65f,1),head);
                var radial=Vector3.ProjectOnPlane(p-new Vector3(0,1.94f,1.02f),n);
                float whorl=(1-Mathf.SmoothStep(.035f,.095f,radial.magnitude))*Mathf.InverseLerp(1.78f,1.88f,p.y)*Mathf.InverseLerp(.78f,.96f,p.z);
                if(radial.sqrMagnitude>1e-6f)direction=Vector3.Lerp(direction,radial.normalized+Vector3.Cross(n,radial.normalized)*.55f,whorl);
                var tangent=Vector3.ProjectOnPlane(direction,n);
                if(tangent.sqrMagnitude<1e-5f)tangent=Vector3.ProjectOnPlane(Vector3.right,n);
                tangent=matrix.inverse.MultiplyVector(tangent.normalized).normalized;
                if(float.IsNaN(tangent.x))throw new InvalidOperationException("Invalid coat tangent");
                tangents[i]=new Vector4(tangent.x,tangent.y,tangent.z,1);
            }
            mesh.tangents=tangents;
            var saved=PersistentMeshAsset.Save(mesh,HeroHorseBenchmarkBuilder.Root+"/Coat mesh.asset");
            // The four-influence source uses the legacy packed layout. SetBoneWeights
            // in the generic persistence path can canonicalize its packed weight entries.
            // Restore that exact imported layout after copying the native buffers.
            saved.boneWeights=sourceWeights;
            EditorUtility.SetDirty(saved);AssetDatabase.SaveAssetIfDirty(saved);
            body.sharedMesh=saved;
        }
    }
}
