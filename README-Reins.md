# Barrel Rivals — Reins Racing

The current source configures **0.5.0/build 5**, Reins rules **v2**, and Reins-only player startup plus MyStable. The last installed iPhone app is still **0.4.0/build 4**. Read [the current source/test checkpoint](Docs/Plan/Reins-v2-Alley-Checkpoint.md) for evidence and limits.

## Play the current source

Open `Assets/_Project/Generated/ReinsLab/Arena_ReinsLab.unity` in Unity 6000.6.0f1.

1. Choose footing, sound, reduced motion and your own-best ghost before starting.
2. Hold the center pad. Walk down the alley in first person; beeps sound at two, three and four seconds. Release on the third beep as the horse reaches the invisible starting plane. Early/late releases grade and continue.
3. Pull a side rein to turn; pull both to brake. Tap the center heartbeat with the other thumb. The launch finger must lift before becoming a new cadence touch.
4. Near a barrel, hold center for 300 ms to Wrap. It trades speed/rhythm for tighter turning, within a two-second budget per barrel.
5. Circle left around the first barrel, then right around the second and third. Geometric contact adds five seconds once per knocked barrel.
6. After three legal turns, alternate side taps during Drive and ride through the finish between the posts.
7. Retry repeats the same conditions. Valid completed personal bests are local and can play as your own ghost. They award no coins, rank or ownership.

Reins v2 records are separate from Classic and `reins-lab-v1`; their times are never migrated. Sound/comfort settings do not change Core results. Actual phone timing and thumb ergonomics still require playtesting.

## MyStable, Tack and Rider Gear

Choose **MY STABLE** while Ready or after a completed/stopped run. Inspect the one starter horse, rotate the view, open Tack or Rider Gear, preview a card, then explicitly equip it. An unequipped preview is discarded. Twenty original free cosmetic styles across saddle, pad, reins, headstall and gloves persist locally and appear on the next ride. Cosmetics do not modify racing performance. The warm barn and item-grid reference guides presentation; photographic quality and earned progression remain unfinished. [Feature details](Docs/Features/MyStable-and-Gear.md).

## Development and verification

Use only one Unity Editor against this project. `Tools/run-reins.sh generate` rebuilds the saved Reins/Stable scenes and thumbnails; it requires graphics. `validate`, `editmode`, `playmode`, `android` and `ios` are separate operations. Test output names use `Reins-v2-*`; some historical capture tests still write older image names, so back up those bytes before running and collect new evidence under a fresh prefix. Controlled gait/movie tests require fresh output paths as documented in their source.

`Tools/ReinsServerCheck/run.sh build` compiles shared Core and the bounded loopback verifier. `serve` binds 127.0.0.1 only; `smoke` exercises contracts. `fingerprint` deliberately writes the six-source hash; generate a new fixture only for an intentional rules change, never to mask a regression. V1 fixtures/evidence are historical and must remain unchanged.

Build configuration is not an APK/IPA or device acceptance. Native exports, caches, signing material and raw editor logs are excluded from Git. Read [the handoff](Barrel-Rivals-AI-Handoff.md), [master status](Docs/Plan/Barrel-Rivals-Master-Build-Status.md) and [approved 0.5 plan](Docs/Plan/Reins-Racing-0.5-Implementation.md) before further implementation.

<!-- REINS05_ANDROID -->
Fresh **0.5.0/build 5 iPhone and Android development packages are verified**. iPhone export/native compilation/signature/IPA integrity and Android version/signature/ARM64/16 KB alignment checks passed. Neither new package has been installed or playtested on a phone; the last verified installed iPhone app remains 0.4.0/build 4. See [the Android checkpoint](Docs/Build/Reins-0.5-Android-Checkpoint.md) for the current build/verification commands and [the iPhone checkpoint](Docs/Build/Reins-0.5-iPhone-Checkpoint.md).
