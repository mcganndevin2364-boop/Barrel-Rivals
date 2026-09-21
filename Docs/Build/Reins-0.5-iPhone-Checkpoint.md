# Reins 0.5 mobile checkpoint

Source/art baseline: `43741bd13cf58b6d022d4a37e9b64a88ef8f8820`, Unity 6000.6.0f1, rules reins-v2, version 0.5.0/build 5. The [connected glove checkpoint](../Art/Reins-Glove-Anatomy-Checkpoint.md) records 140 Editor / 37 Play Mode tests, actual rendered evidence and the remaining art limits. This checkpoint qualifies build stages separately from phone play and performance.

## Fresh iPhone export

Unity exported successfully with **zero build errors**, including the current horse/rider, connected gloves, terrain/fiber materials, warm MyStable and shared equipment. The two player scenes are Arena_ReinsLab and MyStable. Classic stays in source/history and is excluded from the player build.

[Export evidence](../../Evidence/Reins05-iOS-Export.json) verifies version 0.5.0/build 5, device SDK, ARM64, IL2CPP, Metal and the existing development bundle ID. The committed ExportIOS method deliberately switches iOS graphics from Automatic to explicit Metal. Its only serialized settings change in this export was `m_Automatic: 1 -> 0`; no runtime/rules change was introduced.

The previous 0.4 export and signed IPA remain preserved. Unity replaced the initial external-directory symlink during export, so the finished fresh export was moved intact to a task-owned temporary directory after Unity exited. Its PBX hash was checked before native compilation and the default ignored export path now points to that directory. Generated native files and private signing data remain outside source.

The fresh export contained **3,485 canonical runtime source/header files matching the installed engine**, no unmatched/differing files in that check and **zero numbered source copies** to quarantine. The older 0.4 duplicate-repair record remains historical; no engine or gameplay source was repaired for this export.

## Build and device status

The fresh **0.5.0/build 5 iPhone app compiled and signed successfully** with Xcode 15.2 using the previously selected Personal Team. Signature, valid exact-app development profile including the paired phone, ARM64 app/framework binaries, IPA integrity and every archived file were verified. [Signed artifact evidence](../../Evidence/Reins05-iOS-Artifact.json). The profile expires **2026-09-26T02:40:02+00:00**. Installation, physical launch and measured phone performance remain unverified; the last verified installed version is still 0.4.0/build 4.

No new Android APK or named Android handset qualification is included. Existing Android evidence remains 0.4.0. The complete R2 visual/control/performance gate is still open. Source tests, controlled 25 fps capture and native compilation are distinct from measured phone FPS or photographic acceptance.

## Reproduction and review

From the repository root, with other Editors closed:

```bash
bash Tools/run-reins.sh ios
python3 Tools/verify-ios-export.py --version 0.5.0 --build 5 --output Evidence/Reins05-iOS-Export.json
```

Use fresh evidence filenames for a later checkpoint rather than overwriting these accepted records. Unity writes `Builds/iOS/BarrelRivals-Practice`; move a completed fresh export outside cloud-coordinated Documents before Xcode compilation on this host. Preserve the previous known-good export/app. Check the PBX hash and generated runtime contents after relocation. Do not modify installed Unity runtime files to repair a generated export.

The existing `Tools/build-ios-native.sh` performs an unsigned compiler check only. An installable development app uses the Unity-iPhone target, ReleaseForRunning, iphoneos SDK, two compiler jobs, automatic signing and the user's selected Personal Team. Keep build products outside source and use the resolved external export path. Signing credentials, account sessions, device identifiers and raw logs are private local state and are excluded from the source handoff.

Before installing, verify the signature/profile, matching app identifier, valid profile expiry, inclusion of the paired phone, ARM64 app/framework binaries, exact version/build and every file in the IPA against the signed app. Detect the currently attached device again; old connection files do not prove current presence. On this host the previously proven USB route is pymobiledevice3's standard developer app installation service, followed by a targeted installed-version query. A successful install is not a successful launch.

After installation, use [the phone review](Reins-0.5-Phone-Review.md) for launch/beep alignment, rein/cadence/Wrap controls, retry/interruption, safe areas, comfort and local gear persistence. Measure a separate 20-minute session; keep the 60 FPS/16.7 ms p95 target and qualified 30 FPS/33.3 ms fallback. No release section is accepted by compilation alone.
