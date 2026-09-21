using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BarrelRivals.Practice;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace BarrelRivals.Editor
{
    public static partial class HeroHorseBenchmarkBuilder
    {
        public const string ScenePath=Root+"/HeroHorseBenchmark.unity";
        [MenuItem("Barrel Rivals/Development/Open fitted horse benchmark")]
        public static void Open()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            if(!File.Exists(ScenePath))Build();
            EditorSceneManager.OpenScene(ScenePath);
        }
        [MenuItem("Barrel Rivals/Development/Build fitted horse benchmark")]
        public static void BuildInEditor()
        {
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())Build();
        }
        public static void Build()
        {
            AssetDatabase.Refresh();
            Directory.CreateDirectory(Output);
            // FBX subassets are configured through ModelImporter, never edited in place.
            var importer=(ModelImporter)AssetImporter.GetAtPath(ModelPath);
            importer.skinWeights=ModelImporterSkinWeights.Custom;importer.maxBonesPerVertex=4;importer.minBoneWeight=0.0000001f;
            var clips=importer.defaultClipAnimations;
            foreach(var clip in clips){clip.loopTime=true;clip.loopPose=false;clip.keepOriginalPositionY=true;clip.keepOriginalPositionXZ=true;}
            importer.clipAnimations=clips;importer.SaveAndReimport();

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var horse=new GameObject("Hero horse benchmark");
            var model=new GameObject("Horse model space");model.transform.SetParent(horse.transform,false);
            var imported=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath),model.transform);
            // Measured import faces -Z. Rotate art only to the +Z game convention.
            imported.transform.localRotation=Quaternion.Euler(0,180,0)*imported.transform.localRotation;
            var animator=imported.GetComponent<Animator>();animator.enabled=false;
            RestoreBindPose(model.transform);
            var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>();
            var body=skins.Single(r=>r.name=="HeroHorseBody");
            var eyes=skins.Single(r=>r.name=="HeroHorseEyes");
            var groom=skins.Single(r=>r.name=="HeroHorseGroom");
            var nodes=model.GetComponentsInChildren<Transform>();
            Transform Bone(string name)=>nodes.Single(t=>t.name==name);
            var bindings=horse.AddComponent<HorseRigBindings>();
            bindings.Configure(model.transform,Bone("Root"),Bone("Spine"),Bone("Head"),Bone("TailBase"),Bone("TailTip"),body,eyes,groom,animator);
            if(model.transform.InverseTransformPoint(bindings.Head.position).z<.8f)throw new InvalidOperationException("Horse must face +Z.");
            var neutral=MakeNeutralClip(animator,skins.SelectMany(s=>s.bones).Distinct().Concat(new[]{bindings.MotionRoot}).Distinct().ToArray());
            var walk=AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));

            Materials(body,eyes,groom);
            HeroHorseCoatBuilder.Apply(body,model.transform);
            HeroHorseTackFitter.Build(bindings);
            HeroHorseRiderBuilder.Build(bindings);
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Root+"/HorseBenchmark.controller");
            if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(Root+"/HorseBenchmark.controller");
            var machine=controller.layers[0].stateMachine;
            foreach(var state in machine.states){machine.RemoveState(state.state);if(state.state)Object.DestroyImmediate(state.state,true);}
            var walkState=machine.AddState("Walk");walkState.motion=walk;machine.defaultState=walkState;
            var idleState=machine.AddState("Neutral fitting pose");idleState.motion=neutral;
            ConfigureLocomotion(controller,bindings);
            animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.enabled=true;
            foreach(var s in skins){s.quality=SkinQuality.Bone4;s.updateWhenOffscreen=true;s.shadowCastingMode=ShadowCastingMode.On;}
            var camera=new GameObject("Horse review camera",typeof(Camera)).GetComponent<Camera>();
            camera.tag="MainCamera";camera.nearClipPlane=.04f;camera.farClipPlane=250;camera.fieldOfView=43;
            camera.transform.position=new Vector3(3.8f,2.8f,4.3f);camera.transform.LookAt(new Vector3(0,1.45f,0));
            Lighting(camera);
            var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.name="Benchmark arena surface";
            Object.DestroyImmediate(ground.GetComponent<Collider>());ground.transform.localScale=new Vector3(6,1,6);
            var floorMesh=Object.Instantiate(ground.GetComponent<MeshFilter>().sharedMesh);floorMesh.uv=floorMesh.uv.Select(uv=>uv*20).ToArray();ground.GetComponent<MeshFilter>().sharedMesh=PersistentMeshAsset.Save(floorMesh,Root+"/Benchmark floor.asset");
            ground.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(ReinsPremiumArenaBuilder.Root+"/Materials/Arena soil.mat");
            var motion=horse.AddComponent<HeroHorseBenchmarkPlayback>();motion.horse=bindings;motion.reviewCamera=camera;
            motion.neutralRiderPosition=model.transform.InverseTransformPoint(model.GetComponentsInChildren<Transform>().Single(t=>t.name=="Fitted saddle horn").position)+new Vector3(0,.60f,-.20f);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath);
            File.WriteAllText(Path.Combine(Output,"build.json"),JsonUtility.ToJson(new BuildReport{bodyVertices=body.sharedMesh.vertexCount,horseTriangles=skins.Sum(s=>s.sharedMesh.triangles.Length/3),horseSlots=skins.Sum(s=>s.sharedMaterials.Length),bones=nodes.Count(t=>t.IsChildOf(bindings.MotionRoot)||t==bindings.MotionRoot),walkSeconds=walk.length},true));
            Debug.Log("HORSE_BENCHMARK_BUILT "+ScenePath);
        }
        [Serializable] class BuildReport {public int bodyVertices,horseTriangles,horseSlots,bones;public float walkSeconds;}
        public static void RestoreBindPose(Transform model)
        {
            var skins=model.GetComponentsInChildren<SkinnedMeshRenderer>();
            var targets=new Dictionary<Transform,Matrix4x4>();
            foreach(var skin in skins)
                for(int i=0;i<skin.bones.Length;i++)
                    if(!targets.ContainsKey(skin.bones[i]))targets.Add(skin.bones[i],skin.localToWorldMatrix*skin.sharedMesh.bindposes[i].inverse);
            var root=model.GetComponentsInChildren<Transform>().Single(t=>t.name=="Root");
            // This source's non-deforming root is not guaranteed to be in a skin palette.
            // Its neutral source head is (0,-.35,0) metres, mapping to canonical (0,0,-.35).
            root.position=model.TransformPoint(new Vector3(0,0,-.35f));
            foreach(var pair in targets.OrderBy(p=>Depth(p.Key)))
            {pair.Key.position=pair.Value.GetColumn(3);pair.Key.rotation=pair.Value.rotation;}
        }
        static int Depth(Transform t){int n=0;for(;t;t=t.parent)n++;return n;}
        static AnimationClip MakeNeutralClip(Animator animator,Transform[] bones)
        {
            var clip=new AnimationClip{name="Neutral fitting pose",frameRate=30};
            foreach(var bone in bones)
            {
                string path=AnimationUtility.CalculateTransformPath(bone,animator.transform);
                var p=bone.localPosition;var q=bone.localRotation;var s=bone.localScale;
                foreach(var entry in new[]{("m_LocalPosition.x",p.x),("m_LocalPosition.y",p.y),("m_LocalPosition.z",p.z),("m_LocalRotation.x",q.x),("m_LocalRotation.y",q.y),("m_LocalRotation.z",q.z),("m_LocalRotation.w",q.w),("m_LocalScale.x",s.x),("m_LocalScale.y",s.y),("m_LocalScale.z",s.z)})
                    AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),entry.Item1),AnimationCurve.Constant(0,28f/30,entry.Item2));
            }
            clip.EnsureQuaternionContinuity();var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"/Neutral.anim");
            if(old){EditorUtility.CopySerialized(clip,old);Object.DestroyImmediate(clip);return old;}
            AssetDatabase.CreateAsset(clip,Root+"/Neutral.anim");return clip;
        }
        static void Materials(SkinnedMeshRenderer body,SkinnedMeshRenderer eyes,SkinnedMeshRenderer groom)
        {
            var surface=Shader.Find("Barrel Rivals/Horse Surface");if(!surface)throw new InvalidOperationException("Horse surface shader missing");
            var coat=Material("Bay coat",surface);coat.SetColor("_Tint",Color.white);coat.SetFloat("_CoatSmoothness",.42f);coat.SetFloat("_BareSmoothness",.48f);coat.SetFloat("_Reflectance",.026f);coat.SetFloat("_MicroNormal",.02f);coat.SetVector("_MicroScale",new Vector4(400,100,0,0));coat.SetFloat("_CoatSheen",.65f);coat.SetFloat("_CoatVariation",.055f);
            var eye=Material("Dark eyes",surface);eye.SetColor("_Tint",Color.white);eye.SetFloat("_CoatSmoothness",.90f);eye.SetFloat("_BareSmoothness",.90f);eye.SetFloat("_MicroNormal",0);eye.SetFloat("_Reflectance",.03f);
            eye.SetFloat("_CoatSheen",0);eye.SetFloat("_CoatVariation",0);
            body.sharedMaterial=coat;eyes.sharedMaterial=eye;
            var fiber=Shader.Find("Barrel Rivals/Horse Fiber");var hairMaterials=new Material[2];
            for(int i=0;i<2;i++)
            {
                var m=Material(i==0?"Dense mane":"Outer strands",fiber);
                m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Reins/Hair/"+(i==0?"Original natural strand atlas.png":"Original separated strand atlas.png")));
                m.SetColor("_BaseColor",i==0?new Color(.52f,.49f,.45f):new Color(.36f,.33f,.30f));
                m.SetColor("_FiberTint",new Color(.48f,.34f,.23f));m.SetFloat("_Cutoff",.36f);m.SetFloat("_Cull",0);m.SetFloat("_AlphaToMask",1);m.EnableKeyword("_ALPHATEST_ON");m.renderQueue=2450;
                m.SetFloat("_PrimaryStrength",.045f);m.SetFloat("_SecondaryStrength",.025f);hairMaterials[i]=m;
            }
            groom.sharedMaterials=hairMaterials;
            var mesh=Object.Instantiate(groom.sharedMesh);mesh.name="Fitted groom with strand progress";
            mesh.uv2=mesh.uv.Select(uv=>new Vector2(0,Mathf.Clamp01((.969f-uv.y)/.939f))).ToArray();
            groom.sharedMesh=PersistentMeshAsset.Save(mesh,Root+"/Groom mesh.asset");
            foreach(var m in new[]{coat,eye}.Concat(hairMaterials))EditorUtility.SetDirty(m);
        }
        static void Lighting(Camera camera)
        {
            RenderSettings.skybox=AssetDatabase.LoadAssetAtPath<Material>(ReinsPremiumArenaBuilder.Root+"/Materials/Photographic dusk sky.mat");
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.58f,.65f,.76f);RenderSettings.ambientEquatorColor=new Color(.50f,.45f,.38f);RenderSettings.ambientGroundColor=new Color(.26f,.20f,.14f);
            RenderSettings.reflectionIntensity=.65f;
            var sun=new GameObject("Benchmark arena sun",typeof(Light)).GetComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(1,.90f,.76f);sun.intensity=2.5f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(32,-32,0);RenderSettings.sun=sun;
            var volume=new GameObject("Benchmark arena grade",typeof(Volume)).GetComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(ReinsPremiumArenaBuilder.Root+"/Premium dusk grade.asset");
            camera.allowHDR=true;var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
        }
        static Material Material(string name,Shader shader)
        {string path=Root+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(shader){name=name};AssetDatabase.CreateAsset(m,path);}else m.shader=shader;return m;}
    }
}
