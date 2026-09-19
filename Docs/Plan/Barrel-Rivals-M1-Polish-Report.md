# Practice polish — 0.3.0 / build 3

## Implemented

The previous 0.2.0 iPhone practice build was confirmed working by the player, who liked its touch feel. This update preserves rules version 1: launch thresholds, shape grading, the fixed four-second drawing window, automatic path, separate exit input and five-second barrel penalty.

- **Retry Same Challenge** keeps the seed. **New Challenge** advances it; the last selected challenge survives restart.
- **Local personal bests** compare the exact seed and rules version, include penalties, preserve ties and retain up to 64 challenges. Writes use a temporary file and replacement; bad/old data does not block practice. Unsaved results are labeled session-only.
- **Own-best ghost** replays the recorded local input timeline using a separate rules instance. Validation reproduces its result before allowing playback. It is explicitly the player's own best, never a live opponent. Ghost display is optional and hides when it would overlap the current horse.
- **Coaching** gives one specific action based on the completed run. The result still shows raw time, penalty, skill grades and target/trace comparison.
- **Feedback** adds restrained grade confirmations, hoof/tack/dirt sounds and native haptics, with separate persistent sound/haptic controls. Launch beeps retain their existing schedule. The drawing surface and camera remain steady.
- **Presentation** adds original dirt textures, shadows, stands, rails, distant terrain, a scaled articulated horse prototype with saddle/bridle, and a bounded pool of hoof-dust particles. No purchased model or rigged human rider has been added.

## Validation

- Engine-independent .NET Standard 2.1 build: passed, zero warnings/errors.
- Unity 6000.6.0f1 scene generation, save, reopen and binding validation: passed.
- Editor tests: **55/55 passed**, including scoring invariants, exact challenge records, deterministic replay, invalid/corrupt storage, coaching and optional feedback behavior.
- First Play Mode run: **5/5 passed**. Includes a full touch launch/draw/exit/result/retry, persistent replay, delayed hold origin, late held-touch completion, cancellation and unrelated challenge isolation, plus the foundation preview checks.
- Final visual pass and corrected overlay capture: **5/5 Play Mode tests passed**; all three renders reviewed.
- iOS 0.3.0 export, native compilation, signature/profile checks, IPA integrity and USB installation: **passed**. A targeted phone query verified 0.3.0/build 3 on September 18, 2026 (Central). The new version's manual launch/playtest remains pending; the earlier 0.2.0 confirmation does not certify it.
- Android 0.3.0 artifact and physical-device performance: pending.

The initial Editor failure expected the old foundation rendering asset. The check now verifies the saved pipeline selected for the configured build scenes across every quality tier. The first render review led to neutral earth/lighting, irregular scenery, better horse scale and capture layering. Those changes were driven by observed images.

## Production graphics

The procedural horse is an interim presentation upgrade. Convincing realism still requires production geometry/materials, a rigged rider, synchronized gait/turn animation, foot contact and refinement on real phones. The user reviewed the paid options and chose **free assets only for now**. No purchase has been made or is authorized. See [horse and rider options](../Art/Horse-and-Rider-Options.md).

The paid Horse Animset Pro and Realistic Horse Herd candidates are deferred. Evaluate free commercially usable horse/animation sources and original production art. Free assets still need license, appearance, animation and phone-performance checks. Imported animation must follow our race state and path rather than replace the gameplay controller.

## Remaining build sequence

The user subsequently requested ten revised mechanics. The separate Reins Lab now precedes the adapted M1N authority proof; see [the integration plan](Reins-Mechanics-Integration.md). This report remains the completed classic-practice 0.3.0 checkpoint. M2 still requires a polished complete race and representative finished horse/rider/arena. Quick Duel/rematch, recorded friend challenges, Championships, stable/customization, progression and release/store work retain their existing dependencies. No full-release section is marked complete by this practice update.

## Device acceptance still needed

Confirm readable bodycam/drawing/results, useful sound levels, restrained haptics, same/new challenge behavior, best replay after restarting, interruption recovery and sustained frame pacing/temperature. Editor success is not a physical-device benchmark.
