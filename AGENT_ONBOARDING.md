# Barrel Rivals development onboarding

1. Read [AGENTS.md](AGENTS.md), [README-M0.md](README-M0.md) and the [M0 decision record](Docs/Decisions/M0-Foundation.md).
2. Check [the current master status](Docs/Plan/Barrel-Rivals-Master-Build-Status.md) and [verification report](Docs/Plan/Barrel-Rivals-M0-Report.md).
3. Use the exact S01–S53 contract in [the engineering playbook](Docs/Plan/Barrel-Rivals-Engineering-Playbook.md), alongside the [gameplay blueprint](Docs/Plan/Barrel-Rivals-Gameplay-Blueprint.md).
4. Build a connected increment, verify its affected boundaries and update evidence. Do not turn existing scaffolding or future stack recommendations into claims of completed systems.

Unity 6000.6.0f1 and Packages/packages-lock.json define the current reproduction baseline. Shared rules compile without Unity against .NET Standard 2.1. Photon Fusion and the backend remain future integration work. Rendering/performance targets require named-device measurements; no locked frame rate or memory budget has yet been demonstrated.
