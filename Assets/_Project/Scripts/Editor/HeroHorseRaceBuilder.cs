using System;
using System.Linq;
using BarrelRivals.Core.Stable;
using BarrelRivals.Practice;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace BarrelRivals.Editor
{
    /// <summary>Full-course integration review using the real saved controller and arena.
    /// Kept outside player scenes until character, budget and device gates are qualified.</summary>
    public static class HeroHorseRaceBuilder
    {
        public const string ScenePath=HeroHorseBenchmarkBuilder.Root+"/HeroHorseRaceReview.unity";

        [MenuItem("Barrel Rivals/Development/Build full-course horse review")]
        public static void BuildInEditor()
        {
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())Build();
        }

        public static void Build()
        {
            var race=EditorSceneManager.OpenScene(ReinsLabBuilder.ScenePath);
            var actor=GameObject.Find("Horse proxy").transform;
            var oldChildren=Enumerable.Range(0,actor.childCount).Select(actor.GetChild).ToArray();
            var benchmark=EditorSceneManager.OpenScene(HeroHorseBenchmarkBuilder.ScenePath,OpenSceneMode.Additive);
            var original=benchmark.GetRootGameObjects().Single(g=>g.GetComponent<HorseRigBindings>());
            var clone=Object.Instantiate(original);clone.name="Fitted race horse";
            SceneManager.MoveGameObjectToScene(clone,race);clone.transform.SetParent(actor,false);
            clone.transform.localPosition=Vector3.zero;clone.transform.localRotation=Quaternion.identity;
            var preview=clone.GetComponent<HeroHorseBenchmarkPlayback>();
            var neutralEye=preview.neutralRiderPosition;
            Object.DestroyImmediate(preview);
            EditorSceneManager.CloseScene(benchmark,true);
            SceneManager.SetActiveScene(race);
            foreach(var child in oldChildren)Object.DestroyImmediate(child.gameObject);
            var oldVisual=actor.GetComponent<PracticeHorseVisual>();if(oldVisual)oldVisual.enabled=false;

            var horse=clone.GetComponent<HorseRigBindings>();
            horse.Animator.enabled=false;
            // The saved fitting clip targets only horse paths. A completed rider has
            // another bone named Root, so the importer-only bind helper is unsuitable here.
            AssetDatabase.LoadAssetAtPath<AnimationClip>(HeroHorseBenchmarkBuilder.Root+"/Neutral.anim")
                .SampleAnimation(horse.Animator.gameObject,0);
            var presentation=actor.GetComponent<ReinsHorsePresentation>();
            presentation.ConfigureRig(clone.transform,horse,neutralEye);
            var driver=clone.GetComponent<HeroHorseLocomotion>();driver.source=presentation;driver.targetSpeed=0;
            var attachments=clone.GetComponent<HeroHorseAttachments>();attachments.source=presentation;attachments.reviewSpeed=0;
            attachments.SetRiderView(true);attachments.PoseRig();
            foreach(var part in horse.ModelSpace.GetComponentsInChildren<Transform>())
                if(part.name=="Fitted western saddle")
                    foreach(var renderer in part.GetComponentsInChildren<Renderer>())renderer.shadowCastingMode=ShadowCastingMode.ShadowsOnly;
            horse.Animator.enabled=true;

            var bindings=horse.ModelSpace.GetComponentsInChildren<MeshRenderer>().Select(renderer=>
            {
                StableSlot slot;
                switch(renderer.name)
                {
                    case "Western saddle":slot=StableSlot.Saddle;break;
                    case "Woven saddle pad":slot=StableSlot.Pad;break;
                    case "Fitted left rein":case "Fitted right rein":slot=StableSlot.Reins;break;
                    case "Fitted leather headstall":slot=StableSlot.Headstall;break;
                    case "Glove shell":slot=StableSlot.Gloves;break;
                    default:return null;
                }
                return new StableAppearance.Binding{renderer=renderer,materialIndex=slot==StableSlot.Reins?1:0,slot=slot};
            }).Where(b=>b!=null).ToArray();
            if(bindings.Length!=7 || bindings.Select(b=>b.slot).Distinct().Count()!=5)
                throw new InvalidOperationException("Candidate must retain all five saved cosmetic slots, including both reins/gloves.");
            var appearance=actor.GetComponent<StableAppearance>();
            appearance.Configure(bindings,StableWardrobeArtBuilder.Palettes());appearance.Apply(StableProfile.Starter());

            var cameraRig=Camera.main.GetComponent<RiderCameraRig>();
            if(!cameraRig)cameraRig=Camera.main.gameObject.AddComponent<RiderCameraRig>();
            var view=new SerializedObject(cameraRig);
            view.FindProperty("downwardPitch").floatValue=25;
            view.FindProperty("fieldOfView").floatValue=72;
            view.ApplyModifiedPropertiesWithoutUndo();
            if(clone.GetComponentsInChildren<Collider>(true).Length!=0)throw new InvalidOperationException("Character art cannot introduce race colliders.");
            if(EditorBuildSettings.scenes.Any(s=>s.path==ScenePath))throw new InvalidOperationException("Development review must stay outside player build scenes.");
            EditorSceneManager.SaveScene(race,ScenePath);
            EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("HERO_HORSE_FULL_COURSE_REVIEW_SAVED "+ScenePath);
        }
    }
}
