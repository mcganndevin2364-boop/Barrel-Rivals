using System.Collections;
using System.Linq;
using BarrelRivals.Practice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
namespace BarrelRivals.Tests.PlayMode
{
    public sealed class HeroHorseLocomotionTests
    {
        [UnityTest]
        public IEnumerator LiveSpeedBlendKeepsActorAndGripsConnected()
        {
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Project/Development/HeroHorse/HeroHorseBenchmark.unity",new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("Development rig excluded from player scenes.");yield break;
#endif
            yield return null;
            var driver=Object.FindFirstObjectByType<HeroHorseLocomotion>();Assert.That(driver,Is.Not.Null);
            var horse=driver.horse;var tack=driver.attachments;var actor=horse.transform.position;
            var camera=horse.GetComponent<HeroHorseBenchmarkPlayback>();camera.riderView=true;
            var ground=horse.GetComponent<HeroHorseGrounding>();Assert.That(ground,Is.Not.Null);
            var hoof=horse.GetComponentsInChildren<Transform>().Single(t=>t.name=="ForeHoof.L");
            driver.targetSpeed=12;float previous=driver.SmoothedSpeed;var priorHoof=hoof.position;float travel=0;
            for(int i=0;i<70;i++)
            {
                yield return new WaitForSeconds(.02f);
                Assert.That(driver.SmoothedSpeed,Is.GreaterThanOrEqualTo(previous-.0001f));previous=driver.SmoothedSpeed;
                travel+=Vector3.Distance(priorHoof,hoof.position);priorHoof=hoof.position;
                Assert.That(Vector3.Distance(actor,horse.transform.position),Is.LessThan(1e-6f));
                Assert.That(tack.rider.MaximumReachError,Is.LessThan(.01f));
                Assert.That(tack.reviewSpeed,Is.EqualTo(driver.SmoothedSpeed).Within(.0001f));
                Assert.That(ground.MinimumAfter,Is.GreaterThan(-.002f));
                foreach(var grip in new[]{tack.leftGrip,tack.rightGrip})
                {
                    var p=camera.reviewCamera.WorldToViewportPoint(grip.position);
                    Assert.That(p.z,Is.GreaterThan(camera.reviewCamera.nearClipPlane));
                    Assert.That(p.x,Is.InRange(.02f,.98f));Assert.That(p.y,Is.InRange(.01f,.98f));
                }
            }
            Assert.That(driver.SmoothedSpeed,Is.GreaterThan(11.8f));Assert.That(travel,Is.GreaterThan(.5f));
            var clips=horse.Animator.GetCurrentAnimatorClipInfo(0);
            Assert.That(clips.Any(c=>c.clip.name=="Sprint" && c.weight>.95f),Is.True,"Live state must use authored sprint, not a faster walk.");
            driver.targetSpeed=0;
            for(int i=0;i<70;i++)yield return new WaitForSeconds(.02f);
            Assert.That(driver.SmoothedSpeed,Is.LessThan(.1f));
            Assert.That(horse.Animator.GetCurrentAnimatorClipInfo(0).Any(c=>c.clip.name=="Idle" && c.weight>.9f),Is.True);
            Assert.That(horse.Animator.applyRootMotion,Is.False);
            driver.enabled=false;driver.targetSpeed=float.NaN;driver.Advance(10);
            Assert.That(driver.SmoothedSpeed,Is.EqualTo(0).Within(.0001f));
            driver.targetSpeed=14;driver.Advance(float.NaN);
            Assert.That(driver.SmoothedSpeed,Is.EqualTo(0).Within(.0001f));
        }
    }
}
