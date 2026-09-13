using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public sealed class VFXDirector : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _driftSparkBlue;
        [SerializeField] private ParticleSystem _driftSparkOrange;
        [SerializeField] private ParticleSystem _driftSparkPurple;
        [SerializeField] private ParticleSystem _barrelExplosion;

        public void SetDriftVFX(DriftTier tier)
        {
            if (_driftSparkBlue) _driftSparkBlue.gameObject.SetActive(tier == DriftTier.Blue);
            if (_driftSparkOrange) _driftSparkOrange.gameObject.SetActive(tier == DriftTier.Orange);
            if (_driftSparkPurple) _driftSparkPurple.gameObject.SetActive(tier == DriftTier.Purple);
        }

        public void PlayBarrelKnock(Vector3 pos)
        {
            if (_barrelExplosion != null)
            {
                _barrelExplosion.transform.position = pos;
                _barrelExplosion.Play();
            }
        }
    }
}
