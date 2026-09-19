# Practice 0.3.0 asset provenance

| Content | Source | Runtime ownership |
| --- | --- | --- |
| Horse body/head/neck, articulated joints, saddle and bridle | Original procedural geometry in PracticePresentationBuilder; Unity primitive geometry for some fittings | Generated persistent meshes/materials; animation only changes visual children |
| Dirt albedo/normal, dust mask | Original deterministic texture code in PracticePresentationBuilder and PracticeBuilder | Generated persistent texture assets |
| Arena rails, stands, props, crowd silhouettes, distant ridge | Original procedural scene/mesh construction using Unity primitive meshes where appropriate | Static merged scenery batches; no new race collision authority |
| Gait/head/ear/tail/lean motion | Original presentation code in PracticeHorseVisual | Observes the unchanged race root; no movement or grading authority |
| Hoof/tack/dirt/grade audio | Original procedural synthesis in PracticeFeedback; launch tones retained | Fixed audio pool; created clips cleaned up at destruction |
| Haptic bridges | Project source using UIKit and Android system feedback APIs | Optional local feedback; no external SDK or permission to vibrate in the background |
| Rendering | Pinned Unity URP and built-in procedural sky shader | Project's saved practice pipeline and persistent material references |

No third-party horse, rider, animation pack, image, recording or texture was purchased or imported for this update. The prototype is not finished realistic art. Production candidates are documented separately; record a receipt, license type/seat, publisher version and import evaluation when an asset is approved and acquired. Do not redistribute raw purchased assets outside the applicable license.
