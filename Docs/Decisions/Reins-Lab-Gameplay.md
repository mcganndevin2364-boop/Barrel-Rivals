# Reins Lab gameplay decision — historical 0.4 contract

**Superseded product defaults, September 19, 2026:** the user selected Reins as the main game, first-person throughout, realistic arcade visuals and forgiving controls. [The approved 0.5 plan](../Plan/Reins-Racing-0.5-Implementation.md) now governs implementation (gameplay revision 5 / engineering revision 6). Its moving alley and single hold/release replace the three-tap gate described below. Classic remains historical source/saves and leaves the next player build. This record preserves why the implemented 0.4 prototype was built; its pending-choice/additive-Lab clauses are no longer active instructions.

Gameplay revision 4 / engineering revision 5. This decision applies the user's attached **Barrel Rivals — Reimagined Gameplay Mechanics** document to the complete-game plan. The attachment supplies ten requested mechanics and illustrative tuning. Its claims about addiction, gambling psychology and commercial success are proposals, not evidence or instructions to maximize compulsive play.

## Current product choice

All ten mechanics remain in scope. Pending the user's optional preference answer, the working default is an **additive, opt-in Reins Lab**, preserving the working 0.3.0 Classic Practice checkpoint. This is a recommendation for comparison/playtesting, not confirmation that the user has chosen permanent replacement. The future lead ruleset is selected after controls, readability, fair outcomes and repeat-play feedback are measured.

Classic keeps `PracticeRun.RulesVersion == 1`, remembered drawing, hold/release launch, its own personal bests and its existing replay format. New continuous-control work uses a separate **`reins-lab-v1`** ruleset namespace, configuration hash, records and replay stream. A matching integer version alone never makes the two compatible. Classic own-best data must not be imported as a Reins rival or resimulated under Reins rules.

The second pending preference is the streak economy. The safe working recommendation is **capped earned streak bonuses**; existing winnings are never put at risk, and premium loss insurance is excluded. No wallet, purchase or online reward is activated by the Lab. A different stake policy would require a new explicit design decision before implementation.

## Accepted adaptations of the ten-mechanic proposal

| # / original name | Current design contract |
|---|---|
| 1 — Reins Tension | Two side zones capture normalized left/right rein tension. Differential tension steers; combined tension brakes. Counter-rein handling is a bounded movement/grip technique to test, not a probability modifier or purchased input advantage. |
| 2 — Cadence Gallop | Center-pad presses grade against a versioned beat schedule. Hot/Blazing Hooves are bounded movement boosts. Current Core periods are 300–500 ms (2–3.33 Hz), with ±40/80/140 ms windows; these and percentage gains are provisional tuning, not established mobile-accessibility targets. |
| 3 — Barrel Pocket / Barrel Kiss | Clearance is measured between the horse's collision bounds and the barrel's bounds, not center distance alone. Legal route progress and actual swept contact decide rewards/knocks; no hidden RNG rolls. Kiss means a narrow clean pass. A visual wobble cannot independently flip a trusted result. |
| 4 — Gate Break | Three wave-peak taps replace hold/release **inside the Lab**. False breaks produce an explicit bounded standstill/recovery outcome with spam/cooldown rules. Launch boosts change movement; there are no negative time credits. The source heading says hold/release but its described action is three taps; the action is the adopted interpretation. |
| 5 — Leg Wrap | Holding the center pad for 300 ms near a barrel activates bounded turning control while the other thumb steers. The initial press is one cadence event; later held Wrap is a separate timeline event and generates no repeated cadence. It trades future cadence opportunities/speed for control. Flash Wrap remains a technique to validate, not collision immunity. |
| 6 — Dirt Read | A disclosed, versioned per-round surface map controls bounded speed/grip. Both riders receive the identical map, including mixed patches. Degradation occurs only between rounds and equally for both riders; cosmetic ruts from the first run never modify the second run's physics. |
| 7 — Home Stretch Drive | Capped, alternating left/right taps act only after legal barrel-three completion. Same-side repeats earn no extra boost. The proposed 14+ taps/second tier is an extreme research case, not the required/default route to competitive performance. Attainable cadence, fatigue and accessible equivalents need phone tests. |
| 8 — Horse IQ | Nerve, Fire, Biddability and Heart influence bounded movement, grip, response curves and round effort. They do not add an artificial 80 ms input delay, random knock risk, paid timing windows or automatic steering. Core trait parameters exist; a Lab horse picker, bond/stamina progression UI and bond persistence are planned. Trusted settlement must own later grants. |
| 9 — Rival Ghost Pressure | Compatible recorded rivals/own-best ghosts provide optional visual/audio drama only. Ahead/behind never changes timing windows, readability, grip or Drive thresholds. Gap means elapsed-time difference at the same valid course progress; show unavailable until measurable. Avoid a second-rider information advantage by using equal preassigned references or postponing the first run's rival ghost until both finish. |
| 10 — Streak Stakes | Preserve the streak-progression concept as capped, earned-only bonus milestones pending user choice. The source table and recursive example disagree and compound exponentially. Use a frozen base reward plus fixed extra milestones; no re-multiplication of the accumulated balance, existing-winnings loss or premium insurance. |

