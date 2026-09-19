# Reins Lab implementation — 0.4.0 / build 4

The user's ten-mechanic brief is now represented in the current blueprint, all 53 engineering section cards and the cross-stack integration plan. An additive offline Reins Lab implements the first connected gameplay experiment, while Classic Practice preserves its drawing rules and record namespace. The optional additive-mode/economy questions have not been answered; additive Lab and no paid loss insurance remain working defaults.

## Connected implementation

| Layer | What exists | Boundary |
|---|---|---|
| Shared C# | Fixed 20 ms rein movement; three-peak gate; cadence windows/streaks; legal three-barrel winding; swept circular contacts; finite wrap/Drive; surfaces and bounded horse traits; validated input replay | .NET Standard 2.1, separate `reins-lab-v1`; prototype geometry/tuning |
| Unity | Saved Lab scene, timestamped owned touch input, center cadence/hold-wrap, bodycam/chase, footing preview, map/zones, visible meters, results/retry and local own-best ghost | Offline; no opponent or wallet authority |
| Persistence | Exact surface/seed/fingerprint bests; bounded replay resimulation; atomic replacement; corrupt-data rejection and session-only fallback | Device local; no account/cloud sync |
| Backend proof | ASP.NET loopback endpoint using the same Core; strict bounded frame/manifest validation; recomputed result and source fingerprint | Installed .NET 8 development harness; production .NET 10 target unchanged, no hosted service |
| Contracts/data | Versioned request/response schemas, canonical complete input fixture, manifest/replay digests and unapplied PostgreSQL transaction/uniqueness draft | Schema files are not deployed storage; no database execution claim |
| Mobile | Combined iOS/Android builders target 0.4.0/build 4 and preserve Classic navigation | Artifact, install and manual-play evidence are separate |

Horse learning/bond progression and horse selection UI, opponent ghost matchmaking/live gaps, and streak reward UI/awards are still planned. Their server/data/UX dependencies are specified; none is simulated as a real online feature. Flash Wrap does not yet have a separate skill grade. No purchase, premium insurance, new paid service or paid art asset was introduced.

## Observed checks

- Unity scene generation, save/reopen and persistent bindings passed.
- Shared rules and regressions: **77/77 Editor tests passed**.
- Both scenes and real UI event dispatch: **8/8 Play Mode tests passed**, including a second successful run after the final presentation-only pass. All four actual Unity renders were reviewed.
- Canonical complete input stream: Unity Editor and .NET verifier agree on **35,120 ms**, **zero knocks**, **300 style points**, **2,156 fixed steps**. The Unity adapter also saves, reloads/replays, rejects an incompatible fingerprint and preserves a session best when disk saving fails.
- Scoped iPhone crash-report listing found no existing BarrelRivals report since installation; no flush/delete or unrelated report download was performed. This does not establish launch/gameplay acceptance. See `Evidence/Reins-iOS-Device.json`.
- Loopback HTTP proof: **46/46 checks passed**, including malformed/oversized requests, unknown enum/rule fields, forged state/results, sequence gaps, post-terminal input, timeout/slot limits, browser-origin isolation and independent digest/fingerprint comparison. No server remains running.
- iPhone **0.4.0/build 4** compiled, signed and passed signature/profile/ARM64/IPA checks; USB installation and the targeted installed-version query passed. Manual launch/control/performance acceptance remains pending. The Android **0.4.0/build 4** APK compiled with zero reported errors and passed package/version, ARM64, SDK 26/target 36, signature, ZIP/native 16 KB alignment and archive-integrity checks. Its haptic Java class is present. The first compile identified the missing built-in Android JNI module; enabling installed `com.unity.modules.androidjni` 1.0.0 fixed it. No Android handset run is claimed. The Reins simulation and input source are unchanged. Earlier 0.2.0 user confirmation is not reused for either newer build.

The first full-course test timed out because its test follower demanded a waypoint closer than it could reach while turning. Increasing the test-only reach tolerance let it complete; production course legality was not weakened. Review found and fixed missing sprites on filled UI meters, catch-up input backdating, session-best loss after failed saving, and missing rules fingerprint checks. The Lab omits Classic's decorative inner alley rails, whose collision was not implemented; separate world meshes preserve Classic's arena. The outer course bounds remain shared rules.

Actual Unity renders: `Evidence/Reins-Ready.png`, `Reins-Mixed-Footing.png`, `Reins-Riding.png`, `Reins-Result.png`. The 0.4.0 regression suite also regenerates Classic's `M1-*.png` screenshots; those filenames are reusable test outputs, not immutable 0.3.0 artifact captures. Version-specific IPA/source evidence preserves the earlier checkpoint.

The native build required an isolated temporary export after a coordinated project-read wait, then quarantine of 119 byte-identical numbered generated copies. All 1,122 canonical IL2CPP source/header files matched the installed engine afterward. The corrected native build succeeded; game source and installed Unity were unchanged. See `Evidence/Reins-iOS-Native.json` and [the iPhone guide](../../README-iPhone.md).

## Remaining acceptance

1. Compare two-thumb control comfort on the iPhone: rein pull distance, cadence timing, gate peaks, center hold/Wrap, Drive and safe areas. Prototype thresholds are adjustable after observation.
2. Qualify real-device persistence, audio/haptics, sustained frame pacing, heat and interrupted sessions. Desktop Unity/.NET agreement does not certify IL2CPP numerical parity.
3. Improve the representative horse/rider and arena using original/free licensed assets. The downloaded CC0 horse source is isolated and not part of this build; current art remains procedural.
4. Prove continuous-input authority with two clients, authenticated server manifests, latency/admission rules, compatible rival ghosts and service failure recovery before activating online progression or rewards.
5. Implement trusted horse bond/stamina settlement and capped streak rewards with database concurrency/recovery checks. Then expand the full content and release scope.

See [the ten-mechanic plan](Reins-Mechanics-Integration.md), [play/reproduction guide](../../README-Reins.md) and [master build status](Barrel-Rivals-Master-Build-Status.md). No complete release section is certified solely by this prototype.
