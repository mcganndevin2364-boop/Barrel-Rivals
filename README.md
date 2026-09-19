# Barrel Rivals

An iOS/Android arcade barrel-racing game in development. The new Reins Lab explores continuous rein steering, cadence, close barrel turns, changing footing and a final drive. Classic Practice preserves the earlier automatic riding/drawing controls for comparison. Live turns with spectating, recorded challenges and simultaneous duels remain in scope, alongside ten horses, progression and a trusted economy.

## Current build

**Classic 0.3.0 checkpoint + Reins Lab source targeting 0.4.0:** Unity 6000.6.0f1, saved URP scenes, original prototype art, isolated rules/saves and a local shared-Core replay verifier. Reins adds a legal three-barrel course; it is an experimental offline game mode. Final horse/rider art, trusted online play, progression/economy and measured mobile qualification remain ahead. Version-labelled evidence distinguishes source/tests, builds, installation and actual phone confirmation.

- [Play the current scene and reproduce checks](README-M1.md)
- [Play Reins Lab and inspect the cross-stack prototype](README-Reins.md)
- [All ten mechanics: stack contracts and delivery gates](Docs/Plan/Reins-Mechanics-Integration.md)
- [iPhone export and test setup](README-iPhone.md)
- [M1 implementation results and limits](Docs/Plan/Barrel-Rivals-M1-Report.md)
- [Master status: eight categories and 53 sections](Docs/Plan/Barrel-Rivals-Master-Build-Status.md)
- [Full build plan](Docs/Plan/Barrel-Rivals-8-Category-Build-Plan.md)
- [Gameplay blueprint](Docs/Plan/Barrel-Rivals-Gameplay-Blueprint.md)
- [Engineering stack and section contracts](Docs/Plan/Barrel-Rivals-Engineering-Playbook.md)

The next work is physical-phone comparison of both controls, then the adapted M1N continuous-input authority proof. The existing Mac can compile/sign/install personal iPhone test builds; debugger compatibility and release toolchain qualification remain open. [README-M0.md](README-M0.md) preserves the foundation preview; `Arena_Practice` and `Arena_ReinsLab` are the playable development scenes. Original folders remain preserved; no changes have been pushed or submitted to a store.

## Development context

Read [AGENTS.md](AGENTS.md) and the relevant engineering card before implementation. Preserve Unity metadata and the pinned package lockfile. Generated M0 assets have explicit ownership; future hand-authored production art belongs outside the generated directory.

Earlier documents are retained in [Docs/Archive/Before-M0](Docs/Archive/Before-M0). Their old completion grades, Unity 2022.3 baseline, course dimensions and alternate section numbering do not describe this branch’s current implementation.
