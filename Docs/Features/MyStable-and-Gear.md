# MyStable and Gear — connected source increment

September 20, 2026. Continues the existing Reins project and v1 simulation. The user requested both screens while continuing toward premium racing graphics. Their MyStable/Gear reference photos will be uploaded later; the current layout is an original western design, not an asserted match to unseen references.

## Player flow

From a ready or completed Reins run, choose **MY STABLE**. Copper appears as an animated 3D horse inside an original timber stable. Drag the horse to rotate; **RESET VIEW** restores the initial angle. **GEAR** or **CUSTOMIZE GEAR** opens six free starter cosmetics: two western saddles, two woven-pad colorways and two rein-braid colors. Selecting an item previews it. **EQUIP GEAR** saves it. Returning to MyStable discards an unequipped preview; **RIDE TO ARENA** uses equipped choices. Stable navigation is unavailable during an active attempt.

Saddle and pad meshes now exist on both the stable and racing horse; racing reins use the selected braid material. The seated player's saddle/pad render shadows only, preventing the cantle from filling the first-person camera. Stable and ghost horses retain visible tack. A future multi-camera spectator system will need per-camera visibility layers rather than reusing this owner-only setting. The stable uses stowed reins with no floating rider hands. It shares the existing rig, textures and idle clip with the race. This is one horse, not a ten-horse collection disguised as color swaps. Cosmetic choices do not change race traits, timing, collision, replay results or competitive power.

## Stack and persistence

- **CORE / CONTENT:** `Packages/com.barrelrivals.core/Runtime/Stable/StableCatalog.cs` defines stable IDs, an immutable six-item catalog, slot validation and a versioned cosmetic profile. It has no Unity/API dependency. This starter collection is freely available; it is not a client implementation of paid ownership.
- **CLIENT / STORAGE:** `StableProfileStore` stores only `stable-cosmetics-v1.json` in Unity's persistent-data directory, independently of all Classic/v1 race saves. Valid writes flush to a temporary file and replace the primary atomically, retaining a prior valid backup. Reads are limited to 16 KiB. Corrupt/missing data falls back to a backup or starter setup. Unrecognized/newer data is preserved; unavailable storage keeps a clearly reported session-only choice. Snapshots cannot mutate the store.
- **CLIENT / PRESENTATION:** `StableController` owns inspect/equip/navigation and landscape safe-area layout. `StableAppearance` resolves material assets on the horse without modifying shared material assets. The racing controller reads equipped appearance before creating its private monochrome ghost; the ghost strips the cosmetic binder and never follows later profile edits. The v1 replay remains a timing recording, not a historical cosmetic snapshot.
- **ART / BUILD:** Original western saddle/pad, stowed reins and stable geometry are generated from versioned editor code. Photographic wood/leather/soil assets reuse the already verified CC0 sources. No new downloads, purchases, service subscriptions or third-party logos. The new `MyStable` scene is included in both mobile build routes. Existing 0.4/build 4 identity is unchanged; no new native artifact is implied.
- **API / DB:** This increment makes no cloud, transaction, currency, entitlement, bond, level or ranked-power writes. Competitive gear upgrades still require a versioned frozen trait/loadout contract and trusted settlement. Do not treat cosmetic preferences or legacy `EconomyManager` scaffolding as an authority for those features.

## Verification

Unity 6000.6.0f1 generated, saved and reopened the race/stable scenes. **89/89 Editor tests and 17/17 Play Mode tests passed.** The shared Core also compiled independently for .NET Standard 2.1 with zero warnings/errors. Evidence: [Editor XML](../../Evidence/StableGear-EditMode.xml), [Play Mode XML](../../Evidence/StableGear-PlayMode.xml), [Core build/source hashes](../../Evidence/StableGear-CoreBuild.json).

The tests cover invalid/wrong-slot IDs, immutable catalog/profile snapshots, equip/restart round trips, atomic backups, corrupt and oversized files, protection of newer saves, blocked storage, and unchanged historical race bytes. Unity checks also exercise preview cancellation, explicit equip, disk reload, race materials, unchanged default traits, unavailable stable navigation during a run, drag-to-rotate and gear-button raycast reachability. Existing controls, own-best ghosts and the full canonical v1 replay pass; the canonical result remains **35,120 ms, zero knocks, 300 style, 2,156 frames**.

Actual 1280 × 720 Unity renders on the Mac are included: [MyStable](../../Evidence/StableGear-MyStable.png), [gear preview](../../Evidence/StableGear-GearPreview.png), [equipped gear](../../Evidence/StableGear-GearEquipped.png). Visual review corrected a detached saddle-seat shape, excessive floor highlights, dark interior lighting an oversaturated leather tint and first-person saddle occlusion. The initial naming failure was fixed before the passing suite. The stable uses one shadowed sun and one unshadowed point fill; those are not baked-lighting or phone-performance evidence.

All 58 earlier image/JSON/XML evidence files were restored and verified byte-for-byte. Fresh full-race capture copies use `StableGear-Race-*` to distinguish them from the earlier graphics checkpoint. No new iOS/Android export, install, phone frame-time measurement or visual acceptance is claimed. The horse anatomy/mane and current saddle study still look stylized and require substantial art work against the photographic target.

## Remaining scope

This is a connected feature increment toward the user's full game, not completion of S05, S08, S13, S43 or S50. The exact MyStable/Gear layouts await the user's photos. Natural horse/rider anatomy, mane and gaits, stable props/texture refinement, advanced gear models, ten distinct horses, earned upgrades and progression, all multiplayer modes, trusted services, phone profiling and store readiness remain open. The current horse is still visibly below the photographic reference. Complete the planned v2 alley/release and benchmark character work alongside subsequent progression integration; do not restart the project or replace this working equipment flow with static mockups.

## Reproduction

Generate the race and stable together with `BarrelRivals.Editor.ReinsLabBuilder.Generate`. `BarrelRivals.Editor.StableBuilder.Generate` regenerates only MyStable from the already saved race horse. Preserve historical Evidence images/JSON/XML before full tests because earlier test classes still write old filenames. Use fresh `StableGear-EditMode.xml` and `StableGear-PlayMode.xml` for this source, then restore the earlier evidence bytes.
