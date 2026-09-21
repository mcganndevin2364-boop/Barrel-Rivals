# Fitted horse development benchmark

Open **Barrel Rivals → Development → Open fitted horse benchmark** in Unity 6000.6.0f1. Play starts the imported walk; on the horse root's `HeroHorseBenchmarkPlayback`, enable `riderView` for first person or `reducedMotion` for a fixed camera. The neutral fitting pose is not an authored idle gait.

This scene is excluded from player builds and does not replace the racing/stable/ghost horse. [Implementation, measured checks and open issues](../../../../Docs/Art/Reins-Horse-Rider-Fit.md).

Horse anatomy derives from **Horse by b2przemo**, CC-BY-3.0, with project rig, eye/coat, groom and animation modifications. Carry [attribution and license](../../../../ArtSource/HorseStudy/ATTRIBUTION.md) with derivatives. Editable source and pinned hashes are under `ArtSource/HorseStudy`. Existing original Barrel Rivals saddle meshes and hair atlases are reused. No purchased assets or reference-image pixels.

Do not rename the new rig to mimic the earlier hierarchy. Configure `HorseRigBindings` only at the restored neutral pose, keep four-weight skinning and disable root motion. The fitted bridle/reins/rider now follow explicit anchors; first-person view switches its FOV and hides the rider head while preserving shadows. Remaining natural gaits/actions, detail, LODs, runtime adoption and reference-quality/device acceptance are unfinished. Candidate cost is 92,402 triangles/25 slots. Rider art retains the CC0 credit/source record under Assets/_Project/Art/Reins/Rider. The seven-pose animated source comparison still misses its retained 0.2 mm target at 0.364 mm; the capture report records that failure explicitly.
