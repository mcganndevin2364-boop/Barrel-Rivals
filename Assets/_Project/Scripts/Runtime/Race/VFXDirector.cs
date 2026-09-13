using System;
using System.Collections.Generic;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public enum RaceVFXType { HoofDustStep, DriftSparksBlue, DriftSparksOrange, DriftSparksPurple, DriftBoostBurst, WhipCrackBurst, BarrelKnockDebris, PocketNearMissSparkle, GateLaunchSmoke }

    [DisallowMultipleComponent]
    public sealed class VFXDirector : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _driftBoostBurstPrefab;
        [SerializeField] private ParticleSystem _whipCrackBurstPrefab;
        [SerializeField] private ParticleSystem _barrelKnockDebrisPrefab;

        private DriftTier _activeTier = DriftTier.None;

        public void UpdateHorseSpeed(float speedRatio) { }
        public void SetDriftSparkTier(DriftTier tier, Vector3 pos, Quaternion rot) => _activeTier = tier;
        public void StopDriftSparks() => _activeTier = DriftTier.None;

        public void PlayDriftBoostReleaseBurst(Vector3 pos, Quaternion rot) => SpawnEffect(_driftBoostBurstPrefab, pos, rot);
        public void PlayWhipCrackBurst(Vector3 pos, Quaternion rot) => SpawnEffect(_whipCrackBurstPrefab, pos, rot);
        public void PlayBarrelKnockDebris(Vector3 pos) => SpawnEffect(_barrelKnockDebrisPrefab, pos, Quaternion.identity);

        private void SpawnEffect(ParticleSystem prefab, Vector3 pos, Quaternion rot)
        {
            if (prefab != null)
            {
                ParticleSystem ps = Instantiate(prefab, pos, rot);
                ps.Play();
                Destroy(ps.gameObject, 2.0f);
            }
        }
    }
}
