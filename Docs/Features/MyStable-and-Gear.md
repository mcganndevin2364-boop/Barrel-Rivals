# MyStable and Gear — reference-driven source increment

Latest art-only revision: [mane crown/coverage checkpoint](../Art/Reins-Mane-Flow-Checkpoint.md). The same horse has fuller fitted hair in stable/racing/ghost; current captures and 135 Editor / 32 Play Mode checks use ManeFlow-*. Existing 20 cosmetics and feature contracts remain unchanged. Reference-quality art and progression are still unfinished.


Current gameplay integration is now rules v2 / configured 0.5.0 build 5. [The v2 checkpoint](../Plan/Reins-v2-Alley-Checkpoint.md) includes fresh Stable/Tack/Rider Gear captures and regressions; no new native/device install is claimed. The historical v1 race result below belongs to the original feature checkpoint. Cosmetic-v2 storage remains independent from gameplay-v2 storage.

The later [renderer/lighting checkpoint](../Art/Reins-Lighting-Flow-Checkpoint.md) fixes missing post resources and verifies actual ungraded HUD capture for MyStable/Tack/Rider Gear. Cosmetic contracts remain unchanged; its new captures and checks are separate from the historical records below.

The subsequent [groom checkpoint](../Art/Reins-Hair-Groom-Checkpoint.md) adds layered hair with stable Idle motion and reduces racing-hand material slots. Current cosmetic IDs, preview/equip/cancel, save isolation and frozen race appearance remain intact. Its fresh evidence is separate from the historical results below.

The subsequent [surface-fit checkpoint](../Art/Reins-Surface-Fit-Checkpoint.md) refines the same horse and fitted western headstall across MyStable and racing. It has fresh visual/test evidence; the feature implementation and earlier results below retain their original scope. No additional horses, earned upgrades or online ownership were added.

The latest [art-fit source](../Art/Reins-Art-Fit-Checkpoint.md) fits mane/forelock strands and a contoured saddle against the live horse, corrects strand shading, and routes the neutral stowed reins around the neck. It preserves the same 20 local cosmetics and connected UI; use its separate evidence for the new visuals.

The later [visual-depth checkpoint](../Art/Reins-Visual-Depth-Checkpoint.md) improves the window/lantern lighting and torso-bound saddle, with fresh stable/gear captures and 110 Editor / 21 Play Mode checks. The counts below describe their historical feature boundary; the full roster, earned progression and photographic art remain unfinished.

A later [character-motion source](../Art/Reins-Character-Motion-Checkpoint.md) replaces the rigid mane/tail, corrects animation culling and records fresh stable/race evidence. The counts and images below retain this earlier stable-reference checkpoint; they are not silently replaced by later captures.

September 20, 2026. The user has now supplied the three-panel reference: a warm timber stable with a horse roster and traits, an illustrated saddle collection with an equip inspector, and a rider glove collection with a close-up view. That image guides the new live scene and UI. Its pixels, named horses, levels, rarity and stat bonuses are not imported or represented as already implemented features. At this historical feature checkpoint racing used `reins-lab-v1` and native identity 0.4.0/build 4. **Scene generation, desktop tests and real Unity captures are verified below; device and reference-quality acceptance remain open.**

## Player flow

From a ready or completed Reins run, choose **MY STABLE**. Copper remains the one available horse. The enclosed timber showroom adds a window, framing, stalls, original furnishings and straw, using the previously reviewed photographic materials. Drag Copper to rotate and use **RESET VIEW** to restore the angle. The roster lists the real horse; the right panel shows the current Nerve, Fire, Biddability and Heart base ratings. **SKILLS** explains those existing traits, including Heart's later-round role. Earned levels and training are not implemented.

**TACK** opens a collection with **SADDLES**, **PADS**, **REINS** and **HEADSTALLS** filters. Each card uses a Unity-rendered thumbnail of the actual equipped mesh and material. The inspector shows the selected style and an explicit **EQUIP** action. **APPEARANCE** opens the pad collection. **RIDER GEAR** displays the original rotatable glove/forearm preview and six glove styles. Equipped glove and headstall colors also bind to the racing presentation.

