using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BarrelRivals.Practice;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace BarrelRivals.Editor
{
    public static partial class HeroHorseBenchmarkBuilder
    {
        public static void CaptureAttachments()
        {
            ValidateAndCapture();
            var horse=Object.FindFirstObjectByType<HorseRigBindings>();
            var tack=horse.GetComponent<HeroHorseAttachments>();
            var neutral=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"/Neutral.anim");
            var walk=AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
            var actor=horse.transform.position;
            float maxReach=0,maxRein=0,maxSeat=0,maxSole=0;
            var samples=new List<AttachmentSample>();
            var nodes=horse.GetComponentsInChildren<Transform>();
            var pelvis=nodes.Single(t=>t.name=="pelvis");var seat=nodes.Single(t=>t.name=="Fitted rider pelvis");
            for(int phase=0;phase<28;phase++)foreach(float pull in new[]{0f,.5f,1f})
            {
                walk.SampleAnimation(horse.Animator.gameObject,phase/30f);
                tack.leftPull=pull;tack.rightPull=1-pull;tack.RenderImmediate();
                maxReach=Mathf.Max(maxReach,tack.rider.MaximumReachError);
                maxSeat=Mathf.Max(maxSeat,Vector3.Distance(pelvis.position,seat.position));
                foreach(string name in new[]{"Fitted left ankle","Fitted right ankle"})
                    maxSole=Mathf.Max(maxSole,Mathf.Abs(HeroHorseRiderBuilder.MeasureBootClearance(horse,nodes.Single(t=>t.name==name))));
                foreach(var row in new[]{(tack.leftRein,tack.leftGrip,tack.leftBit),(tack.rightRein,tack.rightGrip,tack.rightBit)})
                {
                    var vertices=row.Item1.sharedMesh.vertices;
                    Vector3 Center(int first){var sum=Vector3.zero;for(int i=0;i<8;i++)sum+=row.Item1.transform.TransformPoint(vertices[first+i]);return sum/8;}
                    maxRein=Mathf.Max(maxRein,Vector3.Distance(Center(0),row.Item2.position),Vector3.Distance(Center(ReinsRiderTackPresentation.Segments*8),row.Item3.position));
                }
                Require(Vector3.Distance(actor,horse.transform.position)<1e-6f,"Attachment review moved the actor");
                samples.Add(new AttachmentSample{seconds=phase/30f,leftPull=pull,reachError=tack.rider.MaximumReachError});
            }
            Require(maxReach<.01f && maxSeat<.001f && maxRein<.00001f && maxSole<.012f,"Unreachable or disconnected candidate attachments");
            var geometry=horse.GetComponentsInChildren<Renderer>(true).Select(renderer=>
            {
                Mesh mesh=renderer is SkinnedMeshRenderer skin?skin.sharedMesh:renderer.GetComponent<MeshFilter>()?.sharedMesh;
                return new AttachmentGeometry{name=renderer.name,triangles=mesh?Enumerable.Range(0,mesh.subMeshCount).Sum(i=>(int)mesh.GetIndexCount(i)/3):0,slots=renderer.sharedMaterials.Length};
            }).Where(r=>r.triangles>0).ToArray();
            File.WriteAllText(Path.Combine(Output,"attachments.json"),JsonUtility.ToJson(new AttachmentReport{
                maximumReachErrorM=maxReach,maximumSeatErrorM=maxSeat,maximumReinEndpointErrorM=maxRein,maximumBootTreadClearanceM=maxSole,samples=samples.ToArray(),geometry=geometry,
                totalTriangles=geometry.Sum(r=>r.triangles),totalSlots=geometry.Sum(r=>r.slots)},true)+"\n");
            neutral.SampleAnimation(horse.Animator.gameObject,0);tack.leftPull=tack.rightPull=0;tack.SetRiderView(false);tack.RenderImmediate();
            var camera=Camera.main;camera.transform.position=new Vector3(-4.3f,2.4f,.2f);camera.transform.LookAt(new Vector3(0,1.45f,0));camera.fieldOfView=43;
            Capture(camera,Path.Combine(Output,"rider-side.png"),1280,720);
            CaptureStableFit(horse,tack);
            Debug.Log("HERO_ATTACHMENTS_CAPTURED");
        }

        static void CaptureStableFit(HorseRigBindings horse,HeroHorseAttachments tack)
        {
            // Temporary neutral art review in the real saved showroom. Never save this substitution.
            var benchmark=SceneManager.GetActiveScene();
            var stable=EditorSceneManager.OpenScene(StableBuilder.ScenePath,OpenSceneMode.Additive);
            var original=stable.GetRootGameObjects().Single(g=>g.name=="Copper");
            var targetPosition=original.transform.position;var targetRotation=original.transform.rotation;
            original.SetActive(false);
            SceneManager.MoveGameObjectToScene(horse.gameObject,stable);
            EditorSceneManager.SetActiveScene(stable);EditorSceneManager.CloseScene(benchmark,true);
            horse.transform.SetPositionAndRotation(targetPosition,targetRotation);
            tack.enabled=false;tack.rider.gameObject.SetActive(false);tack.leftHand.gameObject.SetActive(false);tack.rightHand.gameObject.SetActive(false);
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.enabled=false;
            var bodyMesh=new Mesh();horse.Body.BakeMesh(bodyMesh,true);
            var matrix=horse.ModelSpace.worldToLocalMatrix*horse.Body.localToWorldMatrix;
            bodyMesh.vertices=bodyMesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray();bodyMesh.RecalculateBounds();
            var sampling=new GameObject("Temporary stowed rein fitting skin");var collider=sampling.AddComponent<MeshCollider>();collider.sharedMesh=bodyMesh;Physics.SyncTransforms();
            var horn=horse.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Fitted saddle horn");
            try
            {
                foreach(var row in new[]{(side:-1,filter:tack.leftRein,bit:tack.leftBit),(side:1,filter:tack.rightRein,bit:tack.rightBit)})
                {
                    var start=horse.ModelSpace.InverseTransformPoint(horn.position)+new Vector3(row.side*.045f,0,0);
                    var end=horse.ModelSpace.InverseTransformPoint(row.bit.position);var points=new Vector3[41];var minimum=new float[41];
                    for(int i=0;i<points.Length;i++)
                    {
                        float t=i/40f;var p=Vector3.Lerp(start,end,t)+new Vector3(row.side*.08f*Mathf.Sin(t*Mathf.PI),-.26f*Mathf.Sin(t*Mathf.PI),0);
                        minimum[i]=row.side*p.x;
                        if(i>0 && i<40 && collider.Raycast(new Ray(new Vector3(row.side*2,p.y,p.z),Vector3.left*row.side),out var hit,2))
                            minimum[i]=Mathf.Max(minimum[i],row.side*hit.point.x+.028f);
                        p.x=row.side*minimum[i];points[i]=p;
                    }
                    for(int pass=0;pass<80;pass++)for(int i=1;i<40;i++)
                    {var p=points[i];p.x=row.side*Mathf.Max(minimum[i],row.side*(points[i-1].x+points[i+1].x)*.5f);points[i]=p;}
                    for(int i=0;i<points.Length;i++)points[i]=row.filter.transform.InverseTransformPoint(horse.ModelSpace.TransformPoint(points[i]));
                    var cord=new ReinsRiderTackBuilder.Surface();cord.Tube(points,.009f,10,true);
                    var mesh=cord.Mesh();var triangles=mesh.triangles;mesh.subMeshCount=3;mesh.SetTriangles(Array.Empty<int>(),0);mesh.SetTriangles(triangles,1);mesh.SetTriangles(Array.Empty<int>(),2);
                    row.filter.sharedMesh=mesh;
                    // Adapter retains and disposes its own private live rein; this mesh is review-only.
                    temporaryStableMeshes.Add(mesh);
                }
                var camera=stable.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).Single(c=>c.name=="Stable camera");
                Capture(camera,Path.Combine(Output,"stable-fit.png"),1280,720);
            }
            finally
            {
                Object.DestroyImmediate(sampling);Object.DestroyImmediate(bodyMesh);
                foreach(var mesh in temporaryStableMeshes)Object.DestroyImmediate(mesh);temporaryStableMeshes.Clear();
            }
        }
        static readonly List<Mesh> temporaryStableMeshes=new List<Mesh>();
        [Serializable] class AttachmentSample {public float seconds,leftPull,reachError;}
        [Serializable] class AttachmentGeometry {public string name;public int triangles,slots;}
        [Serializable] class AttachmentReport
        {
            public float maximumReachErrorM,maximumSeatErrorM,maximumReinEndpointErrorM,maximumBootTreadClearanceM;
            public AttachmentSample[] samples;public AttachmentGeometry[] geometry;public int totalTriangles,totalSlots;
            public string scope="84 sampled walk/pull combinations in isolated Unity scene. Endpoint alignment is not continuous collision or natural-motion acceptance; stable substitution is an unsaved neutral inspection only.";
        }
    }
}
