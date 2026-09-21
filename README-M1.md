> Historical Classic/0.4 guidance. Current Reins-only 0.5 builds and evidence are linked from [README](README.md) and [the Reins guide](README-Reins.md). Do not use the 0.4 version arguments below to validate a new 0.5 artifact.

# Barrel Rivals — first-barrel practice

M1 continues development in this same recovered repository. The folder is still named `Barrel-Rivals-M0`; the active implementation branch is `codex/m1-skill-loop`. Original source folders remain preserved. The [AI handoff](Barrel-Rivals-AI-Handoff.md) and publication receipt identify the current branch/checkpoint. This document preserves Classic development history; the user has selected Reins as the main game.

## Classic checkpoint: 0.3.0 / build 3

The current source adds practice progression and presentation polish while preserving the controls the user liked in 0.2.0. The launch timing, drawing grader, four-second drawing window, automatic path and separate exit tap remain rules version 1. **0.3.0 checkpoint verified:** scene generation/reopen validation, 55/55 Editor tests and 5/5 Play Mode tests passed. Native compilation, development signature, IPA integrity and installation were verified; the phone query confirmed 0.3.0/build 3. Its manual phone playtest remains pending. The separately requested Reins Lab is documented in [the integration plan](Docs/Plan/Reins-Mechanics-Integration.md).

The current combined release for personal testing is **0.4.0/build 4**, with both Classic and Reins. Its iPhone installation and Android artifact checks passed. The user has played and prefers Reins; detailed phone qualification remains pending. Use [README-Reins.md](README-Reins.md) and the combined build commands below for current mobile work. The legacy Classic-only builders/menu entries retain the old version and single-scene scope; they are not the current mobile build route.

## Play the Classic scene

1. Add `/Users/devinmcgann/Documents/Codex/2026-09-16/fix-x20/outputs/Barrel-Rivals-M0` to Unity Hub and open with **6000.6.0f1**.
2. Open `Assets/_Project/Generated/Practice/Arena_Practice.unity` and press Play.
3. Hold **HOLD TO BEGIN**, then release on the third beep/GO. Remember the gold shape, draw it in the pad, then lift. Make a fresh tap when the exit bar reaches its center.
4. Review time, penalty, skill grades, your best for this challenge and one coaching tip. **Retry** repeats the same seed; **New Challenge** selects the next seed.
5. Use **SOUND**, **HAPTICS** and **GHOST** to set your preferences. The mint ghost shows your own validated best for the same seed and rules; it becomes available after an eligible saved best and hides when overlapping your horse.

Touch and mouse use the same UI path. App interruption cancels this offline practice run. The arena now has original dirt textures, scenery and structures; the horse is an articulated procedural prototype. This scene stops after the first barrel; it does not award currency, ranking or a full-course result.

The existing `Arena_Foundation` scene remains the M0 approach/reset preview. Use `Arena_Practice` for the new gameplay.

## iPhone testing

**Historical 0.2.0 / build 2:** the app was built, development-signed and installed on the user’s iPhone 17 Pro. The user confirmed it opens, works and has good touch feel. That confirmation applies to 0.2.0. **The 0.4.0/build 4 app is installed and version-verified. The user has since played and selected Reins; this qualitative report does not establish a complete device test matrix or measured performance.** The 0.3.0 native checkpoint is retained separately. Use [the iPhone setup guide](README-iPhone.md) for the established personal-testing route. The Android APK is a separate artifact and cannot run on an iPhone.

## Reproduce checks and the Android build

Close other Editors using this same project before running these commands from the repository root:

```bash
Tools/run-reins.sh validate
Tools/run-reins.sh editmode
Tools/run-reins.sh playmode
Tools/run-reins.sh android
python3 Tools/verify-android.py --apk Builds/Android/BarrelRivals-ReinsLab.apk --output Evidence/Reins-Android-Artifact.json --version 0.4.0 --code 4
```

`Tools/run-m1.sh generate` deliberately regenerates the builder-owned practice scene from the foundation, including the practice HUD and bindings. It removes the added Reins navigation from Classic; follow it with `Tools/run-reins.sh generate` before exporting the combined build. Keep hand-authored content outside generated directories. `UNITY_EDITOR` overrides the Editor path; `BARREL_ANDROID_TOOLCHAIN` overrides the Android inspector's SDK/NDK/OpenJDK root. M0 commands and artifact defaults remain available.

The combined Android build targets **0.4.0/code 4** at `Builds/Android/BarrelRivals-ReinsLab.apk`. That APK passed version/signature/ARM64/16 KB alignment checks; it has not been run on an Android phone. Both use the existing development package ID, so installation replaces an older development build with that ID. The build configuration is ARM64 IL2CPP with debug signing. Builds and raw licensing logs are ignored; sanitized evidence stays in `Evidence/`.

The rules remain in `Packages/com.barrelrivals.core`; compile them separately with `dotnet build Tools/CoreCheck/BarrelRivals.Core.csproj --ignore-failed-sources`. No Unity references or network service are needed for that check.

## Local practice records

The last selected seed persists across restart. Up to 64 personal bests are stored on this device, keyed by exact seed and rules version. Comparisons include the five-second barrel penalty; tied runs retain the existing best. A bounded input replay must reproduce its saved result before being shown as an own-best ghost. Invalid replay data is discarded, and a save failure leaves practice playable with an on-screen warning. These records do not sync to a cloud account or award rank/currency.

## Evidence and next work

Read [the M1 report](Docs/Plan/Barrel-Rivals-M1-Report.md), [decision record](Docs/Decisions/M1-Practice.md), [0.3.0 polish decisions](Docs/Decisions/M1-Practice-Polish.md) and [53-section status](Docs/Plan/Barrel-Rivals-Master-Build-Status.md). Actual Unity screenshots are in `Evidence/M1-Ready.png`, `M1-Preview.png` and `M1-Result.png`.

The user’s positive 0.2.0 touch feedback guides this update; 0.3.0 audio/haptics, own-best replay, persistence and appearance still need a phone playtest. Cue latency, gesture calibration, safe areas and sustained performance remain to be measured. The user chose free assets only for now. No final horse/rider asset has been purchased; [asset options and limitations](Docs/Art/Horse-and-Rider-Options.md) record the art research. The approved [0.5 plan](Docs/Plan/Reins-Racing-0.5-Implementation.md) supersedes that earlier comparison sequence: build the Reins moving alley/release launch and representative rigged art/full-course polish, qualify on phone, then prove continuous-input authority in M1N before online competition. The [integration plan](Docs/Plan/Reins-Mechanics-Integration.md) defines the current gates. The local iOS build/sign/install route works for personal testing; Xcode debugger compatibility and release qualification remain open.
