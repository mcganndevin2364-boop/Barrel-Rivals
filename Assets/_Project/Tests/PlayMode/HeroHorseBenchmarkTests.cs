using System.Collections;
using System.IO;
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
    public sealed class HeroHorseBenchmarkTests
    {
        [UnityTest]
        public IEnumerator SavedRigAdvancesOffscreenAndCameraIncludesInheritedMotion()
        {
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Project/Development/HeroHorse/HeroHorseBenchmark.unity",new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("The development benchmark is deliberately excluded from player scenes.");yield break;
#endif
            yield return null;
            var binding=Object.FindFirstObjectByType<HorseRigBindings>();Assert.That(binding,Is.Not.Null);
            var playback=binding.GetComponent<HeroHorseBenchmarkPlayback>();
            Assert.That(binding.Animator.applyRootMotion,Is.False);
            Assert.That(binding.Animator.cullingMode,Is.EqualTo(AnimatorCullingMode.AlwaysAnimate));
            var actor=binding.transform.position;
            float min=float.PositiveInfinity,max=float.NegativeInfinity;
            var skins=new[]{binding.Body,binding.Eyes,binding.Groom};
            foreach(var skin in skins)skin.enabled=false;
            playback.riderView=true;
            for(int i=0;i<36;i++)
            {
                yield return new WaitForSeconds(.04f);
                float y=binding.MotionRoot.position.y;min=Mathf.Min(min,y);max=Mathf.Max(max,y);
                Assert.That(Vector3.Distance(binding.transform.position,actor),Is.LessThan(1e-6f));
            }
            foreach(var skin in skins)skin.enabled=true;
            Assert.That(max-min,Is.GreaterThan(.010f),"Hidden body must not freeze the visual bones.");
            binding.Animator.speed=0;yield return null;yield return null;
            Assert.That(Vector3.Distance(playback.reviewCamera.transform.position,binding.FollowSupportPoint(playback.neutralRiderPosition)),Is.LessThan(1e-5f));
            playback.reducedMotion=true;
            yield return null;yield return null;
            Assert.That(Vector3.Distance(playback.reviewCamera.transform.position,binding.ModelSpace.TransformPoint(playback.neutralRiderPosition)),Is.LessThan(1e-5f));
            var folder=System.Environment.GetEnvironmentVariable("BARREL_HORSE_BENCHMARK_OUTPUT");
            if(!string.IsNullOrEmpty(folder))File.WriteAllText(Path.Combine(folder,"playback.json"),JsonUtility.ToJson(new Result{sampleCount=36,rootVerticalTravelM=max-min},true)+"\n");
        }
        [System.Serializable] class Result
        {
            public int sampleCount;public float rootVerticalTravelM;
            public bool inheritedCameraMotion=true,hiddenBonesAdvance=true,actorRootStationary=true,reducedMotionFixedCamera=true;
            public string scope="Unity Play Mode development benchmark, not measured phone performance or race integration.";
        }
    }
}
