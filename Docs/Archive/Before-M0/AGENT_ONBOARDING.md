> Historical material from before M0. Superseded by Docs/Plan and the current root instructions. Completion grades, engine versions, section numbering and some dimensions below were not validated.

# BARREL RIVALS — AGENT ONBOARDING GUIDE

## 🎯 Engine & Repository Standards
- **Engine:** Unity 2022.3 LTS (URP).
- **Target Mobile Performance:** 60fps on iPhone 12 / Android equivalents, <150 draw calls, <100MB runtime memory.
- **Multiplayer:** Photon Fusion 2.
- **Workflow:** Strictly check `BUILD_STATUS.md` on every interaction to find the next task.
- **AsmDefs:** 8 isolated assemblies (`BarrelRacing.Core`, `.Gameplay`, `.Multiplayer`, `.Progression`, `.UI`, `.Audio`, `.Data`, `.Platform`).
