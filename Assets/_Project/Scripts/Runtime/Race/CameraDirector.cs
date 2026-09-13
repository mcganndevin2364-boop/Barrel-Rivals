using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public sealed class CameraDirector : MonoBehaviour
    {
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _followOffset = new Vector3(0, 3.2f, -5.5f);
        [SerializeField] private float _fovGallop = 68f;

        private void LateUpdate()
        {
            if (_target == null || _mainCamera == null) return;
            Vector3 desiredPos = _target.TransformPoint(_followOffset);
            _mainCamera.transform.position = Vector3.Lerp(_mainCamera.transform.position, desiredPos, Time.deltaTime * 10f);
            _mainCamera.transform.LookAt(_target.position + Vector3.up * 1.5f);
            _mainCamera.fieldOfView = Mathf.Lerp(_mainCamera.fieldOfView, _fovGallop, Time.deltaTime * 5f);
        }
    }
}
