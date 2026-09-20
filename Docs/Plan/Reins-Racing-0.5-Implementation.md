# Reins Racing 0.5 — approved implementation plan

The current source repairs **previously inactive URP post-processing**, retunes arena light and fence materials, and varies the fitted mane flow. It also corrects evidence capture so world grading does not alter the actual overlay HUD. Read [the renderer/lighting checkpoint](../Art/Reins-Lighting-Flow-Checkpoint.md) for executed pixel checks, current race/stable/gear captures, source costs and visual limits. Photographic quality, full rider animation, device qualification and approved 0.5/v2 gameplay remain unfinished.

The preceding source added **a layered mane/tail groom with restrained phase-driven motion**, including stable Idle, reduced motion and private ghost handling. It also combines the racing hands into fewer material slots while preserving all 20 cosmetics. Read [the groom checkpoint](../Art/Reins-Hair-Groom-Checkpoint.md) for current tests, actual moving captures and source costs. Photographic quality, a full rider, further optimization, phone qualification and the approved 0.5/v2 gameplay remain unfinished.

Approved September 19, 2026. Gameplay revision 5 / engineering revision 6. **Status: approved, not implemented.** Source baseline is `3e1ce2f` (0.4.0/build 4). This document supersedes the additive-Lab, three-tap gate and third-person introduction defaults in earlier planning documents. Historical 0.4 evidence remains valid for its own source and rules only.

## Outcome and boundaries

Reins Racing becomes the main player-facing game, with first-person riding throughout. Preserve Classic source, evidence and isolated saves as history/maintenance material; remove its navigation and scene from the next player build. Continue the same repository, not a new Unity project.

Deliver one polished horse/rider/arena and a complete three-barrel race: moving alley approach → timed release → rein steering and heartbeat cadence → Pocket/Wrap turns → Drive → finish/result/retry. The user selected realistic arcade visuals, responsive and forgiving handling, and an early/late start that grades and continues. Preserve player steering and the existing liked Reins loop. No automatic barrel steering, hidden knock probability, time-credit boosts or paid timing advantages.

Use free or original art with recorded commercial-use provenance. Paid assets/services remain deferred. Multiplayer, account services, cloud saves, economy, ten-horse roster and store qualification remain later milestones; the local verifier is not a production service. Preserve all eight master categories and 53 section IDs.

## 1. First-person alley and launch

### Canonical timeline

| Moment from approach start | Core behavior | Player presentation |
|---|---|---|
| Ready | Horse at `(0,-6)`, heading toward positive Z; no race clock | Idle horse, visible ears/neck/hands/reins. Compact surface selection and “Hold to approach.” No overhead introduction. |
| 0 ms | Center pointer-down calls `Start()` and arms the launch hold at tick 0 | Horse starts walking; finger remains held. |
| 0–4,000 ms | Fixed six-metre approach at 1.5 m/s; position derived directly from elapsed ticks | In-place walk, synchronized rider/hooves, stable first-person view. |
| 2,000 / 3,000 ms | Published first/second cue ticks | First and second beep. Alley opening provides timing perspective. |
| 4,000 ms / tick 200 | Horse reaches invisible start plane `z=0`; Racing begins, race time zero, speed 1.5 m/s | Third beep/GO. Steering becomes available. No painted starting line. |
| GO +400 ms / tick 220 | Close launch grading if still held | Normal/weak launch; no pause or forced restart. |
| GO +600 ms | First cadence beat | Heartbeat meter/audio and normal center-tap play. |

At tick 200, snap to the exact canonical start pose; the first free movement step is tick 201. Ready setup does not count toward race time. No input can accelerate the scripted approach or move the clock origin. Update the first-barrel incoming direction in `ReinsCourseJudge` and both fixture route generators from old `(0,-9)` to `(0,0)`.

### Release and launch outcome

- Grade the first intentional release relative to GO: Perfect `abs(error) <= 120 ms`; Good `120 < abs(error) <= 240 ms`; Weak outside those windows. Keep signed timing error in the result. A held timeout has an explicit timeout outcome and no fabricated release timestamp.
- Perfect multiplies positive acceleration by 1.35; Good by 1.15; Weak/timeout uses normal acceleration. Normal braking and current absolute speed/yaw caps remain. All launch benefits expire at **GO +1,200 ms**, never release +1,200 ms.
- An early release locks its outcome but finishes the same approach. A late grade affects movement starting on the next simulation step; do not retroactively accelerate. Earlier Perfect releases consequently have a small advantage over later Perfect releases. Do not promise identical trajectories for every Perfect grade.
- Remove old three-peak counters, peak tapping, speed-cap boost and false-break standstill from v2. Repeated holds/releases never rearm or grant another award.
- Initial launch arming is part of the replay contract: a down/up within one 20 ms interval still produces a release at tick 1. Cancellation takes precedence over grading; a same-tick intentional release precedes timeout. Preserve ordinary first-subsequent-boundary input timing; never backdate input during catch-up.
- Keep legitimate side-rein touches across approach/race transition. The launch-owned pointer cannot become cadence or Wrap. Retire it after release/deadline and suppress it until physically lifted; require a fresh press for cadence. Pre-GO re-presses do not rearm launch.
- Touch cancellation/backgrounding cancels this offline attempt. Retry clears held state and scheduled cues. Missed launch does not delay the first cadence beat.

