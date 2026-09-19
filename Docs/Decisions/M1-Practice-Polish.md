# M1 practice polish — 0.3.0 / build 3

The player confirmed that the installed 0.2.0 iPhone practice build works and that its touch feel is good. Preserve the launch thresholds, drawing grader, fixed four-second draw window, automatic path and separate exit tap. The shared practice rules remain version 1.

## Connected upgrade

- Retry repeats the exact seed; New Challenge advances it. The last challenge survives restart.
- Personal bests use the exact seed and rules version, include the five-second knock penalty, preserve ties, and retain up to 64 entries locally. Save failure leaves the current session playable and is labeled in the HUD. These are not ranked results or rewards.
- The best run records a bounded input timeline relative to the beginning of the hold. Playback must reproduce the saved result under the same rules before it can become a ghost. The mint horse is explicitly the player's own best; it is hidden when overlapping the current horse/camera and can be turned off.
- Results give one specific next action based on observed launch, trace or exit performance.
- Optional sound/haptics persist separately. Audio uses bounded voices and owned procedural clips; native short impacts have no authority over timing. Scheduled launch beeps remain unchanged and respect mute.
- Original generated dirt textures, arena structures, scenery, lighting, bounded hoof-dust puffs and an articulated procedural horse improve the practice presentation. This horse is a prototype, not final realistic art. No external asset has been purchased.

## Production art decision

After reviewing the paid options, the user chose **free assets only for now**. No purchase is authorized. Prioritize original work and free assets with verified commercial-use rights; record any required attribution. Evaluate appearance, turn/canter/gallop/sprint clips, rider synchronization, saddle/bridle, mobile LODs and URP support. An animation/controller asset supplies presentation; it must not replace the shared race rules or take ownership of movement. Verify Unity6000.6 / URP17.6 / Metal and Android graphics in a small integration scene before adopting it.

## Historical roadmap at the 0.3.0 checkpoint

This is an improvement to M1, not completion of the release scope. M1N still proves two-client authority, independent clocks and challenge privacy. M2 then builds the full three-barrel run, rhythmic sprint, a finished horse/rider and representative arena. Quick Duel/rematch, recorded friend challenges, Championships, stable/progression and store work retain their existing milestone dependencies.

## Validation record

See the new polish report and Evidence files for actual results. A successful compile/render does not establish phone frame pacing, heat, audio latency or haptic quality. Do not reuse 0.2.0's user-confirmed launch as proof that 0.3.0 was launched.

The later ten-mechanic request adds an opt-in Reins Lab ahead of the adapted authority proof. See [the current integration plan](../Plan/Reins-Mechanics-Integration.md); this decision remains the Classic 0.3.0 record.
