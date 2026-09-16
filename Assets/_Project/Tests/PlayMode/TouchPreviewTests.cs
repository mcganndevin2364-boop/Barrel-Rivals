using System.Collections;
using System.Collections.Generic;
using BarrelRivals.Foundation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace BarrelRivals.Tests
{
    public sealed class TouchPreviewTests
    {
        [UnityTest]
        public IEnumerator TouchEventsReachPreviewAndResetThroughTheUiModule()
        {
            yield return SceneManager.LoadSceneAsync("Arena_Foundation", LoadSceneMode.Single);
            yield return null;
            var preview = Object.FindFirstObjectByType<FoundationPreview>();
            Assert.IsNotNull(preview);
            Vector3 origin = preview.Horse.position;
            // An unattended batch Editor has no focused Game view. Simulate foreground
            // input for this test, then restore the project's normal focus policy.
            var backgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            var editorRouting = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            var touchscreen = InputSystem.AddDevice<Touchscreen>();
            try
            {
                yield return null;
                Canvas.ForceUpdateCanvases();
                yield return Tap(touchscreen, "Preview ride", 1);
                Assert.IsTrue(preview.IsRunning, "The UI must receive touch through its input module.");
                yield return new WaitForSeconds(0.2f);
                Assert.Greater(Vector3.Distance(origin, preview.Horse.position), 0.1f);
                yield return Tap(touchscreen, "Reset view", 2);
                Assert.IsFalse(preview.IsRunning);
                Assert.That(Vector3.Distance(origin, preview.Horse.position), Is.LessThan(0.001f));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                InputSystem.RemoveDevice(touchscreen);
                InputSystem.settings.backgroundBehavior = backgroundBehavior;
#if UNITY_EDITOR
                InputSystem.settings.editorInputBehaviorInPlayMode = editorRouting;
#endif
            }
        }

        private static IEnumerator Tap(Touchscreen screen, string buttonName, int touchId)
        {
            var rect = GameObject.Find(buttonName).GetComponent<RectTransform>();
            Vector2 position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
            Assert.IsTrue(hits.Exists(hit => hit.gameObject == rect.gameObject),
                "The rendered button must be hittable at " + position + "; focus=" + EventSystem.current.isFocused);
            InputSystem.QueueStateEvent(screen, new TouchState
            {
                touchId = touchId, phase = TouchPhase.Began, position = position, pressure = 1
            });
            yield return null;
            yield return null;
            Assert.IsTrue(screen.primaryTouch.press.isPressed, "Input System must consume the queued touch press.");
            InputSystem.QueueStateEvent(screen, new TouchState
            {
                touchId = touchId, phase = TouchPhase.Ended, position = position
            });
            yield return null;
            yield return null;
        }
    }
}