Schedule three dedicated beep voices ahead of time from one DSP/accepted-start timing origin; use a separate cadence voice. Do not schedule each beep from whichever render frame happens to cross its threshold. Cancel future cues on retry/cancel. Test actual speaker/input alignment on the phone and preserve playable visual cues with sound disabled.

### Alley and finish geometry

Build an actual alley behind `z=0`. Publish its wall bounds in shared course data and use those same bounds to place visible rails. Add deterministic swept wall contact/slide without a barrel knock or time penalty. End rails at least one metre behind the score plane, with endcaps plus horse radius clear of the full existing finish gate `|x| <= 6`. Test wrong-way approaches as well as normal launch/finish; decorative walls alone are insufficient because players can ride backward. The scoring plane remains invisible; posts and compact finish feedback identify the finish.

## 2. Responsive arcade control and feedback

- Retain direct differential rein steering, both-rein braking, current turning-radius/speed caps, legal winding rules and once-per-barrel five-second knock penalties.
- Reduce full-pull travel from 65% to **50% of pad height**, keeping the current 80-unit minimum and pointer ownership.
- Cadence windows: Perfect ±60 ms, Great ±100 ms, Good ±140 ms. Period range: **360–500 ms**. Preserve one award per beat and existing bounded recovery/Hot/Blazing behavior unless a failing acceptance case requires a separately documented adjustment.
- Preserve contextual 300 ms Wrap and alternating final Drive. A launch release is neither a cadence tap nor a Wrap request.
- Add short, readable launch/turn grades, optional brief haptics, clear heartbeat feedback and a stronger audible transition into acceleration. Avoid persistent instruction blocks over the horse or first barrel.
- Move footing/settings to pre-run or pause UI. Teach each action contextually; keep rein controls, rhythm cue and course target readable in landscape safe areas. Effects/comfort settings never change simulation results.

## 3. Representative art and animation

### One finished scene

Create one convincing chestnut horse, western tack, one neutral rider and one warm dusk rodeo arena before expanding content. The September 20 source now contains an imported CC0 horse, basic gait studies and modeled foreground tack/hands/reins; these still cannot meet the finished character milestone through lighting changes alone. See [the current graphics checkpoint](../Art/Reins-Premium-Graphics-Checkpoint.md) for implementation and evidence.

