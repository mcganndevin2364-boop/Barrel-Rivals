using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BarrelRivals.Practice;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace BarrelRivals.Editor
{
    public static partial class HeroHorseBenchmarkBuilder
    {
        public static void ValidateAndCapture()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Directory.CreateDirectory(Output);
            var binding=Object.FindFirstObjectByType<HorseRigBindings>();
            var animator=binding.Animator;animator.enabled=false;
            var neutral=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"/Neutral.anim");
            neutral.SampleAnimation(animator.gameObject,0);
            var debug=new List<string>();
            foreach(var skin in new[]{binding.Body,binding.Eyes,binding.Groom})
            {
                var baked=new Mesh();skin.BakeMesh(baked,true);float error=0;var bakedPoints=baked.vertices;var sourcePoints=skin.sharedMesh.vertices;
                for(int i=0;i<bakedPoints.Length;i++)error=Mathf.Max(error,Vector3.Distance(skin.transform.TransformPoint(bakedPoints[i]),skin.transform.TransformPoint(sourcePoints[i])));
                debug.Add(skin.name+" maximum bake/shared deviation="+error);
                for(int i=0;i<skin.bones.Length;i++)
                {
                    var expected=skin.localToWorldMatrix*skin.sharedMesh.bindposes[i].inverse;
                    float bindDistance=Vector3.Distance(skin.bones[i].position,expected.GetColumn(3));
                    float rotation=Quaternion.Angle(skin.bones[i].rotation,expected.rotation);
                    if(bindDistance>.00001f||rotation>.01f)debug.Add(skin.bones[i].name+" bind position deviation="+bindDistance+" rotation="+rotation);
                }
                Object.DestroyImmediate(baked);
            }
            File.WriteAllLines(Path.Combine(Output,"neutral-diagnostic.txt"),debug);
            var surfaces=VerifySurfaceReference(binding);
            Require(!animator.applyRootMotion && animator.cullingMode==AnimatorCullingMode.AlwaysAnimate,"Animator presentation policy");
            Require(!EditorBuildSettings.scenes.Any(s=>s.path==ScenePath),"Benchmark must not become a player startup scene");
            Require(binding.Groom.sharedMaterials.All(m=>m.shader.name=="Barrel Rivals/Horse Fiber"),"Explicit groom material binding");
            Require(binding.Body.sharedMaterial.shader.name=="Barrel Rivals/Horse Surface" && binding.Eyes.sharedMaterial.shader.name=="Barrel Rivals/Horse Surface","Explicit body/eye material binding");
            Require(binding.Groom.sharedMesh.uv2.Length==binding.Groom.sharedMesh.vertexCount,"Persisted strand progress");
            Require(binding.Groom.sharedMesh.boneWeights.All(w=>Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)<.00001f),"Persisted normalized groom skin");
            var rootBefore=binding.MotionRoot.position;var followed=binding.FollowSupportPoint(new Vector3(0,2.2f,-.35f));
            var delta=binding.ModelSpace.TransformVector(new Vector3(.025f,.05f,.01f));binding.MotionRoot.position+=delta;
            Require(Vector3.Distance(binding.FollowSupportPoint(new Vector3(0,2.2f,-.35f))-followed,delta)<.00002f,"Camera must inherit visual Root movement");
            binding.MotionRoot.position=rootBefore;
            var camera=Camera.main;var originalActor=binding.transform.position;
            var attachments=binding.GetComponent<HeroHorseAttachments>();
            var riderPoint=binding.GetComponent<HeroHorseBenchmarkPlayback>().neutralRiderPosition;
            if(attachments){attachments.SetRiderView(false);attachments.RenderImmediate();}
            var walk=AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));
            foreach(var shader in new[]{binding.Body.sharedMaterial.shader,binding.Groom.sharedMaterial.shader})
                Require(!ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity.ToString()=="Error"),"Shader import errors: "+shader.name);
            if(attachments){camera.transform.position=new Vector3(3.8f,2.8f,4.3f);camera.transform.LookAt(new Vector3(0,1.45f,0));}
            Capture(camera,Path.Combine(Output,"neutral.png"),960,720);
            camera.transform.position=new Vector3(1.7f,2.1f,2.5f);camera.transform.LookAt(new Vector3(0,1.82f,1.04f));camera.fieldOfView=31;
            Capture(camera,Path.Combine(Output,"head.png"),960,720);
            var frames=new List<Frame>();
            foreach(string view in new[]{"quarter","rider"})
            {
                string folder=Path.Combine(Output,"frames-"+view);Directory.CreateDirectory(folder);
                for(int frame=0;frame<28;frame++)
                {
                    float time=frame/30f;walk.SampleAnimation(animator.gameObject,time);
                    if(attachments){attachments.SetRiderView(view=="rider");attachments.RenderImmediate();}
                    if(view=="quarter")
                    {camera.transform.position=new Vector3(3.8f,2.8f,4.3f);camera.transform.LookAt(new Vector3(0,1.45f,0));camera.fieldOfView=43;}
                    else
                    {camera.transform.position=binding.FollowSupportPoint(riderPoint);camera.transform.rotation=binding.ModelSpace.rotation*Quaternion.Euler(binding.GetComponent<HeroHorseBenchmarkPlayback>().riderPitch,0,0);camera.fieldOfView=binding.GetComponent<HeroHorseBenchmarkPlayback>().riderFieldOfView;}
                    Capture(camera,Path.Combine(folder,(frame+1).ToString("D3")+".png"),800,600);
                    Require(Vector3.Distance(binding.transform.position,originalActor)<1e-7f,"Visual sampling moved actor root");
                    frames.Add(new Frame{view=view,time=time,camera=camera.transform.position,root=binding.ModelSpace.InverseTransformPoint(binding.MotionRoot.position),head=binding.ModelSpace.InverseTransformPoint(binding.Head.position)});
                }
            }
            foreach(var shader in new[]{binding.Body.sharedMaterial.shader,binding.Groom.sharedMaterial.shader})
                Require(!ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity.ToString()=="Error"),"Shader rendering errors: "+shader.name);
            Require(frames.Where(f=>f.view=="rider").Max(f=>f.camera.y)-frames.Where(f=>f.view=="rider").Min(f=>f.camera.y)>.008f,"Inherited camera motion did not advance");
            var report=new ReviewReport{surfaces=surfaces,frames=frames.ToArray(),activePipeline=GraphicsSettings.currentRenderPipeline.name,unity=Application.unityVersion,shaderErrors=0,opaqueMaskPixels=VerifyOpaqueMask(),maximumAnimatedBodyErrorM=bodyMotionError,animatedReferenceTargetMet=bodyMotionError<AnimatedReferenceTargetM};
            File.WriteAllText(Path.Combine(Output,"review.json"),JsonUtility.ToJson(report,true)+"\n");
            Debug.Log("HORSE_UNITY_CAPTURE_COMPLETED animatedReferenceTargetMet="+report.animatedReferenceTargetMet);
        }
        [Serializable] public class SurfaceResult {public string name;public int sourceVertices,importedVertices,matchedSourceVertices;public float maximumPositionErrorM,maximumLinearColorError;public string colorFormat;}
        [Serializable] class Frame {public string view;public float time;public Vector3 camera,root,head;}
        [Serializable] class ReviewReport {public string unity,activePipeline;public int shaderErrors;public int animatedBodySamples=7;public float maximumAnimatedBodyErrorM;public float animatedReferenceTargetM=AnimatedReferenceTargetM;public bool animatedReferenceTargetMet;public int[] opaqueMaskPixels;public int captureMsaaSamples=2;public SurfaceResult[] surfaces;public Frame[] frames;public string scope="Controlled actual Unity material/walk review. No production replacement, reference-quality or phone performance acceptance.";}
        // Keep the original precision target visible even when a development capture proceeds.
        const float AnimatedReferenceTargetM=.0002f;
        static float bodyMotionError;
        static SurfaceResult[] VerifySurfaceReference(HorseRigBindings binding)
        {
            var results=new List<SurfaceResult>();int[] bodyMap=null;
            using(var reader=new BinaryReader(File.OpenRead("ArtSource/HorseStudy/UnitySurfaceReference.bytes")))
            {
                Require(Encoding.ASCII.GetString(reader.ReadBytes(8))=="BRHCOL02","Reference format");
                int meshes=reader.ReadInt32();Require(meshes==2,"Reference mesh count");
                for(int part=0;part<meshes;part++)
                {
                    string name=Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));int count=reader.ReadInt32();
                    var positions=new Vector3[count];var colors=new Color[count];var grid=new Dictionary<Vector3Int,List<int>>();
                    Vector3Int Cell(Vector3 p)=>new Vector3Int(Mathf.FloorToInt(p.x*1000),Mathf.FloorToInt(p.y*1000),Mathf.FloorToInt(p.z*1000));
                    for(int i=0;i<count;i++)
                    {positions[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());colors[i]=new Color(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());var cell=Cell(positions[i]);if(!grid.TryGetValue(cell,out var list))grid[cell]=list=new List<int>();list.Add(i);}
                    var skin=name=="HeroHorseBody"?binding.Body:binding.Eyes;var mesh=new Mesh();skin.BakeMesh(mesh,true);
                    var matrix=binding.ModelSpace.worldToLocalMatrix*skin.localToWorldMatrix;
                    var actualColors=skin.sharedMesh.colors;var matched=new HashSet<int>();var mapping=new int[mesh.vertexCount];
                    var result=new SurfaceResult{name=name,sourceVertices=count,importedVertices=mesh.vertexCount,colorFormat=skin.sharedMesh.GetVertexAttributeFormat(VertexAttribute.Color).ToString()};
                    Require(actualColors.Length==mesh.vertexCount,"Vertex color channel missing");
                    foreach(var item in mesh.vertices.Select((v,i)=>(point:matrix.MultiplyPoint3x4(v),index:i)))
                    {
                        var cell=Cell(item.point);float best=float.PositiveInfinity;int match=-1;
                        for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)for(int z=-1;z<=1;z++)
                            if(grid.TryGetValue(cell+new Vector3Int(x,y,z),out var candidates))foreach(int j in candidates)
                            {float distance=Vector3.Distance(item.point,positions[j]);if(distance<best){best=distance;match=j;}}
                        Require(match>=0,"Neutral source position not found for "+name+" "+item.point.ToString("F6"));
                        mapping[item.index]=match;matched.Add(match);result.maximumPositionErrorM=Mathf.Max(result.maximumPositionErrorM,best);
                        Color c=actualColors[item.index],expected=colors[match];for(int channel=0;channel<4;channel++)result.maximumLinearColorError=Mathf.Max(result.maximumLinearColorError,Mathf.Abs(c[channel]-expected[channel]));
                    }
                    result.matchedSourceVertices=matched.Count;Object.DestroyImmediate(mesh);
                    Require(result.maximumPositionErrorM<.00001f && matched.Count==count,"Neutral geometry fidelity: "+JsonUtility.ToJson(result));
                    float colorTolerance=result.colorFormat=="UNorm8"?1f/255+.00001f:.00001f;
                    Require(result.maximumLinearColorError<colorTolerance,"Linear color fidelity: "+JsonUtility.ToJson(result));
                    results.Add(result);if(name=="HeroHorseBody")bodyMap=mapping;
                }
                int poses=reader.ReadInt32(),vertices=reader.ReadInt32();Require(poses==7&&vertices==19502,"Animated reference dimensions");
                Require(binding.Body.quality==SkinQuality.Bone4,"Benchmark must preserve four-weight skinning across quality tiers");
                var clip=AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));bodyMotionError=0;var motionDiagnostics=new List<string>();
                motionDiagnostics.Add("Blend weight format="+binding.Body.sharedMesh.GetVertexAttributeFormat(VertexAttribute.BlendWeight)+" renderer quality="+binding.Body.quality+" global="+QualitySettings.skinWeights);
                for(int pose=0;pose<poses;pose++)
                {
                    float time=reader.ReadSingle();var expected=new Vector3[vertices];
                    for(int j=0;j<vertices;j++)expected[j]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
                    clip.SampleAnimation(binding.Animator.gameObject,time);var baked=new Mesh();binding.Body.BakeMesh(baked,true);
                    var matrix=binding.ModelSpace.worldToLocalMatrix*binding.Body.localToWorldMatrix;var points=baked.vertices;
                    float maximum=0;int worst=0;var mean=Vector3.zero;
                    for(int i=0;i<points.Length;i++){var difference=matrix.MultiplyPoint3x4(points[i])-expected[bodyMap[i]];Require(!float.IsNaN(difference.sqrMagnitude)&&!float.IsInfinity(difference.sqrMagnitude),"Non-finite animated body position");mean+=difference;if(difference.magnitude>maximum){maximum=difference.magnitude;worst=i;}}
                    bodyMotionError=Mathf.Max(bodyMotionError,maximum);var bw=binding.Body.sharedMesh.boneWeights[worst];
                    motionDiagnostics.Add("time="+time+" max="+maximum+" mean="+(mean/points.Length).ToString("F8")+" source vertex="+bodyMap[worst]+" position="+expected[bodyMap[worst]].ToString("F6")+" weights="+bw.weight0+","+bw.weight1+","+bw.weight2+","+bw.weight3);
                    Object.DestroyImmediate(baked);
                }
                AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"/Neutral.anim").SampleAnimation(binding.Animator.gameObject,0);
                File.WriteAllLines(Path.Combine(Output,"motion-diagnostic.txt"),motionDiagnostics);
                // A measured import discrepancy must not be mistaken for accepted source fidelity.
                // Capture is diagnostic and the candidate stays excluded from player scenes.
                if(bodyMotionError>=AnimatedReferenceTargetM)
                    Debug.LogWarning("Animated source fidelity remains open: "+bodyMotionError+" metres versus target "+AnimatedReferenceTargetM);
                Require(reader.BaseStream.Position==reader.BaseStream.Length,"Unexpected reference bytes");
            }
            return results.ToArray();
        }
        static int[] VerifyOpaqueMask()
        {
            var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="Temporary opaque-mask check";quad.layer=29;quad.transform.position=new Vector3(12,3,0);
            var cameraObject=new GameObject("Temporary mask camera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
            camera.transform.position=new Vector3(12,3,-2);camera.orthographic=true;camera.orthographicSize=1;camera.cullingMask=1<<29;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.blue;camera.allowHDR=false;camera.allowMSAA=false;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            var mesh=Object.Instantiate(quad.GetComponent<MeshFilter>().sharedMesh);quad.GetComponent<MeshFilter>().sharedMesh=mesh;
            var material=new Material(AssetDatabase.LoadAssetAtPath<Material>(Root+"/Bay coat.mat"));quad.GetComponent<Renderer>().sharedMaterial=material;
            var texture=new Texture2D(256,256,TextureFormat.RGB24,false);var target=new RenderTexture(256,256,24);var prior=RenderTexture.active;var counts=new int[2];
            try
            {
                for(int alpha=0;alpha<2;alpha++)
                {
                    mesh.colors=Enumerable.Repeat(new Color(.25f,.09f,.03f,alpha),mesh.vertexCount).ToArray();
                    camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,256,256),0,0);texture.Apply();
                    counts[alpha]=texture.GetPixels32().Count(p=>p.b<200);
                }
                Require(counts.All(n=>n>15000)&&Mathf.Abs(counts[0]-counts[1])<100,"Bare-surface mask must not change opaque coverage");
            }
            finally{RenderTexture.active=prior;camera.targetTexture=null;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(texture);Object.DestroyImmediate(material);Object.DestroyImmediate(mesh);Object.DestroyImmediate(quad);Object.DestroyImmediate(cameraObject);}
            return counts;
        }
        static void Capture(Camera camera,string path,int width,int height)
        {
            var prior=camera.targetTexture;var active=RenderTexture.active;
            // Repeated explicit renders can share one Editor frame. Without this,
            // the skeleton advances while GPU skinning can retain an earlier pose.
            // Scope it to offline review; live players keep their normal skin policy.
            var skins=Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None);
            var recalculate=skins.Select(s=>s.forceMatrixRecalculationPerRender).ToArray();
            var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){antiAliasing=2};var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            try
            {
                foreach(var skin in skins)skin.forceMatrixRecalculationPerRender=true;
                camera.targetTexture=target;VolumeManager.instance.Update(camera.transform,camera.GetUniversalAdditionalCameraData().volumeLayerMask);
                camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
                File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally{for(int i=0;i<skins.Length;i++)if(skins[i])skins[i].forceMatrixRecalculationPerRender=recalculate[i];camera.targetTexture=prior;RenderTexture.active=active;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);}
        }
        static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
    }
}
