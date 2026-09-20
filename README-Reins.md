# Barrel Rivals — Reins Lab

This page documents the implemented 0.4 offline Reins prototype. The user has now selected Reins as the main game. Classic is still available in this binary, but the approved 0.5 update removes its player-facing route while preserving historical code and saves. The combined mobile build is **0.4.0 / build 4**. The iPhone app compiled, signed and installed with its version verified; the Android APK compiled and passed artifact checks. The user has played and prefers Reins. Formal iPhone controls/performance qualification and Android handset testing remain pending. See [the implementation evidence](Docs/Plan/Reins-Lab-Implementation.md) for the exact limits.

**New source, separate from that installed build:** the [reference-graphics checkpoint](Docs/Art/Reins-Reference-Graphics-Checkpoint.md) adds the imported 19-bone CC0 horse and three first-pass gait studies, persistent URP materials, Linear/warm grading, original dirt/crowd art, first-person viewing in every phase with reduced motion, and a charcoal/gold HUD. Scene generation/save/reopen, 77 Editor tests and 13 Play Mode tests passed; records and five real Unity gameplay captures are in that checkpoint. No new native build, installation or measured phone performance is claimed. The source retains 0.4 identity, v1 rules/saves and Classic navigation. Its art is a step toward the user's throughout-gameplay reference, not finished photorealism or the complete horse/rider milestone.

## Approved remaining work

[Reins Racing 0.5](Docs/Plan/Reins-Racing-0.5-Implementation.md) still needs to replace the stationary preview/three-tap gate with a four-second rider-view alley approach and one release at the third beep, loosen cadence/rein demands and complete one finished horse/rider/arena. The graphics checkpoint does not implement those v2 rules. Current controls below retain the 0.4 mechanics. See [the AI handoff](Barrel-Rivals-AI-Handoff.md) before continuing development.

## Play the v1 gameplay

From Classic Practice, select **TRY REINS RACING**. In Unity, open `Assets/_Project/Generated/ReinsLab/Arena_ReinsLab.unity`.

1. Select dirt conditions before starting. The current source keeps the rider camera through the timed preview; use the course map for the pattern. The older installed 0.4 artifact has its original presentation.
2. Tap the **center pad** at each of the three gate-energy peaks. A false break produces one bounded standstill; it never subtracts arbitrary time from a result.
3. Drag down on the **left/right rein pads** to turn. Release to relax a rein; pull both to brake. Hold the turning rein with one thumb and tap the center cadence pad with the other.
4. Near a barrel, holding the center pad for 300 ms activates **Leg Wrap** while eligible. It trades cadence/speed for turning control, with a two-second budget per barrel. It never grants contact immunity.
5. Circle the left barrel left, then the right and far barrels right, following the map. Close clean passes earn style/exit speed; geometric contact knocks a barrel and adds five seconds once.
6. After barrel three, alternate taps on the side pads for the capped four-second Drive opportunity, then cross the white finish line between the gate posts. You can still drag a rein to correct direction.
7. Retry repeats the conditions. A completed best is saved locally and can appear as an explicitly labelled own-best ghost. **CLASSIC PRACTICE** returns to drawing practice.

Both reins plus center are never required simultaneously. Braking with both thumbs temporarily sacrifices center action. This layout needs real-phone ergonomic testing before becoming the permanent control scheme. In the current source, **RIDER VIEW / STEADY VIEW** switches reduced motion while retaining first person; the prior installed artifact still has its original bodycam/chase control. Sound/ghost preferences are presentation choices. The haptic preference is shared with Classic Practice.

## What this pass implements