The catalog now contains **20 free local cosmetics in five slots**: eight saddle colorways, two pads, two reins, two headstalls and six gloves. They reuse the current base meshes; they are not 20 distinct equipment models or performance upgrades. Every preview starts from the saved equipped loadout. **EQUIP** saves the selected slot. Changing category replaces an unequipped preview, **BACK TO STABLE** cancels it, and **RIDE TO ARENA** uses saved choices. Stable navigation remains unavailable during an active attempt. There are no purchase, currency, rarity or stat-boost actions in this flow.

The horse, stable and ghost keep visible tack; the seated player's saddle/pad render shadows only so the cantle cannot obstruct the first-person camera. The stable uses stowed reins without floating rider hands. Its horse still shares the current low-detail rig and idle clip with racing. Cosmetic selection never changes collision, steering, launch timing, race traits or replay results.

## Stack and persistence

- **CORE / CONTENT:** `Packages/com.barrelrivals.core/Runtime/Stable/StableCatalog.cs` defines immutable stable IDs, the 20-item catalog, five slots and `StableProfile` version 2. The original three enum values and six IDs remain stable. This engine-independent data describes freely available cosmetics, not trusted account entitlements.
- **CLIENT / STORAGE:** `StableProfileStore` writes `stable-cosmetics-v2.json`. A valid original `stable-cosmetics-v1.json` or recovery copy migrates its saddle, pad and rein choices and supplies default headstall/glove styles. The original files remain untouched. Writes flush before atomic replacement and preserve recovery data. Reads are size-bounded; unrecognized/newer or unreadable data is preserved and changes become session-only when storage cannot be used safely. This is **cosmetic schema v2, independent of the now implemented Reins gameplay/replay v2**. Classic and Reins race-save namespaces remain separate.
- **CLIENT / PRESENTATION:** `StableController` binds cards by catalog ID, controls inspect/equip/cancel and scene navigation, fits the landscape design inside safe bounds, and directs horse/glove inspection. `StableAppearance` applies saved material references without modifying shared assets. The race reads appearance at scene initialization; its private monochrome ghost removes the cosmetic binder. A race replay remains a timing recording rather than a historical cosmetic snapshot.
- **ART / BUILD:** `StableBuilder` integrates `StableShowroomBuilder`, `StableUiBuilder`, `StableWardrobeArtBuilder` and `StableThumbnailBuilder`. Original geometry and recolors reuse the reviewed CC0 wood/leather/soil sources and existing licensed fonts. All catalog thumbnails come from actual Unity rendering; missing sprites fail generation instead of displaying invented illustrations. The `MyStable` scene remains included in both mobile build routes. No new paid assets, services or reference-image pixels are introduced.
- **API / DB:** There is no cloud wallet, ownership transaction, currency, bond, level or ranked-power write. Competitive upgrades still require the versioned frozen trait/loadout contract and trusted settlement. Local preferences and legacy `EconomyManager` are not an authority for those future systems.

## Verification status

Unity 6000.6.0f1 generated, saved and reopened the connected scenes. **101/101 Editor and 17/17 Play Mode tests passed.** [Editor XML](../../Evidence/StableReference-EditMode.xml), [Play Mode XML](../../Evidence/StableReference-PlayMode.xml), [independent .NET Standard Core build](../../Evidence/StableReference-CoreBuild.json) and [checkpoint/hash record](../../Evidence/StableReference-Checkpoint.json) establish this source boundary. Core compilation completed with zero warnings/errors.

Checks cover all five slot IDs, invalid combinations, old-save migration without modifying its bytes, newer/unreadable/corrupt file protection, backup recovery, session-only storage, preview cancellation, equip/restart, both racing glove bindings, headstall/rein materials and active-run navigation restrictions. UI checks cover card reachability, horse/glove orbit and reset, readable brand text, visible glove framing and 4:3 fitting. The canonical v1 replay remains **35,120 ms, zero knocks, 300 style, 2,156 frames**; its fresh capture record is [here](../../Evidence/StableReference-Race-Run.json).

Actual engine captures: [MyStable](../../Evidence/StableReference-MyStable.png), [saddles](../../Evidence/StableReference-SaddlesPreview.png), [headstalls](../../Evidence/StableReference-HeadstallPreview.png), [rider gloves](../../Evidence/StableReference-RiderEquipped.png) and [4:3 rider layout](../../Evidence/StableReference-RiderTablet.png). Visual review corrected horse/glove framing, stale render-target scaling, text clipping and unpreserved emission keywords. The existing URP package sets linear light intensity from the already selected Linear color space; the saved GraphicsSettings value now matches it. The enclosed stable still uses one directional light and one unshadowed point fill; emissive surfaces do not constitute a baked-lighting result.

