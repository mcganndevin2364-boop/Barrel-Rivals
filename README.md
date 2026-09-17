# Barrel Rivals

An iOS/Android arcade barrel-racing game in development. The planned experience combines automatic first-person riding with launch timing, remembered drawing challenges, barrel-exit timing and a sprint challenge. Live turns with spectating, recorded challenges and simultaneous duels remain in scope, alongside ten horses, progression and a trusted economy.

## Current build

**M1 first-barrel practice:** Unity 6000.6.0f1, a saved URP arena and proxy horse, hold/release launch, remembered drawing, graded turn/knock, separate exit timing and result/retry. Shared rules and Unity input/rendering checks are recorded in the M1 report. A full race, final art and multiplayer remain ahead; mobile performance has not been measured.

- [Play the current scene and reproduce checks](README-M1.md)
- [iPhone export and test setup](README-iPhone.md)
- [M1 implementation results and limits](Docs/Plan/Barrel-Rivals-M1-Report.md)
- [Master status: eight categories and 53 sections](Docs/Plan/Barrel-Rivals-Master-Build-Status.md)
- [Full build plan](Docs/Plan/Barrel-Rivals-8-Category-Build-Plan.md)
- [Gameplay blueprint](Docs/Plan/Barrel-Rivals-Gameplay-Blueprint.md)
- [Engineering stack and section contracts](Docs/Plan/Barrel-Rivals-Engineering-Playbook.md)

The next work is physical-phone calibration and M1N two-client timing/authority proof, before M2 full-course gameplay and content expansion. A compatible iOS build environment remains an open platform requirement. [README-M0.md](README-M0.md) preserves the foundation preview instructions; open `Arena_Practice` to try the new gameplay.

## Development context

Read [AGENTS.md](AGENTS.md) and the relevant engineering card before implementation. Preserve Unity metadata and the pinned package lockfile. Generated M0 assets have explicit ownership; future hand-authored production art belongs outside the generated directory.

Earlier documents are retained in [Docs/Archive/Before-M0](Docs/Archive/Before-M0). Their old completion grades, Unity 2022.3 baseline, course dimensions and alternate section numbering do not describe this branch’s current implementation.