| Requested mechanic | Current connection |
|---|---|
| Reins Tension | Owned multi-touch drags → timestamped 20 ms input → shared steering/brake rules → horse/camera pose |
| Cadence Gallop | Center taps, 300–500 ms beat periods, bounded grades/Hot/Blazing effects, scheduled audio and visible meter |
| Barrel Pocket / Kiss | Swept circular contact, ordered turn completion, one knock per barrel, zone style and capped exit burst |
| Gate Break | Three wave-peak taps, duplicate/spam handling, visible grade and simulated launch/standstill |
| Leg Wrap | Contextual center hold, two-second per-barrel budget, speed/turn tradeoff; no separate Flash Wrap grade yet |
| Dirt Read | Five selectable conditions, six visible Mixed patches, shared deterministic surface/round calculations |
| Home Stretch Drive | Legal three-barrel completion, alternating side taps, bounded event rate/boost/window and physical finish |
| Horse IQ | Four bounded traits and round modifiers in shared Core/contracts; default profile in the scene. Horse picker, learning, bond and ownership progression remain planned |
| Rival Ghost Pressure | Validated own-best recording in Unity. Opponent matchmaking, authorized rival replays, live gaps and cinematic clutch effects remain planned |
| Streak Stakes | Revised earned-bonus design, API/DB ownership and idempotency contracts. No local reward preview, wallet, insurance, purchase or online award is enabled |

The current source imports the CC0 horse with original Idle/Walk/Gallop studies and replaces selected arena/HUD presentation. A finished rigged rider, western tack, planted production gaits and reference-level visual/device acceptance remain pending. The older installed 0.4 artifact still contains its prior procedural art. No paid assets were purchased. [Graphics provenance](Docs/Art/Reins-Reference-Graphics-Provenance.md) records source hashes, licensing and limitations.

## Rule and save boundaries

The new namespace is `reins-lab-v1`; it cannot reinterpret Classic rules-version-1 records. A generated SHA-256 fingerprint identifies the four simulation source files. Saved Lab recordings require that fingerprint, the fixed Lab seed, exact surface and a complete replay that reproduces the final time. There are at most five local surface bests, each bounded to 7,500 input frames and 2 MiB on read. Corrupt/incompatible recordings cannot qualify a best. Save failures retain a verified session best across Retry and report that it was not persisted.

Input changes are queued at Unity's processing timestamp and applied at the first subsequent simulation boundary. A frame interruption longer than 250 ms cancels practice instead of extending its clock; pointer releases/cancellation clear held reins. These are offline consistency rules, not hardware-latency calibration or production anti-cheat.

Knocks use a provisional circular horse/barrel contact radius of 0.82 m; the clean Kiss center-distance band is 1.0–1.2 m. Real production geometry needs matching collision bounds and animation validation. Fixed stepping and desktop agreement do not establish bitwise determinism on iPhone/Android IL2CPP.

## Reproduce

Close other Unity Editors for this project. Run from the repository root:

```bash
python3 Tools/update-reins-fingerprint.py
bash Tools/run-reins.sh generate
bash Tools/run-reins.sh editmode
bash Tools/run-reins.sh playmode
bash Tools/run-reins.sh ios
python3 Tools/verify-ios-export.py --version 0.4.0 --build 4 --output Evidence/Reins-iOS-Export.json
bash Tools/run-reins.sh android
python3 Tools/verify-android.py --apk Builds/Android/BarrelRivals-ReinsLab.apk --output Evidence/Reins-Android-Artifact.json --version 0.4.0 --code 4
```

The scene generator also installs Classic's navigation button and corrects filled timing-meter sprites. Hand-authored assets belong outside generated directories. Changing shared Reins rules requires a deliberate fingerprint/version/replay compatibility review, then `Tools/update-reins-fingerprint.py --write` and fresh fixture checks. Do not regenerate expected results merely to hide a regression.

The [loopback server proof](Tools/ReinsServerCheck/README.md) uses the installed .NET 8 SDK to run the same .NET Standard Core. It accepts input frames and recomputes the result; uploaded positions, final times or rewards are rejected. It is localhost-only, unauthenticated development tooling and cannot authorize ranked results. The production service target remains .NET 10, pending a supported host and network proof. No Fusion session or hosted service is configured.

[Contracts and complete replay fixture](Contracts/Reins/README.md) define the versioned boundary. [The PostgreSQL draft](Backend/Schema/README.md) has not been executed; its example transaction ends with `ROLLBACK`. Schema files and SQL source are not database deployment evidence.

See [the complete ten-mechanic integration plan](Docs/Plan/Reins-Mechanics-Integration.md), [master status](Docs/Plan/Barrel-Rivals-Master-Build-Status.md) and [Reins implementation evidence](Docs/Plan/Reins-Lab-Implementation.md) for remaining gates and observed results.
