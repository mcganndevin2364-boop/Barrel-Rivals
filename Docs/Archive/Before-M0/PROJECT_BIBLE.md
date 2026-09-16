> Historical material from before M0. Superseded by Docs/Plan and the current root instructions. Completion grades, engine versions, section numbering and some dimensions below were not validated.

# BARREL RIVALS — PROJECT BIBLE & SYSTEM SPECIFICATIONS

## Core Game Loop & Architecture
- **Multiplayer Architecture:** Photon Fusion 2 Live 1v1 turn-based / spectator tension loop (Hunting Sniper style).
- **Core Timing Mechanics:**
  - **Gate Launch:** 3 beeps, 3rd beep randomized 0.5-2.0s after 2nd. ±0.1s Perfect (+15% boost), Early tap = +5s penalty.
  - **Alley Acceleration:** Auto-motion 15-20mph ramp-up to gallop (30-35mph).
  - **Rate Tap:** Expanding/shrinking ring sweet-spot at 0.8 normalized.
  - **Pattern Draw:** 7 drawing shapes with gesture recognition.
  - **Sprint Rhythm:** 6-10 taps/second rhythm meter to photo finish.
- **WPRA Dimensions:** 60ft between B1-B2, 90ft to B3, 30ft start line, 15ft fence clearance.
- **Target Framerate:** 60 FPS locked on iPhone 12, <150 draw calls.
