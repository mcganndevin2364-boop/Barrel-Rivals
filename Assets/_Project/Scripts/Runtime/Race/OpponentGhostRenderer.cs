using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public sealed class OpponentGhostRenderer : MonoBehaviour
    {
        [SerializeField] private Material _ghostMaterial;
        [SerializeField] private Renderer[] _renderers;

        public void SetGhostAppearance()
        {
            if (_ghostMaterial == null) return;
            foreach (var r in _renderers)
            {
                if (r != null) r.material = _ghostMaterial;
            }
        }
    }
}
