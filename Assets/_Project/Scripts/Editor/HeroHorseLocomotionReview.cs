using System;
using System.Collections.Generic;
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
        public static void CaptureLocomotion()
        {
            Directory.CreateDirectory(Output);EditorSceneManager.OpenScene(ScenePath);
            var horse=Object.FindFirstObjectByType<HorseRigBindings>();var animator=horse.Animator;
            var driver=horse.GetComponent<HeroHorseLocomotion>();driver.enabled=false;
            var grounding=horse.GetComponent<HeroHorseGrounding>();
            var tack=horse.GetComponent<HeroHorseAttachments>();var camera=Camera.main;
            animator.enabled=false;var actor=horse.transform.position;
            var mesh=new Mesh();var rows=new List<GaitReview>();
            var weights=horse.Body.sharedMesh.boneWeights;
            var hoof=Enumerable.Range(0,weights.Length).Where(i=>weights[i].weight0>.9999f && horse.Body.bones[weights[i].boneIndex0].name.Contains("Hoof.")).ToArray();
            Require(hoof.Length>20,"Missing rigid hoof surface samples");
            try
            {
                for(int g=0;g<GaitNames.Length;g++)
                {
                    var clip=GaitClip(GaitNames[g]);var row=new GaitReview{name=GaitNames[g],seconds=clip.length,minimumHoofY=10};
                    Vector3[] first=null;
                    for(int i=0;i<=32;i++)
                    {
                        clip.SampleAnimation(animator.gameObject,clip.length*i/32);tack.reviewSpeed=GaitSpeeds[g];tack.RenderImmediate();
                        horse.Body.BakeMesh(mesh,true);var vertices=mesh.vertices.Select(v=>horse.Body.transform.TransformPoint(v)).ToArray();
                        row.minimumHoofY=Mathf.Min(row.minimumHoofY,hoof.Min(v=>vertices[v].y));
                        row.maximumReachError=Mathf.Max(row.maximumReachError,tack.rider.MaximumReachError);
                        if(i==0)first=vertices;
                        if(i==32)row.maximumLoopError=vertices.Select((v,j)=>Vector3.Distance(v,first[j])).Max();
                        if(i%8==0 && i<32)
                        {
                            tack.SetRiderView(false);camera.transform.position=new Vector3(-4.1f,2.05f,.2f);camera.transform.LookAt(new Vector3(0,1.2f,0));camera.fieldOfView=43;
                            Capture(camera,Path.Combine(Output,row.name+"-"+i.ToString("D2")+".png"),800,600);
                        }
                    }
                    Require(row.minimumHoofY>-.002f && row.maximumLoopError<.001f && row.maximumReachError<.01f,"Imported gait contact/loop/attachment failure: "+row.name);
                    rows.Add(row);
                }
                animator.enabled=true;animator.Rebind();animator.Update(0);
                var playback=horse.GetComponent<HeroHorseBenchmarkPlayback>();float maximumStep=0,maximumReach=0,minimumBlendHoofY=10,minimumBeforeGrounding=10,maximumGroundCorrection=0;int frames=0;
                foreach(bool riderView in new[]{false,true})
                {
                    var folder=Path.Combine(Output,riderView?"frames-rider":"frames-side");Directory.CreateDirectory(folder);
                    driver.targetSpeed=0;driver.Advance(10);animator.Play("Locomotion",0,0);animator.Update(0);
                    Vector3 previous=horse.MotionRoot.position;
                    for(int i=0;i<210;i++)
                    {
                        driver.targetSpeed=new[]{0f,1.5f,3.5f,6f,12f,14f,0f}[i/30];driver.Advance(1f/30);animator.Update(1f/30);
                        grounding.RenderImmediate();minimumBeforeGrounding=Mathf.Min(minimumBeforeGrounding,grounding.MinimumBefore);maximumGroundCorrection=Mathf.Max(maximumGroundCorrection,grounding.MaximumCorrection);
                        tack.leftPull=.15f;tack.rightPull=.1f;tack.SetRiderView(riderView);tack.RenderImmediate();
                        maximumStep=Mathf.Max(maximumStep,Vector3.Distance(previous,horse.MotionRoot.position));previous=horse.MotionRoot.position;
                        maximumReach=Mathf.Max(maximumReach,tack.rider.MaximumReachError);
                        horse.Body.BakeMesh(mesh,true);var blendVertices=mesh.vertices;
                        minimumBlendHoofY=Mathf.Min(minimumBlendHoofY,hoof.Min(v=>horse.Body.transform.TransformPoint(blendVertices[v]).y));
                        if(riderView){camera.transform.position=horse.FollowSupportPoint(playback.neutralRiderPosition);camera.transform.rotation=horse.ModelSpace.rotation*Quaternion.Euler(playback.riderPitch,0,0);camera.fieldOfView=playback.riderFieldOfView;}
                        else{camera.transform.position=new Vector3(-4.1f,2.05f,.2f);camera.transform.LookAt(new Vector3(0,1.2f,0));camera.fieldOfView=43;}
                        Capture(camera,Path.Combine(folder,(i+1).ToString("D3")+".png"),800,600);frames++;
                        Require(Vector3.Distance(horse.transform.position,actor)<1e-6f,"Locomotion moved the actor");
                    }
                }
                Require(maximumStep<.04f && maximumReach<.01f,"Transition discontinuity or rider reach failure");
                Require(minimumBlendHoofY>-.002f,"Corrected blend still penetrates the floor");
                File.WriteAllText(Path.Combine(Output,"locomotion-review.json"),JsonUtility.ToJson(new LocomotionReview{gaits=rows.ToArray(),frames=frames,maximumRootStepAt30Hz=maximumStep,maximumReachError=maximumReach,minimumBlendHoofY=minimumBlendHoofY,blendGroundTargetMet=minimumBlendHoofY>-.002f,minimumBeforeGrounding=minimumBeforeGrounding,maximumGroundCorrection=maximumGroundCorrection},true)+"\n");
                Debug.Log("HERO_LOCOMOTION_REVIEWED "+Output);
            }
            finally{Object.DestroyImmediate(mesh);}
        }
        [Serializable] class GaitReview{public string name;public float seconds,minimumHoofY,maximumLoopError,maximumReachError;}
        [Serializable] class LocomotionReview
        {
            public GaitReview[] gaits;public int frames;public float maximumRootStepAt30Hz,maximumReachError,minimumBlendHoofY,minimumBeforeGrounding,maximumGroundCorrection;public bool blendGroundTargetMet;
            public string scope="Actual Unity in-place gait and speed-blend study, static floor. 33 samples per clip, 420 controlled frames at 30 Hz. No production foot planting, full collision, Core connection, native or phone acceptance.";
        }
    }
}
