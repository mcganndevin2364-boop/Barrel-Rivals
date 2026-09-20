using System.Collections;
using BarrelRivals.Foundation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BarrelRivals.Tests
{
    public sealed class PreviewTests
    {
        [UnityTest] public IEnumerator PreviewButtonsMoveStopAndResetTheSavedScene()
        {
            yield return HistoricalSceneLoad.Load("Arena_Foundation");
            yield return null;
            var preview=Object.FindFirstObjectByType<FoundationPreview>();
            Assert.IsNotNull(preview); Assert.IsTrue(preview.HasBindings);
            Vector3 origin=preview.Horse.position;
            var start=GameObject.Find("Preview ride").GetComponent<Button>();
            var reset=GameObject.Find("Reset view").GetComponent<Button>();
            start.onClick.Invoke(); Assert.IsTrue(preview.IsRunning);
            yield return new WaitForSeconds(0.3f);
            Assert.Greater(Vector3.Distance(origin,preview.Horse.position),0.1f);
            reset.onClick.Invoke(); Assert.IsFalse(preview.IsRunning);
            Assert.That(Vector3.Distance(origin,preview.Horse.position),Is.LessThan(0.001f));
            start.onClick.Invoke();
            float timeout=Time.realtimeSinceStartup+10;
            while(preview.IsRunning && Time.realtimeSinceStartup<timeout) yield return null;
            Assert.IsFalse(preview.IsRunning,"Preview should stop at the first barrel approach.");
            Assert.IsTrue(start.interactable);
            reset.onClick.Invoke(); Assert.AreEqual(origin,preview.Horse.position);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
