using UnityEngine;
using UnityEngine.UI;
namespace BarrelRivals.Foundation
{
    /// <summary>Development preview only; no competitive race score or rewards.</summary>
    public sealed class FoundationPreview : MonoBehaviour
    {
        [SerializeField] private Transform horse, arrival;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Text status;
        [SerializeField] private Button previewButton, resetButton;
        [SerializeField] private RectTransform safeArea;
        private Vector3 _horseStart, _cameraStart;
        private Quaternion _horseRotation, _cameraRotation;
        private bool _running;
        public bool IsRunning => _running;
        public Transform Horse => horse;
        public bool HasBindings => horse && arrival && viewCamera && status && previewButton && resetButton && safeArea;

        public void Configure(Transform horseRoot, Camera camera, Transform target, Text label,
            Button preview, Button reset, RectTransform screenSafeArea)
        {
            horse = horseRoot; viewCamera = camera; arrival = target; status = label;
            previewButton = preview; resetButton = reset; safeArea = screenSafeArea;
        }
        private void Awake()
        {
            if (!HasBindings) { Debug.LogError("Foundation preview has missing references.", this); enabled = false; return; }
            _horseStart = horse.position; _horseRotation = horse.rotation;
            _cameraStart = viewCamera.transform.position; _cameraRotation = viewCamera.transform.rotation;
            previewButton.onClick.AddListener(BeginPreview);
            resetButton.onClick.AddListener(ResetPreview);
            ResetPreview();
        }
        private void OnDestroy()
        {
            if (previewButton) previewButton.onClick.RemoveListener(BeginPreview);
            if (resetButton) resetButton.onClick.RemoveListener(ResetPreview);
        }
        public void BeginPreview()
        {
            if (!enabled || !HasBindings || _running) return;
            ResetPreview(); _running = true; previewButton.interactable = false;
            status.text = "Riding to the first barrel";
        }
        public void ResetPreview()
        {
            if (!HasBindings) return;
            _running = false;
            horse.SetPositionAndRotation(_horseStart, _horseRotation);
            viewCamera.transform.SetPositionAndRotation(_cameraStart, _cameraRotation);
            previewButton.interactable = true; status.text = "Explore the practice arena";
        }
        private void Update()
        {
            if (Screen.width > 0 && Screen.height > 0)
            {
                Rect rect = Screen.safeArea;
                safeArea.anchorMin = new Vector2(rect.xMin / Screen.width, rect.yMin / Screen.height);
                safeArea.anchorMax = new Vector2(rect.xMax / Screen.width, rect.yMax / Screen.height);
            }
            if (!_running) return;
            Vector3 delta = arrival.position - horse.position; delta.y = 0;
            if (delta.sqrMagnitude < 0.01f)
            {
                _running = false; previewButton.interactable = true;
                status.text = "First barrel reached — reset to explore again"; return;
            }
            horse.rotation = Quaternion.RotateTowards(horse.rotation, Quaternion.LookRotation(delta), 100 * Time.deltaTime);
            horse.position = Vector3.MoveTowards(horse.position, arrival.position, 7 * Time.deltaTime);
        }
        private void LateUpdate()
        {
            if (_running) viewCamera.transform.SetPositionAndRotation(
                horse.TransformPoint(new Vector3(0, 2.15f, -0.25f)), horse.rotation * Quaternion.Euler(7, 0, 0));
        }
        private void OnApplicationPause(bool paused) { if (paused && HasBindings) ResetPreview(); }
    }
}
