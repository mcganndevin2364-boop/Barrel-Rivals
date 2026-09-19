# Barrel Rivals development onboarding

The user has selected **Reins Racing**, first-person throughout, with realistic arcade presentation and forgiving controls. Continue this repository. The implemented checkpoint is **0.4.0/build 4**; the approved **0.5.0/build 5** moving-alley launch and art rebuild are not implemented yet.

Read in this order:

1. [Portable AI handoff](Barrel-Rivals-AI-Handoff.md): checkpoint, user choices, environment, evidence and next task.
2. [AGENTS.md](AGENTS.md): current development boundaries.
3. [Master status](Docs/Plan/Barrel-Rivals-Master-Build-Status.md) and [approved 0.5 plan](Docs/Plan/Reins-Racing-0.5-Implementation.md).
4. [Gameplay blueprint](Docs/Plan/Barrel-Rivals-Gameplay-Blueprint.md), [eight-category plan](Docs/Plan/Barrel-Rivals-8-Category-Build-Plan.md) and relevant S01–S53 cards in [the engineering playbook](Docs/Plan/Barrel-Rivals-Engineering-Playbook.md).
5. [Current 0.4 controls/build commands](README-Reins.md), [implementation evidence](Docs/Plan/Reins-Lab-Implementation.md), [wire contracts](Contracts/Reins/README.md) and [local verifier](Tools/ReinsServerCheck/README.md).

Gameplay revision 5 / engineering revision 6 supersede the earlier additive-Lab, three-wave gate and third-person-intro defaults. Historical M0/M1/0.4 reports describe earlier implementation and evidence, not the current product direction. Do not overwrite their measured results with new claims.

First implementation task: R2.1 in the approved 0.5 plan—shared approach/release rules, versioned replay/verifier and Unity pointer/audio handoff. R2.2 is the one-horse/rider/alley visual benchmark; do not scale the roster before its quality and device gates pass.

Unity 6000.6.0f1 and the package lockfile define the reproduction baseline. Core remains engine-independent .NET Standard 2.1. Photon, production backend/database, ranked economy and cloud services are future integration work. The user prefers the played Reins prototype, but sustained device performance and formal full-course acceptance are unmeasured.
