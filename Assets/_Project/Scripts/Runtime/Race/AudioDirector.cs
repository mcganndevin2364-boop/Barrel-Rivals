using System;
using System.Collections.Generic;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public enum RaceSFXType { CountdownBeepLow, CountdownBeepGo, WhipCrackNormal, WhipCrackPerfect, DriftBoostRelease, BarrelCollisionKnock, ScoreTallyTick, ScoreMultiplierSlam, AdrenalineHeartbeat }

    [DisallowMultipleComponent]
    public sealed class AudioDirector : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _sfxCountdownBeepLow;
        [SerializeField] private AudioClip _sfxCountdownBeepGo;
        [SerializeField] private AudioClip _sfxWhip;
        [SerializeField] private AudioClip _sfxDriftBoost;
        [SerializeField] private AudioClip _sfxBarrelKnock;

        public void UpdateHoofbeatCadence(float speedRatio) { }
        public void SetDriftChargeTierAudio(DriftTier tier) { }
        public void SetAdrenalineState(bool active) { }

        public void PlaySFX(RaceSFXType sfx, float volume = 1.0f, float pitch = 1.0f)
        {
            if (_audioSource == null) return;
            AudioClip clip = sfx == RaceSFXType.CountdownBeepLow ? _sfxCountdownBeepLow :
                             sfx == RaceSFXType.CountdownBeepGo ? _sfxCountdownBeepGo :
                             sfx == RaceSFXType.DriftBoostRelease ? _sfxDriftBoost :
                             sfx == RaceSFXType.BarrelCollisionKnock ? _sfxBarrelKnock : _sfxWhip;
            if (clip != null) { _audioSource.pitch = pitch; _audioSource.PlayOneShot(clip, volume); }
        }
    }
}
