using System;
using System.IO;
using System.Linq;
using BarrelRivals.Practice;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace BarrelRivals.Editor
{
    public static partial class HeroHorseBenchmarkBuilder
    {
        public static void CaptureFinish()
        {
            ValidateAndCapture();
            var horse=Object.FindFirstObjectByType<HorseRigBindings>();
            var tack=horse.GetComponent<HeroHorseAttachments>();
            AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"/Neutral.anim").SampleAnimation(horse.Animator.gameObject,0);
            tack.SetRiderView(false);tack.RenderImmediate();
            var camera=Camera.main;
            camera.transform.position=new Vector3(3.1f,2.6f,3.5f);camera.transform.LookAt(new Vector3(0,1.55f,0));camera.fieldOfView=43;
            Capture(camera,Path.Combine(Output,"character-quarter.png"),1280,960);
            camera.transform.position=new Vector3(1.9f,2.1f,2.8f);camera.transform.LookAt(new Vector3(0,1.77f,.66f));camera.fieldOfView=30;
            Capture(camera,Path.Combine(Output,"coat-after.png"),1280,960);
            var material=horse.Body.sharedMaterial;var baseline=new Material(material);var improved=horse.Body.sharedMesh;
            var original=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath).GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name=="HeroHorseBody").sharedMesh;
            try
            {
                baseline.SetFloat("_CoatSheen",0);baseline.SetFloat("_CoatVariation",0);baseline.SetFloat("_CoatSmoothness",.28f);baseline.SetFloat("_Reflectance",.022f);baseline.SetFloat("_MicroNormal",.025f);baseline.SetVector("_MicroScale",new Vector4(850,260,0,0));
                horse.Body.sharedMaterial=baseline;horse.Body.sharedMesh=original;
                Capture(camera,Path.Combine(Output,"coat-before.png"),1280,960);
            }
            finally{horse.Body.sharedMaterial=material;horse.Body.sharedMesh=improved;Object.DestroyImmediate(baseline);}
            var hat=tack.rider.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name.Contains("hat"));
            var head=hat.bones.Single(b=>b.name=="head");
            camera.transform.position=head.position+new Vector3(.7f,.3f,.8f);camera.transform.LookAt(head.position+Vector3.up*.11f);camera.fieldOfView=32;
            Capture(camera,Path.Combine(Output,"western-hat.png"),960,960);
            camera.transform.position=new Vector3(-4.3f,2.4f,.2f);camera.transform.LookAt(new Vector3(0,1.45f,0));camera.fieldOfView=43;
            Capture(camera,Path.Combine(Output,"character-side.png"),1280,720);
            var flow=improved.tangents;var normals=improved.normals;
            VerifyCoatChannels(original,improved);
            float orthogonal=flow.Select((t,i)=>Mathf.Abs(Vector3.Dot((Vector3)t,normals[i]))).Max();
            Require(orthogonal<.0001f && flow.All(t=>Mathf.Abs(((Vector3)t).magnitude-1)<.0001f),"Invalid persisted coat flow");
            var geometry=horse.GetComponentsInChildren<Renderer>(true).Select(r=>new{renderer=r,mesh=r is SkinnedMeshRenderer s?s.sharedMesh:r.GetComponent<MeshFilter>()?.sharedMesh}).Where(v=>v.mesh).ToArray();
            File.WriteAllText(Path.Combine(Output,"finish-review.json"),JsonUtility.ToJson(new FinishReview{
                maximumTangentNormalDot=orthogonal,bodyVertices=improved.vertexCount,hatTriangles=hat.sharedMesh.triangles.Length/3,hatVertices=hat.sharedMesh.vertexCount,
                characterTriangles=geometry.Sum(v=>v.mesh.triangles.Length/3),characterSlots=geometry.Sum(v=>v.renderer.sharedMaterials.Length),characterRenderers=geometry.Length},true)+"\n");
            ProbeCoat();
            CaptureStableFit(horse,tack);
            Debug.Log("CHARACTER_FINISH_CAPTURED");
        }
        public static void RebuildFinish()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var horse=Object.FindFirstObjectByType<HorseRigBindings>();
            horse.Body.sharedMesh=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath).GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name=="HeroHorseBody").sharedMesh;
            HeroHorseCoatBuilder.Apply(horse.Body,horse.ModelSpace);
            var rider=horse.GetComponent<HeroHorseAttachments>().rider;
            WesternHatBuilder.Apply(rider.GetComponentsInChildren<SkinnedMeshRenderer>(),rider.transform.parent);
            var coat=horse.Body.sharedMaterial;coat.SetFloat("_CoatVariation",.055f);coat.SetFloat("_MicroNormal",.02f);EditorUtility.SetDirty(coat);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            CaptureFinish();
        }
        public static void VerifyFinishData()
        {
            Directory.CreateDirectory(Output);
            EditorSceneManager.OpenScene(ScenePath);
            var original=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath).GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name=="HeroHorseBody").sharedMesh;
            var improved=Object.FindFirstObjectByType<HorseRigBindings>().Body.sharedMesh;
            VerifyCoatChannels(original,improved);
            ProbeCoat();
        }
        static void VerifyCoatChannels(Mesh original,Mesh improved)
        {
            var checks=new[]{original.vertices.SequenceEqual(improved.vertices),original.normals.SequenceEqual(improved.normals),original.colors.SequenceEqual(improved.colors),original.uv.SequenceEqual(improved.uv),original.triangles.SequenceEqual(improved.triangles),original.boneWeights.SequenceEqual(improved.boneWeights),original.bindposes.SequenceEqual(improved.bindposes)};
            var labels=new[]{"positions","normals","colors","uv","indices","weights","bindposes"};
            File.WriteAllLines(Path.Combine(Output,"coat-channels.txt"),labels.Select((n,i)=>n+"="+checks[i]));
            for(int i=0;i<checks.Length;i++)Require(checks[i],"Coat may only change tangents: changed "+labels[i]);
        }
        [Serializable] class FinishReview
        {
            public float maximumTangentNormalDot;public int bodyVertices,hatTriangles,hatVertices,characterTriangles,characterSlots,characterRenderers;
            public bool originalGeometryColorSkinAndUVPreserved=true;
            public string scope="Actual Unity material/geometry review. Stable is an unsaved neutral substitution. Not photographic or phone acceptance.";
        }
    }
}
