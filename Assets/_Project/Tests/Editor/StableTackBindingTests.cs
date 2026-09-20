using System;
using System.Linq;
using BarrelRivals.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace BarrelRivals.Tests
{
    public sealed class StableTackBindingTests
    {
        [Test]
        public void PersistedSaddleFollowsSampledTorsoMotionWithoutMovingTheHorseRoot()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(StableTackBuilder.Root+"/Stable horse.prefab");
            Assert.IsNotNull(prefab,"Regenerate the stable after importing the torso-driven gait.");
            var horse=Object.Instantiate(prefab);
            try
            {
                horse.transform.SetPositionAndRotation(new Vector3(3,.2f,-2),Quaternion.Euler(0,31,0));
                var model=horse.transform.Find("Reference horse");
                Assert.IsNotNull(model);
                var nodes=model.GetComponentsInChildren<Transform>(true);
                var saddles=nodes.Where(t=>t.name=="Western saddle").ToArray();
                Assert.AreEqual(1,saddles.Length,"Regeneration must not retain the earlier direct-child saddle.");
                var saddle=saddles[0];var torso=nodes.Single(t=>t.name=="Bone");
                Assert.AreSame(torso,saddle.parent,"The saddle must follow the torso, not the neck or static presentation root.");
                AssertMatrix(Matrix4x4.identity,model.worldToLocalMatrix*saddle.localToWorldMatrix,
                    "Reparenting must preserve the authored normalized bind pose.");
                var animator=horse.GetComponentInChildren<Animator>(true);
                Assert.IsNotNull(animator);animator.enabled=false;animator.applyRootMotion=false;
                var gallop=AssetDatabase.LoadAllAssetsAtPath(ReinsReferenceArtBuilder.HorsePath).OfType<AnimationClip>()
                    .Single(c=>!c.name.StartsWith("__preview__",StringComparison.Ordinal) && c.name.EndsWith("|Gallop",StringComparison.Ordinal));
                var horsePose=horse.transform.localToWorldMatrix;
                var relative=torso.worldToLocalMatrix*saddle.localToWorldMatrix;
                Vector3 firstTorso=Vector3.zero,firstSeat=Vector3.zero;
                float torsoTravel=0,seatTravel=0;
                for(int frame=0;frame<8;frame++)
                {
                    gallop.SampleAnimation(animator.gameObject,gallop.length*frame/8f);
                    AssertMatrix(relative,torso.worldToLocalMatrix*saddle.localToWorldMatrix,
                        "The saddle slipped relative to the sampled body pose.");
                    AssertMatrix(horsePose,horse.transform.localToWorldMatrix,"Art animation moved the authoritative horse root.");
                    var seat=saddle.TransformPoint(new Vector3(0,1.8f,-.31f));
                    if(frame==0){firstTorso=torso.position;firstSeat=seat;}
                    torsoTravel=Mathf.Max(torsoTravel,Vector3.Distance(firstTorso,torso.position));
                    seatTravel=Mathf.Max(seatTravel,Vector3.Distance(firstSeat,seat));
                }
                Assert.That(torsoTravel,Is.GreaterThan(.005f),"The imported gait must actually translate the torso bone.");
                Assert.That(seatTravel,Is.GreaterThan(.005f),"Visible saddle geometry must follow the moving torso.");
            }
            finally{Object.DestroyImmediate(horse);}
        }

        private static void AssertMatrix(Matrix4x4 expected,Matrix4x4 actual,string message)
        {
            for(int i=0;i<16;i++)Assert.That(actual[i],Is.EqualTo(expected[i]).Within(.0005f),message+" Matrix element "+i);
        }
    }
}
