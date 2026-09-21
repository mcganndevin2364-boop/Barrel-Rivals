using System;
using System.IO;
using System.Linq;
using BarrelRivals.Practice;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace BarrelRivals.Editor
{
    /// <summary>Imports credited rider art and connects a cosmetic seated skeleton.</summary>
    public static class ReinsRiderBodyBuilder
    {
        public const string Root="Assets/_Project/Art/Reins/Rider";
        public static ReinsRiderBodyPresentation Build(Transform horse,bool firstPerson=true)
        {
            AssetDatabase.Refresh();
            string path=Root+"/WesternRider.fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            if(importer.animationType!=ModelImporterAnimationType.Generic || importer.importAnimation || !importer.isReadable)
            {importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.importAnimation=false;importer.isReadable=true;importer.SaveAndReimport();}
            var model=horse.Find("Reference horse");
            if(!model)throw new InvalidOperationException("The horse must exist before its rider.");
            var old=model.Find("Western rider");if(old)Object.DestroyImmediate(old.gameObject);
            foreach(var anchor in model.GetComponentsInChildren<Transform>(true).Where(t=>new[]{"Rider pelvis anchor","Left rider stirrup target","Right rider stirrup target","Rider left wrist target","Rider right wrist target"}.Contains(t.name)).ToArray())Object.DestroyImmediate(anchor.gameObject);
            var container=new GameObject("Western rider").transform;container.SetParent(model,false);
            var rider=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),container);
            foreach(var animator in rider.GetComponentsInChildren<Animator>(true))Object.DestroyImmediate(animator);
            Transform Bone(string name)=>rider.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
            // Normalize standing height, then determine facing from the feet, not a guessed FBX axis.
            container.localScale=Vector3.one*1.10f;
            var direction=model.InverseTransformVector(Bone("ball_l").position-Bone("foot_l").position);
            if(direction.z<0)container.localRotation=Quaternion.Euler(0,180,0);
            var skins=rider.GetComponentsInChildren<SkinnedMeshRenderer>();
            if(skins.Length!=6)throw new InvalidOperationException("The rider must import as six skinned surfaces; a static import cannot follow its bones.");
            foreach(var renderer in skins)
            {
                string name=renderer.name;
                Material material=name.Contains("male_casualsuit")?Pbr("Rider shirt and jeans","male_casualsuit03_diffuse.png","male_casualsuit03_normal.png",.16f)
                    :name.Contains("ankle_boots")?Pbr("Rider brown boots","BootsAnkleM.png","BootsAnkle-norm.png",.23f)
                    :name.Contains("low-poly")?Pbr("Rider eyes","brown_eye.png",null,.55f)
                    :name.Contains("ponytail")?Pbr("Rider ponytail","ponytail01_diffuse.png",null,.12f)
                    :name.Contains("hat")?Pbr("Rider felt hat",null,null,.08f)
                    :Pbr("Rider skin","young_lightskinned_female_diffuse.png",null,.22f);
                if(name.Contains("hat"))material.SetColor("_BaseColor",new Color(.15f,.075f,.035f));
                if(name.Contains("ponytail"))
                {material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.4f);material.SetFloat("_Cull",0);material.EnableKeyword("_ALPHATEST_ON");material.renderQueue=2450;}
                EditorUtility.SetDirty(material);renderer.sharedMaterials=new[]{material};renderer.updateWhenOffscreen=true;
                renderer.quality=SkinQuality.Bone4;
                bool headPart=!name.Contains("casualsuit") && !name.Contains("boots");
                renderer.shadowCastingMode=firstPerson && headPart?ShadowCastingMode.ShadowsOnly:ShadowCastingMode.On;
            }
            WesternHatBuilder.Apply(skins,model);
            var nodes=model.GetComponentsInChildren<Transform>(true);
            var horn=nodes.Single(t=>t.name=="Saddle horn anchor");var saddle=horn.parent;
            var hornPosition=model.InverseTransformPoint(horn.position);
            Transform Anchor(string name,Vector3 position)
            {var a=new GameObject(name).transform;a.SetParent(model,false);a.localPosition=position;a.SetParent(saddle,true);return a;}
            var seat=Anchor("Rider pelvis anchor",hornPosition+new Vector3(0,-.09f,-.33f));
            // Initial evaluated contact targets. The study reports reach error before adoption.
            var leftFoot=Anchor("Left rider stirrup target",new Vector3(-.50f,1.38f,hornPosition.z-.20f));
            var rightFoot=Anchor("Right rider stirrup target",new Vector3(.50f,1.38f,hornPosition.z-.20f));
            var leftHand=nodes.Single(t=>t.name=="Left rider grip");var rightHand=nodes.Single(t=>t.name=="Right rider grip");
            // Earlier hand anchors predated the saddle's measured forward-origin correction.
            var hands=new SerializedObject(horse.GetComponentInChildren<ReinsRiderTackPresentation>());
            leftHand.localPosition=new Vector3(-.28f,2.17f,hornPosition.z+.165f);
            rightHand.localPosition=new Vector3(.28f,2.17f,hornPosition.z+.165f);
            hands.FindProperty("leftRest").vector3Value=leftHand.localPosition;
            hands.FindProperty("rightRest").vector3Value=rightHand.localPosition;hands.ApplyModifiedPropertiesWithoutUndo();
            foreach(var hand in new[]{leftHand,rightHand})
                hand.Find("Sleeve and glove stitching").GetComponent<Renderer>().enabled=false;
            var wristLeft=new GameObject("Rider left wrist target").transform;wristLeft.SetParent(leftHand,false);wristLeft.localPosition=new Vector3(0,0,-.035f);
            var wristRight=new GameObject("Rider right wrist target").transform;wristRight.SetParent(rightHand,false);wristRight.localPosition=new Vector3(0,0,-.035f);
            ReinsRiderBodyPresentation.Limb Limb(string suffix,bool arm,Transform target,int side)=>new ReinsRiderBodyPresentation.Limb{
                upper=Bone((arm?"upperarm_":"thigh_")+suffix),lower=Bone((arm?"lowerarm_":"calf_")+suffix),end=Bone((arm?"hand_":"foot_")+suffix),target=target,
                pole=new Vector3(side*(arm?.6f:.65f),arm?2.15f:1.95f,hornPosition.z+(arm?-.4f:-.15f))};
            bool leftIsNegative=model.InverseTransformPoint(Bone("thigh_l").position).x<0;
            string l=leftIsNegative?"l":"r",r=leftIsNegative?"r":"l";
            var body=container.gameObject.AddComponent<ReinsRiderBodyPresentation>();
            body.Configure(horse.GetComponent<ReinsHorsePresentation>(),Bone("pelvis"),seat,model,
                rider.GetComponentsInChildren<Transform>(true).Where(t=>t!=rider.transform).ToArray(),
                Limb(l,true,wristLeft,-1),Limb(r,true,wristRight,1),Limb(l,false,leftFoot,-1),Limb(r,false,rightFoot,1));
            // Stabilized bodycam stays forward of the torso and below the hidden head.
            // Control pads, camera yaw, motion envelope and Core origin are unchanged.
            model.Find("Rider seat anchor").localPosition=new Vector3(0,2.42f,.35f);
            body.RenderImmediate();AssetDatabase.SaveAssets();return body;
        }

        private static Material Pbr(string name,string color,string normal,float smoothness)
        {
            var path=Root+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};AssetDatabase.CreateAsset(material,path);}
            Texture2D Texture(string file,bool isNormal)
            {
                if(file==null)return null;
                var p=Root+"/"+file;var importer=(TextureImporter)AssetImporter.GetAtPath(p);
                var type=isNormal?TextureImporterType.NormalMap:TextureImporterType.Default;
                if(importer.maxTextureSize!=2048 || importer.textureType!=type)
                {importer.maxTextureSize=2048;importer.textureType=type;importer.mipmapEnabled=true;importer.SaveAndReimport();}
                return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            }
            material.SetTexture("_BaseMap",Texture(color,false));material.SetColor("_BaseColor",Color.white);
            material.SetFloat("_Smoothness",smoothness);material.SetFloat("_Metallic",0);
            if(normal!=null){material.SetTexture("_BumpMap",Texture(normal,true));material.SetFloat("_BumpScale",.5f);material.EnableKeyword("_NORMALMAP");}
            EditorUtility.SetDirty(material);return material;
        }

        public static void CaptureStudy()
        {
            string output=Environment.GetEnvironmentVariable("BARREL_RIDER_STUDY_DIRECTORY");
            if(string.IsNullOrEmpty(output))throw new InvalidOperationException("Set a new BARREL_RIDER_STUDY_DIRECTORY.");
            Directory.CreateDirectory(output);
            if(Directory.GetFiles(output,"*.png").Length>0)throw new InvalidOperationException("Use a fresh study folder.");
            EditorSceneManager.OpenScene(ReinsLabBuilder.ScenePath);
            var horse=GameObject.Find("Horse proxy").transform;horse.position=Vector3.zero;horse.rotation=Quaternion.identity;
            var source=horse.GetComponent<ReinsHorsePresentation>();
            var body=Build(horse,false);var tack=horse.GetComponentInChildren<ReinsRiderTackPresentation>();
            source.ResetFrame(new HorsePresentationFrame(0,Vector3.zero,Quaternion.identity,0,0,0,0,false,false));
            tack.RenderImmediate();body.RenderImmediate();
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.enabled=false;
            var camera=Camera.main;var cameraRig=camera.GetComponent<RiderCameraRig>();if(cameraRig)cameraRig.enabled=false;camera.fieldOfView=38;
            var target=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);camera.targetTexture=target;
            bool oldAsync=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            var report=new System.Text.StringBuilder();report.AppendLine("maximumReachError="+body.MaximumReachError);
            foreach(var t in body.GetComponentsInChildren<Transform>())if(new[]{"pelvis","upperarm_l","lowerarm_l","hand_l","thigh_l","calf_l","foot_l","head"}.Contains(t.name))report.AppendLine(t.name+"="+t.position.ToString("F4"));
            var horseSkin=horse.GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.name=="HorseBody");
            var baked=new Mesh();horseSkin.BakeMesh(baked,true);
            foreach(float y in new[]{1.3f,1.5f,1.7f,1.8f})
            {
                var points=baked.vertices.Select(v=>horse.InverseTransformPoint(horseSkin.transform.TransformPoint(v))).Where(v=>Mathf.Abs(v.y-y)<.08f && Mathf.Abs(v.z-.35f)<.22f).ToArray();
                if(points.Length>0)report.AppendLine("horse half width at "+y+"="+points.Max(v=>Mathf.Abs(v.x)));
            }
            Object.DestroyImmediate(baked);
            File.WriteAllText(Path.Combine(output,"fit.txt"),report.ToString());
            try
            {
                var positions=new[]{new Vector3(-4,2.8f,3.6f),new Vector3(-4.6f,2.4f,0),new Vector3(0,2.42f,.35f)};
                for(int i=0;i<positions.Length;i++)
                {
                    camera.transform.position=positions[i];
                    if(i<2)camera.transform.LookAt(new Vector3(0,1.5f,0));else
                    {
                        camera.transform.rotation=Quaternion.Euler(5,0,0);camera.fieldOfView=65;
                        foreach(var renderer in body.GetComponentsInChildren<Renderer>())
                            if(!renderer.name.Contains("casualsuit") && !renderer.name.Contains("boots"))renderer.shadowCastingMode=ShadowCastingMode.ShadowsOnly;
                    }
                    camera.Render();camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
                    File.WriteAllBytes(Path.Combine(output,"rider-"+i+".png"),image.EncodeToPNG());
                }
            }
            finally{ShaderUtil.allowAsyncCompilation=oldAsync;camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(target);Object.DestroyImmediate(image);}
            // Deliberately do not save the race scene before visual review.
        }
    }
}
