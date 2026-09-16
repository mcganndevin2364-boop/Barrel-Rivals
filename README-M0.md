# Barrel Rivals — recovered Unity foundation

Open this project with **Unity 6000.6.0f1**. It is an isolated repair branch based on GitHub `b77f4e5`, with the original local Unity assets/settings preserved and reconciled. Your original project folders remain unchanged.

## Open and try the scene

1. In Unity Hub, add this folder as a project: `/Users/devinmcgann/Documents/Codex/2026-09-16/fix-x20/outputs/Barrel-Rivals-M0`.
2. Open `Assets/_Project/Generated/Foundation/Arena_Foundation.unity`.
3. Enter Play Mode. Select **Preview ride** to move automatically toward the first barrel with a temporary first-person camera. Select **Reset view** to restore the starting state.

This is the M0 course preview. The horse and arena use simple prototype geometry. Drawing, launch/exit timing, animation, a complete race and multiplayer are the next increments; this scene does not award scores or currency.

## Reproduce the checks

Run from this project folder. Close any other Unity instance using this same project before a batch invocation. Other project folders are independent.

```bash
Tools/run-unity.sh validate
Tools/run-unity.sh editmode
Tools/run-unity.sh playmode
Tools/run-unity.sh render
Tools/run-unity.sh android
python3 Tools/verify-android.py
```

`UNITY_EDITOR` can override the installed Editor executable. `BARREL_ANDROID_TOOLCHAIN` can override the AndroidPlayer SDK/NDK/OpenJDK folder for the artifact inspector (build-tools 36.0.0). These commands use the saved scene. `Tools/run-unity.sh generate` deliberately regenerates the builder-owned foundation scene/materials/prefabs; save hand-authored production content outside that generated directory. Generating again resets development settings such as bundle IDs and the scene build list.

Test reports and the overview render are in `Evidence/`. The development APK is produced at `Builds/Android/BarrelRivals-Foundation.apk`. Raw Unity logs remain ignored in `Logs/m0/`; do not publish licensing/session data from those logs.

Compile the engine-independent core separately with an installed .NET SDK:

```bash
dotnet build Tools/CoreCheck/BarrelRivals.Core.csproj --ignore-failed-sources
```

The target is .NET Standard 2.1/C# 9. This check uses no Unity references. It does not install or implement the future ASP.NET service.

## Scope and evidence

The initial import, generated scene/reference checks, 10 Edit Mode tests and two Play Mode preview/input tests passed. The ARM64 Android development APK built with 0 reported errors and passed package/signature/alignment checks. See [the dated M0 report](Docs/Plan/Barrel-Rivals-M0-Report.md) for Android artifact status and remaining platform checks. The touch test injects events into a temporary touchscreen and verifies rendered button hit-testing; it temporarily enables foreground-style input routing in the unattended Editor and restores the settings afterward. The Play Mode command uses graphics for this UI check. Physical touch/performance tests require actual phones. The available Mac currently lacks the full, compatible Xcode environment needed to qualify the iOS build.

`Docs/Decisions/M0-Foundation.md` explains ownership, package decisions and the temporary preview. `Docs/Plan/` contains the master plan and engineering guidance. A passing foundation test does not mark an entire original section or the complete game finished.
