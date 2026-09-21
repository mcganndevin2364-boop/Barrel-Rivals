using System;
using System.Linq;
using BarrelRivals.Practice;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BarrelRivals.Editor
{
    public static partial class HeroHorseBenchmarkBuilder
    {
        public static readonly string[] GaitNames={"Idle","Walk","Trot","Gallop","Sprint"};
        public static readonly float[] GaitSpeeds={0,1.5f,3.5f,6,12};
        public static AnimationClip GaitClip(string name)=>AssetDatabase.LoadAllAssetsAtPath(
            name=="Walk"?ModelPath:Root+"/Locomotion/HeroHorse-"+name+".fbx")
            .OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__"));

        static void ConfigureLocomotion(AnimatorController controller,HorseRigBindings horse)
        {
            foreach(string name in GaitNames.Where(n=>n!="Walk"))
            {
                var importer=(ModelImporter)AssetImporter.GetAtPath(Root+"/Locomotion/HeroHorse-"+name+".fbx");
                if(!importer)throw new InvalidOperationException("Missing authored gait: "+name);
                importer.animationType=ModelImporterAnimationType.Generic;
                importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation=true;importer.animationCompression=ModelImporterAnimationCompression.Off;
                importer.resampleCurves=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;
                importer.importCameras=false;importer.importLights=false;
                var clips=importer.defaultClipAnimations;
                foreach(var clip in clips){clip.name=name;clip.loopTime=true;clip.loopPose=false;clip.keepOriginalPositionY=true;clip.keepOriginalPositionXZ=true;}
                importer.clipAnimations=clips;importer.SaveAndReimport();
                foreach(var binding in AnimationUtility.GetCurveBindings(GaitClip(name)))
                    if(binding.type==typeof(Transform) && !string.IsNullOrEmpty(binding.path) && !horse.Animator.transform.Find(binding.path))
                        throw new InvalidOperationException("Unbound gait transform: "+name+" "+binding.path);
            }
            foreach(var old in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(controller)).OfType<BlendTree>())
                UnityEngine.Object.DestroyImmediate(old,true);
            controller.parameters=new[]{new AnimatorControllerParameter{name="Speed",type=AnimatorControllerParameterType.Float,defaultFloat=1.5f},
                new AnimatorControllerParameter{name="StrideRate",type=AnimatorControllerParameterType.Float,defaultFloat=1}};
            var tree=new BlendTree{name="Authored horse gaits",blendType=BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};
            AssetDatabase.AddObjectToAsset(tree,controller);
            for(int i=0;i<GaitNames.Length;i++)tree.AddChild(GaitClip(GaitNames[i]),GaitSpeeds[i]);
            var machine=controller.layers[0].stateMachine;
            var state=machine.AddState("Locomotion");state.motion=tree;state.speedParameter="StrideRate";state.speedParameterActive=true;machine.defaultState=state;
            EditorUtility.SetDirty(controller);
            var driver=horse.gameObject.AddComponent<HeroHorseLocomotion>();driver.horse=horse;driver.attachments=horse.GetComponent<HeroHorseAttachments>();
            var grounding=horse.gameObject.AddComponent<HeroHorseGrounding>();grounding.model=horse.ModelSpace;
            var nodes=horse.ModelSpace.GetComponentsInChildren<Transform>();
            var mesh=horse.Body.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;
            grounding.legs=(from family in new[]{"Fore","Hind"} from side in new[]{"L","R"} select (family,side)).Select(pair=>
            {
                Transform Bone(string name)=>nodes.Single(t=>t.name==pair.family+name+"."+pair.side);
                var hoof=Bone("Hoof");int index=Array.IndexOf(horse.Body.bones,hoof);
                var sole=Enumerable.Range(0,vertices.Length).Where(i=>weights[i].boneIndex0==index && weights[i].weight0>.99999f)
                    .Select(i=>mesh.bindposes[index].MultiplyPoint3x4(vertices[i])).ToArray();
                if(sole.Length==0)throw new InvalidOperationException("Missing actual rigid hoof vertices");
                return new HeroHorseGrounding.Leg{upper=Bone(pair.family=="Fore"?"arm":"Shin"),lower=Bone("Cannon"),pastern=Bone("Pastern"),hoof=hoof,
                    rigidVerticesInHoof=sole,poleInModel=pair.family=="Fore"?Vector3.forward:Vector3.back};
            }).ToArray();
        }
    }
}