Inspect the isolated CC0 Rigged Horse candidate with embedded-script execution disabled. Check provenance, anatomy from the rider camera, UVs/texture completeness, skinning and joint deformation. Export only approved mesh/rig/image data. Rework or replace unsuitable geometry with original modeled assets; do not label the candidate production-ready before inspection. The source URL/hash and other options are in [the asset research](../Art/Horse-and-Rider-Options.md). Blender **4.5 LTS Intel** is the selected art-tool line for this host; [official requirements](https://www.blender.org/download/requirements/) retain Intel Mac support. Blender 4.5.13 was subsequently used from the official read-only Intel DMG with auto-execution disabled for the first export; provenance pins the source and tooling.

- Replace the generated horse with a skinned quadruped. Author in-place idle, walk, canter, gallop, sprint, braking, left/right turn and contact motions, with smooth speed-driven blending.
- Synchronize rider seat/torso/arms and rein grip with the horse. Use restrained bone-driven mane/tail movement; no runtime hair/cloth simulation for this milestone.
- Keep the camera on an interpolated, stabilized rider-seat target. Show neck/ears below the course and hands/reins near the edges. Hide the rider head from the first-person camera while preserving suitable body/shadow visibility. Reduced motion disables bob, roll and speed-related FOV changes. Optional chase remains a settings/debug view, not an automatic race cut.
- Replace foreground dirt, barrels, fencing and chute materials/meshes with coherent modeled content. Use dirt albedo/normal/roughness variation, localized ruts and pooled dust. Keep inexpensive distant crowd/horizon geometry until the foreground is convincing.
- Put authored assets/prefabs outside builder-owned `Generated` directories. Builders reference approved content instead of regenerating it.

### Runtime ownership and rendering

A Unity presentation adapter consumes Core position, heading, speed, rein tension, Wrap and accepted events. Core owns movement, contact and scoring; Animator root motion and animation events never move or grade the horse. Horse/rider share a visual gait clock. Hoof sounds/dust use clip contact markers; skill heartbeat timing remains independently authoritative.

Use Linear color space, retuned materials/exposure, URP Forward, one shadowed directional light, baked static ambient detail/reflections, 2× MSAA and restrained color grading. The initial profile starts without HDR/bloom, motion blur or depth of field. The September 20 graphics source evaluates HDR/ACES plus FXAA for the dusk reference; this is an unqualified visual candidate, not a measured replacement for the mobile performance gate. Keep bloom, motion blur and depth of field off. Profile the chosen effects on device; [Unity's URP guidance](https://docs.unity.com/en-us/engine/6000.6/manual/analysis/graphics-performance-profiling/in-urp/understand-performance/understand-performance) explains why render passes, texture traffic, draw calls and render scale matter on mobile.

Initial engineering limits (targets, not achieved measurements):

| Item | Budget |
|---|---|
| Horse + tack + rider | <=60k nearest-LOD triangles; approximately 30k/12k lower LODs |
| Character rig/materials | <=160 combined bones, four weights/vertex, <=6 material slots |
| Textures | 2K maximum character/ground maps, 1K props, no 4K maps |
| Visible race view | <=300k triangles, <=150 batches |
| Effects | <=128 live pooled particles; no steady-state managed allocations |
| Application/texture memory | <=650 MB peak app memory; <=128 MB resident textures |
| iPhone target | 60 FPS, p95 frame time <=16.7 ms during 20 minutes of repeated racing |
| Lower graphics profile | 30 FPS, p95 <=33.3 ms; reduce scale/shadows/crowd/VFX, never input/timing |

## 4. Cross-stack changes

1. **CORE:** retain pure C#/.NET Standard 2.1 and the 20 ms step. Add approach/single-release state and course walls; publish explicit launch outcome/timing. Version new rules as **2** and namespace **`reins-v2`**.
2. **CLIENT:** make Reins the only mobile startup scene; remove Classic navigation/binding requirements. Implement pointer handoff, scheduled audio, presentation adapter, rig/camera/HUD and isolated v2 storage. Preserve legacy code/evidence rather than deleting old work.
3. **CONTRACTS/API:** publish v2 request/response schemas for the existing local `/lab/reins/verify` proof with canonical `launchHeld` replacing v1 `GateTap`, initial armed state and launch outcome/error. Update strict parser, canonical hashing, fixture generator, Unity decoder and smoke checks together. Explicitly reject unsupported v1 verification at the v2 verifier rather than silently reinterpreting it; preserve v1 fixtures/evidence as history.
4. **REPLAY/SAVE:** regenerate the four-source fingerprint and a completed v2 fixture. Keep old Classic and `reins-lab-v1` records untouched and isolated; do not automatically migrate their times or award them v2 best status.
5. **DB:** revise the unapplied schema draft's v1-only assumptions and replay compatibility design. No production database migration or service provisioning is part of this milestone.
6. **BUILD/DOCS:** use **0.5.0/build 5** if still unallocated. Update mobile builders, READMEs, evidence and relevant S01–S53 cards together. Use a clean generated iOS export outside cloud-coordinated folders for native compilation; preserve source/engine files and follow the recorded preflight guidance.

## 5. Delivery sequence and acceptance

1. **R2.1 — rules/input:** implement the deterministic approach, release and handoff; run consequential Core/Unity input tests and v2 replay/verifier checks.
2. **R2.2 — visual benchmark:** complete one horse/rider, alley, camera, lighting and walk-to-gallop sequence. Compare like-for-like phone captures before expanding arena content.
3. **R2.3 — full race presentation:** finish arena, turn/Drive animation, audio, HUD, effects and comfort/quality settings.
4. **R2.4 — mobile qualification:** validate both platform artifacts, install on the user's iPhone and record manual play plus measured performance. A named Android handset is required before Android performance is marked verified.

Required tests: approach pose/cue/clock boundaries; Perfect/Good edges and adjacent 20 ms samples; same-tick release/timeout/cancel; sub-step initial touches; never-released/re-press spam; no premature acceleration; first steering/cadence and no accidental Wrap; pointer ownership/slide-off/lifecycle/retry/audio cleanup; legal/wrong-way wall contact and all finish positions; malformed/mixed-version/fingerprint rejection; complete Unity/.NET v2 fixture equality and no inputs after finish; old-save isolation.

Playtest the actual walking rider view, beep/release alignment, left/right barrel turns, Drive, interruption/retry and sound/haptics/reduced-motion settings. Check no obvious hand/rein clipping, foot sliding, camera jolts, detached shadows or obscured targets. Verify equivalent simulation at 30/60/120 Hz rendering. Record profiler/player metrics for the 20-minute session; compilation, visual review and measured device acceptance remain separate gates.

**Done means the launch, controls and improved art work together on the phone.** A successful build or additional procedural decoration alone does not satisfy R2. After R2, continue M1N continuous-input authority proof, then the existing online/progression/content/release sequence.
