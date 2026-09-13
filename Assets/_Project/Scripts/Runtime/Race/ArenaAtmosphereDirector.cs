using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public sealed class ArenaAtmosphereDirector : MonoBehaviour
    {
        [SerializeField] private Light _sunLight;
        [SerializeField] private Color _dayColor = Color.white;

        public void SetAtmosphere(float intensity)
        {
            if (_sunLight != null) _sunLight.intensity = intensity;
        }
    }
}
