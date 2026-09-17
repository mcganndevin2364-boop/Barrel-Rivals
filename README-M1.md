# Barrel Rivals — first-barrel practice

M1 continues development in this same recovered repository. The folder is still named `Barrel-Rivals-M0`; the active implementation branch is `codex/m1-skill-loop`. Original source folders remain preserved, and no changes have been pushed to GitHub.

## Play the current scene

1. Add `/Users/devinmcgann/Documents/Codex/2026-09-16/fix-x20/outputs/Barrel-Rivals-M0` to Unity Hub and open with **6000.6.0f1**.
2. Open `Assets/_Project/Generated/Practice/Arena_Practice.unity` and press Play.
3. Hold **HOLD TO BEGIN**, then release on the third beep/GO. Remember the gold shape, draw it in the pad, then lift. Make a fresh tap when the exit bar reaches its center.
4. Review time, penalty and skill grades. **Retry** starts a new practice challenge.

Touch and mouse use the same UI path. App interruption cancels this offline practice run. The horse and arena are placeholder geometry. This scene stops after the first barrel; it does not award currency, ranking or a full-course result.

The existing `Arena_Foundation` scene remains the M0 approach/reset preview. Use `Arena_Practice` for the new gameplay.

## Reproduce checks and the Android build

Close other Editors using this same project before running these commands from the repository root:

```bash
Tools/run-m1.sh validate
Tools/run-m1.sh editmode
Tools/run-m1.sh playmode
Tools/run-m1.sh android
python3 Tools/verify-android.py --apk Builds/Android/BarrelRivals-Practice.apk --output Evidence/M1-Android-Artifact.json
```

`Tools/run-m1.sh generate` deliberately regenerates the builder-owned practice scene from the foundation, including the practice HUD and bindings. Keep hand-authored content outside generated directories. `UNITY_EDITOR` overrides the Editor path; `BARREL_ANDROID_TOOLCHAIN` overrides the Android inspector's SDK/NDK/OpenJDK root. M0 commands and artifact defaults remain available.

The development APK is `Builds/Android/BarrelRivals-Practice.apk`, version 0.2.0/code 2, using the existing development package ID. Installing it replaces an installed M0 development APK with that ID. It is ARM64 IL2CPP, debug-signed and not a store submission. Builds and raw licensing logs are ignored; sanitized evidence stays in `Evidence/`.

The rules remain in `Packages/com.barrelrivals.core`; compile them separately with `dotnet build Tools/CoreCheck/BarrelRivals.Core.csproj --ignore-failed-sources`. No Unity references or network service are needed for that check.

## Evidence and next work

Read [the M1 report](Docs/Plan/Barrel-Rivals-M1-Report.md), [decision record](Docs/Decisions/M1-Practice.md) and [53-section status](Docs/Plan/Barrel-Rivals-Master-Build-Status.md). Actual Unity screenshots are in `Evidence/M1-Ready.png`, `M1-Preview.png` and `M1-Result.png`.

Phone feel, cue latency, gesture calibration, safe areas and sustained performance remain to be measured. The next engineering milestone is M1N: two-client timing/authority and independent slow-motion proof. M2 then connects a complete three-barrel race and sprint with a finished horse/rider and representative arena. The full iOS build environment remains an open platform requirement.
