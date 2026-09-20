# Reins v2 moving alley — source checkpoint

September 20, 2026. **0.5.0/build 5 source configuration; no new native artifact or phone installation.** R2 remains incomplete: representative art, native/device timing, ergonomics and sustained performance still need acceptance. The last installed phone build remains 0.4.0/build 4.

## Implemented across the stacks

- CORE: Ready at `(0,-6)`; accepted center hold starts a four-second, six-metre walk at 1.5 m/s. Tick 200 is exactly `(0,0)`, race time zero. First free movement is tick 201. Rein/brake/cadence inputs cannot alter the approach.
- Launch: one intentional release, Perfect ±120 ms, Good ±240 ms, Weak outside; signed error retained. Holding through GO+400 ms gives explicit TimedOut with null release error. Down/up inside one step resolves at tick 1. Re-press cannot rearm. Cancel takes precedence. Same-tick release beats timeout.
- Launch benefit: positive acceleration only, ×1.35 Perfect / ×1.15 Good; late release affects the following step. Braking and speed caps stay intact. All benefit ends at GO+1,200 ms.
- CLIENT: center hold owns startup; launch pointer cannot become cadence/Wrap. Side rein owners persist through GO; release/retry/interruption clears the appropriate state. Rein full-pull travel is 50% of pad height, minimum 80 units. Cadence starts GO+600 ms, with ±60/100/140 ms grades and 360–500 ms periods.
- AUDIO: three dedicated voices scheduled at accepted-start DSP time +2/+3/+4 seconds; separate cadence voice. Retry/cancel removes queued cues. Muted visual cues remain. Physical speaker/touch alignment is unmeasured.
- WORLD: visible alley uses shared wall bounds x±3, z[-12,-1.5], radius .05. Swept horse/capsule contact slides, including wrong-way endcap approaches. Full finish gate remains clear, without a painted score line or rail penalty.
- CONTRACTS: explicit v2 envelope, initial armed hold, launchHeld frames, launch outcome/error, strict parser and canonical digests. Six-source fingerprint includes alley and StandardCourse. Completed v2 replay agrees in Unity/.NET. Historical v1 contracts remain byte-for-byte intact.
- STORAGE/BUILD: isolated reins-v2 local bests, no v1 time migration. Reins and MyStable are the only player scenes; historical Classic/Foundation remain testable source. Version configured to 0.5.0/build 5. Unapplied DB draft updated; no service provisioned.

## Executed checks and evidence

135/135 Editor tests, 32/32 Play Mode tests and 78/78 local HTTP checks passed. The first Play Mode attempt exposed an uncaught malformed-record exception; the loader/saver now explicitly catches InvalidDataException, keeps incompatible bytes, and uses the verified session best when possible. The full rerun passed. Canonical v2 run: **33,860 ms, 0 knocks, 300 style, Perfect launch at error 0, 1,893 frames**. Fingerprint `759406550bc144d59973d0e26573d532614551bbe7a1b7bbacb2307a956bd274`. HTTP proof is local consistency only, not authenticated competitive authority.

- [Source/test checkpoint](../../Evidence/AlleyV2-Checkpoint.json)
- [Editor results](../../Evidence/AlleyV2-EditMode.xml) / [Play Mode results](../../Evidence/AlleyV2-PlayMode.xml)
- [HTTP results](../../Tools/ReinsServerCheck/verification-evidence.v2.json)
- [Actual moving alley capture](../../Evidence/AlleyV2-Launch.mp4) / [per-frame poses and capture method](../../Evidence/AlleyV2-Launch-Capture.json)
- [Separate motion study](../../Evidence/AlleyV2-Motion.mp4) / [encoded frame verification](../../Evidence/AlleyV2-Video-Verification.json)
- [Current race Ready](../../Evidence/AlleyV2-Race-Ready.png), [first turn](../../Evidence/AlleyV2-Race-Turn-1.png), [finish](../../Evidence/AlleyV2-Race-Finish.png)
- [MyStable](../../Evidence/AlleyV2-MyStable.png), [saddles](../../Evidence/AlleyV2-SaddlesEquipped.png), [rider gloves](../../Evidence/AlleyV2-RiderEquipped.png)
- [Historical integrity](../../Evidence/AlleyV2-Historical-Integrity.json): all 302 earlier tracked evidence files restored exactly.

The launch movie renders the actual saved scene, actual ungraded overlay HUD, first-person camera and skinned model. It samples fixed 20 ms simulation/animation steps at 25 encoded frames/sec, with a player-loop yield before each rendered frame. Its 6.44 seconds include Ready, approach, GO and first steering. It has no sound track and is not a device-FPS measurement. The separate controlled motion study remains a presentation inspection, not production gait acceptance.

## Remaining work

The horse, foreground hands/reins, mane, distant crowd and ground still fall short of the photographic references. Existing free/original assets and provenance are preserved. One horse, 20 local cosmetics across five slots, preview/equip/cancel and stable inspection exist; roster, leveling, paid ownership and gear performance advantages are not implemented.

Next: inspect and refine the moving rider/horse benchmark, then produce clean 0.5 platform artifacts. Test actual iPhone release/audio alignment, interruptions, thumb reach and repeat races; measure the 20-minute performance gates. Android needs a named test handset. Full rider anatomy/animation, planted turns/braking, further material/LOD optimization and reference-quality acceptance remain open. Do not mark R2 or any whole S01–S53 release section complete from source tests. M1N authority/network proof follows R2 acceptance; multiplayer, trusted progression/economy, cloud saves and release work remain in the eight-category plan.
