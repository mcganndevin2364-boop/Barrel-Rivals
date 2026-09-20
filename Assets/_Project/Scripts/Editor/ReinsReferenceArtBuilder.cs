using System;
using System.IO;
using System.Linq;
using BarrelRivals.Practice;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace BarrelRivals.Editor
{
    /// <summary>Reins-only reference art integration. Never owns competitive positions, colliders or timing.</summary>
    public static class ReinsReferenceArtBuilder
    {
        public const string Root = "Assets/_Project/Art/Reins";
        public const string HorsePath = Root + "/Horse/RodeoHorse.fbx";
        public static void Apply(Transform horse, Camera camera)
        {
            Directory.CreateDirectory(Root + "/Materials");
            AssetDatabase.Refresh();
            var dirtTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/ArenaDirtAlbedo.png");
            if (dirtTexture)
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(dirtTexture));
                if (importer.maxTextureSize != 2048 || importer.wrapMode != TextureWrapMode.Repeat || !importer.mipmapEnabled)
                { importer.maxTextureSize = 2048; importer.wrapMode = TextureWrapMode.Repeat; importer.mipmapEnabled = true; importer.anisoLevel = 8; importer.SaveAndReimport(); }
            }
            var dirt = Material("Rodeo clay", new Color(.88f, .83f, .75f), .07f);
            if (dirtTexture && !dirt.mainTexture)
            { dirt.mainTexture = dirtTexture; dirt.mainTextureScale = new Vector2(22, 30); EditorUtility.SetDirty(dirt); }
            var microNormal=AssetDatabase.LoadAssetAtPath<Texture2D>(PracticePresentationBuilder.Root+"/Original dirt normal.asset");
            if(microNormal && !dirt.GetTexture("_BumpMap"))
            {dirt.SetTexture("_BumpMap",microNormal);dirt.SetFloat("_BumpScale",.2f);dirt.EnableKeyword("_NORMALMAP");EditorUtility.SetDirty(dirt);}
            var steel = Material("Galvanized rails", new Color(.48f,.45f,.38f), .36f, .7f);
            var wood = Material("Warm timber", new Color(.21f,.115f,.055f), .15f);
            var roof = Material("Weathered red roof", new Color(.25f,.075f,.037f), .25f, .22f);
            var navy = Material("Arena charcoal", new Color(.042f,.052f,.055f), .18f);
            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (renderer.transform.IsChildOf(horse)) continue;
                var materials = renderer.sharedMaterials;
                for (int i=0;i<materials.Length;i++)
                {
                    if (!materials[i]) continue;
                    switch (materials[i].name)
                    {
                        case "Arena loam": materials[i]=dirt; break;
                        case "Weathered ivory rail": materials[i]=steel; break;
                        case "Aged cedar": materials[i]=wood; break;
                        case "Oxide red roof": materials[i]=roof; break;
                        case "Arena teal": materials[i]=navy; break;
                    }
                }
                renderer.sharedMaterials=materials;
            }
            ConfigureLight(camera);
            BuildCharacter(horse);
            AddArenaDetails();
            AssetDatabase.SaveAssets();
        }
        private static Material Material(string name, Color color, float smoothness, float metallic=0)
        {
            string path=Root+"/Materials/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material)return material; // Authored materials survive regeneration.
            material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};
            material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",smoothness);material.SetFloat("_Metallic",metallic);
            AssetDatabase.CreateAsset(material,path);return material;
        }
        private static void ConfigureLight(Camera camera)
        {
            PlayerSettings.colorSpace=ColorSpace.Linear;
            var sun=RenderSettings.sun;
            if(sun)
            {
                sun.color=new Color(1,.87f,.68f);sun.intensity=1.45f;sun.transform.rotation=Quaternion.Euler(27,-42,0);
                sun.shadowStrength=.82f;sun.shadowBias=.035f;sun.shadowNormalBias=.17f;
            }
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.42f,.55f,.72f);
            RenderSettings.ambientEquatorColor=new Color(.43f,.37f,.28f);
            RenderSettings.ambientGroundColor=new Color(.19f,.13f,.08f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
            RenderSettings.fogStartDistance=80;RenderSettings.fogEndDistance=240;RenderSettings.fogColor=new Color(.66f,.62f,.52f);
            string skyPath=Root+"/Materials/Golden hour sky.mat";
            var sky=AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if(!sky)
            {
                sky=new Material(Shader.Find("Skybox/Procedural"));sky.SetColor("_SkyTint",new Color(.5f,.5f,.5f));
                sky.SetColor("_GroundColor",new Color(.45f,.34f,.21f));sky.SetFloat("_AtmosphereThickness",1.05f);sky.SetFloat("_Exposure",1.15f);
                AssetDatabase.CreateAsset(sky,skyPath);
            }
            RenderSettings.skybox=sky;camera.farClipPlane=250;camera.nearClipPlane=.08f;
            var old=GameObject.Find("Reins reference grade");if(old)Object.DestroyImmediate(old);
            var volume=new GameObject("Reins reference grade").AddComponent<Volume>();volume.isGlobal=true;volume.priority=1;
            const string volumePath=Root+"/Reference grade.asset";
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(volumePath);
            if(!profile)
            {
                profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,volumePath);
                var grade=profile.Add<ColorAdjustments>(true);grade.postExposure.Override(.15f);grade.contrast.Override(14);grade.saturation.Override(3);
                AssetDatabase.AddObjectToAsset(grade,profile);EditorUtility.SetDirty(profile);
            }
            volume.sharedProfile=profile;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        }
        private static void BuildCharacter(Transform horse)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(HorsePath);if(!prefab)return;
            var importer=(ModelImporter)AssetImporter.GetAtPath(HorsePath);
            if(importer.animationType!=ModelImporterAnimationType.Generic || !importer.importAnimation || importer.avatarSetup!=ModelImporterAvatarSetup.CreateFromThisModel)
            {importer.animationType=ModelImporterAnimationType.Generic;importer.importAnimation=true;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.SaveAndReimport();prefab=AssetDatabase.LoadAssetAtPath<GameObject>(HorsePath);}
            var clipSettings=importer.defaultClipAnimations;
            if(clipSettings.Length>0 && importer.clipAnimations.Length==0)
            {foreach(var clip in clipSettings){clip.loopTime=true;clip.loopPose=true;}importer.clipAnimations=clipSettings;importer.SaveAndReimport();prefab=AssetDatabase.LoadAssetAtPath<GameObject>(HorsePath);}
            var prior=horse.Find("Reference horse");if(prior)Object.DestroyImmediate(prior.gameObject);
            foreach(var r in horse.GetComponentsInChildren<Renderer>(true))r.enabled=false;
            var oldVisual=horse.GetComponent<PracticeHorseVisual>();if(oldVisual)oldVisual.enabled=false;
            var model=new GameObject("Reference horse");model.transform.SetParent(horse,false);
            // Retain the FBX conversion/normalization transform under an identity presentation root.
            var imported=(GameObject)PrefabUtility.InstantiatePrefab(prefab,model.transform);
            // This legacy FBX faces -Z after Unity axis conversion; turn art only to canonical +Z.
            imported.transform.localRotation=Quaternion.Euler(0,180,0)*imported.transform.localRotation;
            foreach(var collider in model.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
            var coat=Material("Horse coat",new Color(.62f,.40f,.26f),.32f);
            var albedo=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Horse/HorseAlbedo.png");
            if(albedo && !coat.mainTexture){coat.mainTexture=albedo;coat.SetColor("_BaseColor",Color.white);EditorUtility.SetDirty(coat);}
            var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Horse/HorseNormal.png");
            if(normal && !coat.GetTexture("_BumpMap"))
            {
                var textureImporter=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(normal));
                if(textureImporter.textureType!=TextureImporterType.NormalMap){textureImporter.textureType=TextureImporterType.NormalMap;textureImporter.SaveAndReimport();}
                coat.SetTexture("_BumpMap",normal);coat.SetFloat("_BumpScale",.45f);coat.EnableKeyword("_NORMALMAP");EditorUtility.SetDirty(coat);
            }
            var hair=Material("Horse mane and tail",Color.white,.22f);
            if(!hair.mainTexture) { hair.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Horse/HorseHair.png");hair.SetFloat("_AlphaClip",1);hair.SetFloat("_Cutoff",.42f);hair.SetFloat("_Cull",0);hair.EnableKeyword("_ALPHATEST_ON");hair.renderQueue=2450;EditorUtility.SetDirty(hair); }
            var eye=Material("Horse eyes",Color.white,.65f);if(!eye.mainTexture) { eye.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Horse/HorseEye.png");EditorUtility.SetDirty(eye); }
            foreach(var r in model.GetComponentsInChildren<Renderer>(true))
            {var material=r.name.StartsWith("HorseHair")?hair:r.name.StartsWith("HorseEye")?eye:coat;r.enabled=true;r.sharedMaterials=Enumerable.Repeat(material,r.sharedMaterials.Length).ToArray();r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;}
            var animator=model.GetComponentInChildren<Animator>(true);if(!animator)animator=model.AddComponent<Animator>();animator.applyRootMotion=false;
            var clips=AssetDatabase.LoadAllAssetsAtPath(HorsePath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            if(clips.Length>0)
            {
                string controllerPath=Root+"/Horse/RodeoHorse.controller";
                var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                if(!controller)
                {
                    controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
                    controller.CreateBlendTreeInController("Gait",out var tree);tree.blendParameter="Speed";tree.useAutomaticThresholds=false;
                    var idle=clips.FirstOrDefault(c=>c.name.Contains("Idle"))??clips[0];var walk=clips.FirstOrDefault(c=>c.name.Contains("Walk"))??idle;
                    var gallop=clips.FirstOrDefault(c=>c.name.Contains("Gallop"))??walk;
                    tree.AddChild(idle,0);tree.AddChild(walk,1.5f);tree.AddChild(gallop,8);
                }
                animator.runtimeAnimatorController=controller;
            }
            var seat=new GameObject("Rider seat anchor").transform;seat.SetParent(model.transform,false);seat.localPosition=new Vector3(0,2.3f,-.4f);
            var presentation=horse.GetComponent<ReinsHorsePresentation>();if(!presentation)presentation=horse.gameObject.AddComponent<ReinsHorsePresentation>();
            presentation.Configure(model.transform,seat,animator);
        }
        private static void AddArenaDetails()
        {
            var old=GameObject.Find("Reins arena detail");if(old)Object.DestroyImmediate(old);
            var parent=new GameObject("Reins arena detail").transform;
            ReinsCrowdBuilder.Build(parent);
            var metal=Material("Floodlight steel",new Color(.18f,.19f,.18f),.4f,.75f);
            var ivory=Material("Warm fixture glass",new Color(.95f,.85f,.59f),.2f);
            var timber=Material("Warm timber",new Color(.21f,.115f,.055f),.15f);
            // Additional foreground structures stay outside the legal rideable arena.
            foreach(int side in new[]{-1,1})for(int tower=0;tower<3;tower++)
            {
                float x=side*30,z=3+tower*26;
                Part(parent,"Floodlight pole",new Vector3(x,6.5f,z),new Vector3(.16f,13,.16f),metal);
                Part(parent,"Floodlight gantry",new Vector3(x,12.6f,z),new Vector3(3.3f,.12f,.14f),metal);
                for(int col=0;col<5;col++)for(int row=0;row<2;row++)
                    Part(parent,"Floodlight lens",new Vector3(x-1.2f+col*.6f,12.1f+row*.7f,z-.13f),new Vector3(.42f,.48f,.2f),ivory);
            }
            for(int side=-1;side<=1;side+=2)for(int p=0;p<7;p++)
                Part(parent,"Stand roof crossbeam",new Vector3(side*36,6.5f,3+p*8.5f),new Vector3(12,.16f,.2f),timber);
            // Static props are batched; no runtime colliders or extra real-time lights.
            // Unity build-time static batching handles these static renderers; no transient combined mesh is saved.
        }
        private static void Part(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.position=position;go.transform.localScale=scale;
            Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;go.isStatic=true;
        }
    }
}
