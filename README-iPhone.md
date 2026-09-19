# Barrel Rivals — iPhone practice build

**Installed on the user's iPhone 17 Pro:** Barrel Rivals **0.4.0, build 4**, containing Classic Practice and the new Reins Lab. Native compilation, signature/profile, IPA integrity and installed version were verified on September 19. The user previously confirmed 0.2.0 opens and has good touch feel. The user has since played and prefers Reins; that qualitative report is recorded separately in `Evidence/Reins-User-Feedback-2026-09-19.json`. Formal control acceptance and measured device performance remain pending; older automated evidence retains its original timestamps and limits.

## Open the installed app

The initial trust step is complete for this installation. If a future test build needs developer verification, open **Settings → General → VPN & Device Management**, select your Apple Account under **Developer App**, and follow **Trust / Verify App** and any additional prompts. Keep an internet connection available for verification, then open **Barrel Rivals** again. [Apple verification guidance](https://support.apple.com/en-us/118254).

For the new mode, select **TRY REINS RACING → BEGIN RUN**. Pull the side pads down to steer, tap the center pad to the beat, and hold center near a barrel for Leg Wrap. Follow the map through three legal turns, alternate side taps for Drive, then cross the finish. See [the full control guide](README-Reins.md). **CLASSIC PRACTICE** returns to drawing practice.

In Classic Practice, hold **HOLD TO BEGIN**, release on the third beep/GO, remember and draw the gold shape, lift your finger, then make a fresh tap when the exit bar reaches its center. Retry repeats the same challenge; New Challenge advances the seed. This is offline one-barrel practice with placeholder art; multiplayer, purchases and ranked rewards are not enabled.

The current development profile expires **September 26, 2026 at 02:40 UTC** (September 25 at 9:40 PM Central). A refreshed development profile and reinstall will be needed for testing beyond its validity. The signed IPA is `../BarrelRivals-iPhone-Practice-0.4.0.ipa`; it is a personal-device test artifact, not a store submission.

## Verified host and device

- MacBookPro14,1 (2017 Intel), 8 GB RAM, macOS Ventura 13.7.8.
- Unity 6000.6.0f1 with Android, iOS and Mac support retained after storage cleanup.
- Xcode 15.2 (15C500b), with license, first-launch setup and iOS 17.2 runtime installation complete.
- iPhone 17 Pro, iOS 26.6.2 (23G90), wired, paired, available and Developer Mode enabled.
- The user's Personal Team is selected in the generated Unity-iPhone target. One valid development identity and an exact app-ID profile including this phone were verified.

## Build and installation evidence

1. Unity exported the saved `Arena_Practice` and `Arena_ReinsLab` scenes with IL2CPP, ARM64, Metal, device SDK, iOS 15+ deployment target and the existing development bundle ID `com.barrelrivals.foundation`.
2. Installing Apple's iOS 17.2 runtime resolved the storyboard/asset compiler failure. The complete unsigned app then compiled successfully.
3. An actual-device Xcode scheme build timed out during device preparation. The supported target-build path successfully signed the app using the user's selected team and existing valid profile.
4. `codesign --verify --deep --strict` passed. App/framework ARM64 binaries, signed entitlements, profile expiry, exact application ID and phone inclusion were checked. The IPA archive passed its integrity check.
5. Xcode's `devicectl` installation failed because its developer image lacks the requested variant for this iPhone. This is a measured limitation of the local Xcode device tools, not a failure of the game compiler or provisioning profile.
6. **pymobiledevice3 11.15.5** installed the same signed IPA through the ordinary USB installation service. The targeted phone query confirmed bundle ID and version 0.4.0/build 4. No developer-image mount, jailbreak, firmware change or signing bypass was used. [Maintainer's installation-service explanation](https://github.com/doronz88/pymobiledevice3/discussions/821), [CLI reference](https://doronz88.github.io/pymobiledevice3/cli/apps/).

The current evidence is `Evidence/Reins-iOS-Export.json`, `Reins-iOS-Native.json` and `Reins-iOS-Artifact.json`. Version-prefixed M1-Polish files preserve the 0.3.0 checkpoint; the unprefixed M1 native/device/artifact files preserve historical 0.2.0 checks. Private device identifiers, provisioning data and raw tool logs remain outside tracked source.

For 0.4.0, Xcode initially waited on a coordinated read of the project under Documents. An identical export copy in a task-owned temporary directory cleared that wait. Native compilation then exposed numbered duplicate generated sources. Only 119 byte-identical copies were quarantined from that temporary export; all 1,122 canonical runtime source/header files matched installed Unity. The repaired temporary build compiled, signed and installed successfully. Project `Assets`/`Packages`, the installed engine and original export were not changed by this repair. The PBX input hash and repair are recorded in the native evidence. A repeated export should be checked for these duplicates before native compilation.

## Rebuild the Unity export

With other Editors for this project closed, run from the repository root:

```bash
Tools/run-reins.sh ios
python3 Tools/verify-ios-export.py --version 0.4.0 --build 4 --output Evidence/Reins-iOS-Export.json
```

The generated project is `Builds/iOS/BarrelRivals-Practice/Unity-iPhone.xcodeproj`. Regeneration can replace manual signing settings, so reselect the user's Personal Team afterward. Do not invent another account/team or commit certificates/profiles.

`Tools/build-ios-native.sh` reproduces the **unsigned compiler check only**. For a signed target build, use `Unity-iPhone`, `ReleaseForRunning`, `iphoneos`, automatic signing and the selected development team. Allow Xcode to update the development profile as needed. Do not pass the unsigned runner's `CODE_SIGNING_ALLOWED=NO` overrides for an installable app. Reuse the existing native build output where appropriate.

The proven USB install command is `python -m pymobiledevice3 apps install --udid <connected-phone-UDID> --developer <signed-IPA>`. Follow it with `apps query --udid <connected-phone-UDID> com.barrelrivals.foundation` to verify the installed version. The task-local Python environment contains pymobiledevice3 11.15.5 and uses the existing compatible cryptography 48.0.1 library; this tool is not embedded in the game.

## Remaining acceptance work

The user confirmed earlier 0.2.0 play and now reports playing/preferring the Reins prototype after the 0.4.0 installation. That supports the product choice, but is not a complete acceptance matrix. The approved 0.5 plan makes Reins primary; Classic comparison is no longer the next product gate. Verify a complete practice attempt, retry, app interruption, touch ownership, cue timing, safe areas, comfort and sustained performance on the phone. Xcode debugger/device-image compatibility remains unresolved on this old toolchain. This successful compile/sign/install path does not establish App Store readiness. Store publishing needs its own membership, supported build host and current upload requirements. [Apple upload requirements](https://developer.apple.com/news/upcoming-requirements/).
