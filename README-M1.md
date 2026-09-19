# Barrel Rivals — first-barrel practice

M1 continues development in this same recovered repository. The folder is still named `Barrel-Rivals-M0`; the active implementation branch is `codex/m1-skill-loop`. Original source folders remain preserved, and no changes have been pushed to GitHub.

## Current build: 0.3.0 / build 3

The current source adds practice progression and presentation polish while preserving the controls the user liked in 0.2.0. The launch timing, drawing grader, four-second drawing window, automatic path and separate exit tap remain rules version 1. **0.3.0 checkpoint verified:** scene generation/reopen validation, 55/55 Editor tests and 5/5 Play Mode tests passed. Native compilation, development signature, IPA integrity and installation were verified; the phone query confirmed 0.3.0/build 3. Its manual phone playtest remains pending. The separately requested Reins Lab is documented in [the integration plan](Docs/Plan/Reins-Mechanics-Integration.md).

## Play the current scene

1. Add `/Users/devinmcgann/Documents/Codex/2026-09-16/fix-x20/outputs/Barrel-Rivals-M0` to Unity Hub and open with **6000.6.0f1**.
2. Open `Assets/_Project/Generated/Practice/Arena_Practice.unity` and press Play.
3. Hold **HOLD TO BEGIN**, then release on the third beep/GO. Remember the gold shape, draw it in the pad, then lift. Make a fresh tap when the exit bar reaches its center.
4. Review time, penalty, skill grades, your best for this challenge and one coaching tip. **Retry** repeats the same seed; **New Challenge** selects the next seed.
5. Use **SOUND**, **HAPTICS** and **GHOST** to set your preferences. The mint ghost shows your own validated best for the same seed and rules; it becomes available after an eligible saved best and hides when overlapping your horse.

Touch and mouse use the same UI path. App interruption cancels this offline practice run. The arena now has original dirt textures, scenery and structures; the horse is an articulated procedural prototype. This scene stops after the first barrel; it does not award currency, ranking or a full-course result.

The existing `Arena_Foundation` scene remains the M0 approach/reset preview. Use `Arena_Practice` for the new gameplay.

## iPhone testing

**Historical 0.2.0 / build 2:** the app was built, development-signed and installed on the user’s iPhone 17 Pro. The user confirmed it opens, works and has good touch feel. That confirmation applies to 0.2.0. **The new 0.3.0 / build 3 native build and installation are verified; its phone playtest remains pending.** Use [the iPhone setup guide](README-iPhone.md) for the established personal-testing route. The Android APK is a separate artifact and cannot run on an iPhone.

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

The Android build command now targets **0.3.0 / code 3** at `Builds/Android/BarrelRivals-Practice.apk`. The last verified APK remains **0.2.0 / code 2** until a new build and artifact inspection complete. Both use the existing development package ID, so installation replaces an older development build with that ID. The build configuration is ARM64 IL2CPP with debug signing. Builds and raw licensing logs are ignored; sanitized evidence stays in `Evidence/`.

The rules remain in `Packages/com.barrelrivals.core`; compile them separately with `dotnet build Tools/CoreCheck/BarrelRivals.Core.csproj --ignore-failed-sources`. No Unity references or network service are needed for that check.

## Local practice records

The last selected seed persists across restart. Up to 64 personal bests are stored on this device, keyed by exact seed and rules version. Comparisons include the five-second barrel penalty; tied runs retain the existing best. A bounded input replay must reproduce its saved result before being shown as an own-best ghost. Invalid replay data is discarded, and a save failure leaves practice playable with an on-screen warning. These records do not sync to a cloud account or award rank/currency.

## Evidence and next work

Read [the M1 report](Docs/Plan/Barrel-Rivals-M1-Report.md), [decision record](Docs/Decisions/M1-Practice.md), [0.3.0 polish decisions](Docs/Decisions/M1-Practice-Polish.md) and [53-section status](Docs/Plan/Barrel-Rivals-Master-Build-Status.md). Actual Unity screenshots are in `Evidence/M1-Ready.png`, `M1-Preview.png` and `M1-Result.png`.

The user’s positive 0.2.0 touch feedback guides this update; 0.3.0 audio/haptics, own-best replay, persistence and appearance still need a phone playtest. Cue latency, gesture calibration, safe areas and sustained performance remain to be measured. The user chose free assets only for now. No final horse/rider asset has been purchased; [asset options and limitations](Docs/Art/Horse-and-Rider-Options.md) record the art research. The next engineering milestone is M1N: two-client timing/authority and independent slow-motion proof. M2 then connects a complete three-barrel race and sprint with a finished horse/rider and representative arena. The local iOS build/sign/install route works for personal testing; Xcode debugger compatibility and release qualification remain open.