Two-thumb play is a release gate. The current adapter has LEFT/RIGHT drag pads plus a CENTER cadence/Wrap pad. One thumb holds the turning rein while the other taps center or holds it for 300 ms near a barrel. Both rein pulls brake and occupy both thumbs, temporarily sacrificing cadence/Wrap; no phase requires all three pads simultaneously. Gate taps center, and Drive alternates the side pads. Test reach, changing turning hands, slide-off, interruption, small screens, thumb occlusion and menus on phones. A two-zone exclusive tap/drag classifier may be compared later; it is not implemented or the current accepted API.

Current prototype geometry uses a 0.42 m horse circle and 0.40 m barrel circle (0.82 m contact radius), with a 1.0–1.2 m center-distance Kiss band. These are provisional source values. They imply a clearance band only for these simplified bounds; production hulls and horse/barrel art must be reconciled before calling contact realistic. Local own-best replay is wired; rival gap presentation/network, bond previews and streak calculators are not.

## Authority and presentation boundaries

The intended Core experiment is engine-independent C#/.NET Standard 2.1 with a **20 ms fixed simulation step**. Fixed stepping alone is not proof of cross-runtime reproducibility. Accepted timestamp/sequence ordering, numeric quantization, configuration hashes and replay fixtures must agree between Unity IL2CPP and the later verifier. Unity interpolates/renderers and emits effects from accepted events; Unity physics and frame rate do not decide competitive results.

Race time is elapsed validated simulation from the published start cue through legal course completion, plus one five-second penalty per knocked barrel and any explicit ruleset foul term. Prefer foul standstill inside the simulation so it is not counted again as a time penalty. Movement boosts cannot subtract arbitrary milliseconds from the result. Per-barrel clearance, legal wrap direction/progress, contact and exit reward each have one authoritative event identity.

No competing player's position alters the active player's control or score contract. Surface and horse manifests are frozen before the round; stamina can follow a disclosed deterministic between-round schedule. Bond/gear/trait changes from later rewards become eligible only in a later match. Continuous analog input and ghost data require new verifier/replay contracts, not a shortcut through the Classic drawing grader.

## Delivery order and limits

1. **R0:** checkpoint the current 0.3.0 Classic code/assets/evidence and preserve its play route.
2. **R1:** Reins Lab mechanical prototype, with all ten contracts mapped and missing mechanics explicitly listed. Verify Core, saved scene, actual touch ownership and replay isolation. Bounded traits and local own-best playback do not complete horse learning, bond/streak UI or rival competition; those remain planned until separately implemented and verified.
3. **R2:** validate controls/art on phones and polish the full three-barrel course/Drive with one representative horse/rider/arena. This carries the previous M2 complete-course scope; do not scale a full roster yet.
4. **M1N:** adapt the two-client authority experiment to continuous inputs, round-equal footing, honest progress gaps, compatible ghost disclosure and fixed-step result agreement.
5. **M3–M7:** online modes, trusted progression/economy/bond, full content and release qualification with their existing dependencies.

Use original work or free assets with verified commercial-use rights and required attribution. No paid horse/rider asset or new paid service is authorized. The proposed networking/backend/database/IAP stack is not deployed. Source, Editor tests, native builds, physical runs and service tests must each receive their own evidence; this design document marks none of them complete.

See [the ten-mechanic integration specification](../Plan/Reins-Mechanics-Integration.md), [current blueprint](../Plan/Barrel-Rivals-Gameplay-Blueprint.md) and [53-section engineering cards](../Plan/Barrel-Rivals-Engineering-Playbook.md).