All **76 historical** PNG/JSON/XML records were restored byte for byte after testing. Fresh race renders use `StableReference-Race-*`; new stable evidence uses `StableReference-*`. These renders remain development art: coarse mane and glove anatomy, simplified saddle silhouettes, window exposure and repeated surface detail still fall short of the supplied reference.

The **previous six-item checkpoint** passed 89/89 Editor tests and 17/17 Play Mode tests, plus independent .NET Standard Core compilation. Those historical results are [Editor XML](../../Evidence/StableGear-EditMode.xml), [Play Mode XML](../../Evidence/StableGear-PlayMode.xml) and [Core evidence](../../Evidence/StableGear-CoreBuild.json). Historical captures are [MyStable](../../Evidence/StableGear-MyStable.png), [gear preview](../../Evidence/StableGear-GearPreview.png) and [equipped gear](../../Evidence/StableGear-GearEquipped.png). They precede the supplied stable/rider reference and do not establish acceptance of this newer 20-item implementation. Earlier race evidence and canonical v1 results retain their own source boundaries.

No new iOS/Android export, installation, moving-video review, phone frame-time, memory or thermal qualification is claimed. The reference has arrived, but matching its realism remains unfinished: the horse anatomy/mane, tack modeling, hand realism, gaits, material detail and final light treatment still need substantial work. Current screenshots must show the real engine output rather than a substitute mockup.

## Remaining full-game scope

This is another connected increment toward S05, S08, S13, S43 and S50; it does not complete those sections or reduce the eight-category/53-section plan. Natural horse/rider anatomy and animation, ten distinct horses, coat/marking customization, earned upgrades, training and progression, broader apparel, all three multiplayer experiences, trusted services, phone qualification and store readiness remain open. The reference's level, rarity and bonus concepts belong in those future validated systems rather than decorative fake values. Refine the current Reins v2 moving alley/release and benchmark character work alongside subsequent progression integration.

## Reproduction

Use one Unity 6000.6.0f1 Editor at a time. Generate the race and stable together with `BarrelRivals.Editor.ReinsLabBuilder.Generate`; `BarrelRivals.Editor.StableBuilder.Generate` rebuilds only MyStable from the already saved race horse. Preserve historical Evidence image/JSON/XML bytes before full tests because older tests write their original filenames. Use distinct new evidence names for this reference increment, and restore older files afterward. Do not confuse the cosmetic profile's version 2 with a native release version or completion of `reins-v2`.

## Shared tack after the seated rider integration

The [rider source checkpoint](../Art/Reins-Rider-Body-Checkpoint.md) shortens the saddle fenders and makes the stirrup openings face forward beneath the boots. MyStable, rendered horse portrait and item thumbnails share this geometry; the showroom remains horse-only, matching the reference composition. Existing one-horse/20-style/five-slot preview/equip/persistence scope is unchanged. The new fixed racing rider does not implement additional shirt, hat or boot selection. Fresh stable/equip regression and screenshots are included in RiderBody-* evidence; native and photographic-quality acceptance remain pending.

## Connected glove inspection and riding poses

The [glove source checkpoint](../Art/Reins-Glove-Anatomy-Checkpoint.md) shares CC0-derived palm/finger anatomy between the open inspector and closed riding hands. All six glove thumbnails are actual renders of the same inspectable geometry/materials. Softer leather finish replaces excessive speckled highlights; no new texture is imported. The inspection assembly drops from 4,192 triangles/three renderers to 2,537/two. Six-style preview/equip/cancel/persistence and race appearance isolation pass again, with current GloveAnatomy-* evidence. This does not add gear categories, ownership, bonuses or photographic-quality/device acceptance.

## Offline replacement candidate

The [fitted horse groom study](../Art/Reins-Horse-Groom-Study.md) now includes eyes, bay coat, mane, forelock and tail in editable offline source. It does not yet replace the stable or racing horse and adds no new equipment/progression functionality. The next integration uses explicit bone/renderer bindings, reviewed URP materials and refitted tack/rider/camera in a separate Unity benchmark; preserve the current 20-style catalog, preview/equip/cancel and save behavior.
