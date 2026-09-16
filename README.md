# Barrel Rivals

An iOS/Android arcade barrel-racing game in development. The planned experience combines automatic first-person riding with launch timing, remembered drawing challenges, barrel-exit timing and a sprint challenge. Live turns with spectating, recorded challenges and simultaneous duels remain in scope, alongside ten horses, progression and a trusted economy.

## Current build

**M0 foundation:** Unity 6000.6.0f1, a saved URP arena, prototype horse, first-barrel approach and reset controls. Compilation, scene/reference checks and the recorded Editor tests pass. The complete race and multiplayer are still to be implemented; mobile performance has not been measured.

- [Open the project and reproduce checks](README-M0.md)
- [Implementation results and platform limits](Docs/Plan/Barrel-Rivals-M0-Report.md)
- [Master status: eight categories and 53 sections](Docs/Plan/Barrel-Rivals-Master-Build-Status.md)
- [Full build plan](Docs/Plan/Barrel-Rivals-8-Category-Build-Plan.md)
- [Gameplay blueprint](Docs/Plan/Barrel-Rivals-Gameplay-Blueprint.md)
- [Engineering stack and section contracts](Docs/Plan/Barrel-Rivals-Engineering-Playbook.md)

The next gameplay increment is M1: one connected launch/draw/turn/exit loop. M1N then verifies two-client clocks and trusted results before content expansion. Physical Android checks and a compatible iOS build environment remain open platform requirements.

## Development context

Read [AGENTS.md](AGENTS.md) and the relevant engineering card before implementation. Preserve Unity metadata and the pinned package lockfile. Generated M0 assets have explicit ownership; future hand-authored production art belongs outside the generated directory.

Earlier documents are retained in [Docs/Archive/Before-M0](Docs/Archive/Before-M0). Their old completion grades, Unity 2022.3 baseline, course dimensions and alternate section numbering do not describe this branch’s current implementation.
