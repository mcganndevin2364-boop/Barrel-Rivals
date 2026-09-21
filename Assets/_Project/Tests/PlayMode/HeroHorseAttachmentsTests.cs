using System.Collections;
using System.Linq;
using BarrelRivals.Practice;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace BarrelRivals.Tests.PlayMode
{
    public sealed class HeroHorseAttachmentsTests
    {
        [UnityTest]
        public IEnumerator LiveRigKeepsAttachmentsConnectedAndOwnsReinMeshes()
        {
            Mesh leftSource=null,rightSource=null;Vector3[] sourceVertices=null;
#if UNITY_EDITOR
            const string path="Assets/_Project/Development/HeroHorse/";
            leftSource=AssetDatabase.LoadAssetAtPath<Mesh>(path+"Fitted left rein.asset");
            rightSource=AssetDatabase.LoadAssetAtPath<Mesh>(path+"Fitted right rein.asset");
            sourceVertices=leftSource.vertices;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path+"HeroHorseBenchmark.unity",new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("Development scene is excluded from players.");yield break;
#endif
            yield return null;
            var horse=Object.FindFirstObjectByType<HorseRigBindings>();
            var tack=horse.GetComponent<HeroHorseAttachments>();
            var actor=horse.transform.position;
            Assert.That(tack,Is.Not.Null);
            Assert.That(tack.leftRein.sharedMesh,Is.Not.SameAs(leftSource));
            Assert.That(tack.rightRein.sharedMesh,Is.Not.SameAs(rightSource));
            var leftInstance=tack.leftRein.sharedMesh;var rightInstance=tack.rightRein.sharedMesh;
            Assert.That(leftInstance,Is.Not.SameAs(rightInstance));
            var nodes=horse.GetComponentsInChildren<Transform>();
            var seat=nodes.Single(t=>t.name=="Fitted rider pelvis");var pelvis=nodes.Single(t=>t.name=="pelvis");
            var playback=horse.GetComponent<HeroHorseBenchmarkPlayback>();
            float inspectionFov=playback.reviewCamera.fieldOfView;var inspectionPosition=playback.reviewCamera.transform.position;
            playback.riderView=true;
            var firstBit=tack.leftBit.position;float bitTravel=0;
            for(int i=0;i<42;i++)
            {
                tack.leftPull=i%3*.5f;tack.rightPull=1-tack.leftPull;
                yield return new WaitForSeconds(.03f);
                Assert.That(tack.rider.MaximumReachError,Is.LessThan(.01f));
                Assert.That(Vector3.Distance(seat.position,pelvis.position),Is.LessThan(.001f));
                Assert.That(Vector3.Distance(actor,horse.transform.position),Is.LessThan(1e-6f));
                AssertEndpoint(tack.leftRein,0,tack.leftGrip.position);
                AssertEndpoint(tack.leftRein,ReinsRiderTackPresentation.Segments*8,tack.leftBit.position);
                AssertEndpoint(tack.rightRein,0,tack.rightGrip.position);
                AssertEndpoint(tack.rightRein,ReinsRiderTackPresentation.Segments*8,tack.rightBit.position);
                bitTravel=Mathf.Max(bitTravel,Vector3.Distance(firstBit,tack.leftBit.position));
                foreach(var grip in new[]{tack.leftGrip,tack.rightGrip})
                {
                    var viewport=playback.reviewCamera.WorldToViewportPoint(grip.position);
                    Assert.That(viewport.z,Is.GreaterThan(playback.reviewCamera.nearClipPlane));
                    Assert.That(viewport.x,Is.InRange(.02f,.98f));
                    Assert.That(viewport.y,Is.InRange(.01f,.98f),"Both rein grips should remain in the riding view.");
                }
            }
            Assert.That(bitTravel,Is.GreaterThan(.01f));
            foreach(var surface in tack.headSurfaces)Assert.That(surface.shadowCastingMode,Is.EqualTo(ShadowCastingMode.ShadowsOnly));
            Assert.That(playback.reviewCamera.fieldOfView,Is.EqualTo(playback.riderFieldOfView));
            playback.riderView=false;yield return null;yield return null;
            Assert.That(playback.reviewCamera.fieldOfView,Is.EqualTo(inspectionFov));
            Assert.That(Vector3.Distance(playback.reviewCamera.transform.position,inspectionPosition),Is.LessThan(.00001f));
            foreach(var surface in tack.headSurfaces)Assert.That(surface.shadowCastingMode,Is.EqualTo(ShadowCastingMode.On));
            CollectionAssert.AreEqual(sourceVertices,leftSource.vertices,"Live posing must not deform persistent assets.");
            var leftFilter=tack.leftRein;var rightFilter=tack.rightRein;
            Object.Destroy(tack);yield return null;yield return null;
            Assert.That(leftInstance==null && rightInstance==null,Is.True,"Private deformation meshes must be released.");
            Assert.That(leftFilter.sharedMesh,Is.SameAs(leftSource));Assert.That(rightFilter.sharedMesh,Is.SameAs(rightSource));
            LogAssert.NoUnexpectedReceived();
        }

        static void AssertEndpoint(MeshFilter filter,int first,Vector3 expected)
        {
            var points=filter.sharedMesh.vertices;var center=Vector3.zero;
            for(int i=0;i<8;i++)center+=filter.transform.TransformPoint(points[first+i]);
            Assert.That(Vector3.Distance(center/8,expected),Is.LessThan(.00001f));
        }
    }
}
