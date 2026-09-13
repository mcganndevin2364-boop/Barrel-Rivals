using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public sealed class AudioDirector : MonoBehaviour
    {
        [SerializeField] private AudioSource _gallopSource;
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioClip _whipClip;
        [SerializeField] private AudioClip _barrelHitClip;

        public void PlayWhipSound()
        {
            if (_sfxSource && _whipClip) _sfxSource.PlayOneShot(_whipClip);
        }

        public void PlayBarrelKnockSound()
        {
            if (_sfxSource && _barrelHitClip) _sfxSource.PlayOneShot(_barrelHitClip);
        }
    }
}
