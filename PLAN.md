# Gene Color Designer Plan

## Goal
- Create a RimWorld 1.6 mod that adds one or more germline genes with configurable color ranges for pawn appearance.
- When a pawn is first generated, choose a random color from the gene's palette/range.
- When a baby is born, derive the child's available colors from both parents and assign one color from that inherited range.
- Preserve the assigned color so it does not reroll on save/load or routine respawns.
- Cover both skin color and hair color in the overall design, but start with skin only if that materially simplifies the first implementation.

## Scope
- Primary target for v1: skin color.
- Secondary target after skin is stable: hair color.
- Explicitly out of scope for now: eyes, fur, and any custom render channels.
- This is a good fit for RimWorld 1.6 because vanilla already has separate skin and hair gene handling in the decompile.

## Proposed Data Model
- Add a `DefModExtension` to `GeneDef` with:
  - A list of 2 or more colors, stored as hex strings like `#RRGGBB`, for palette-stop mode.
  - Or, preferably for the first pass, explicit hue/saturation/value ranges for range-driven mode.
  - A simple field that says whether the gene affects skin or hair.
  - Optional flags for inheritance mode and weighting.
- Add a custom `Gene` subclass that stores the pawn's resolved color and exposes it through save/load.
- Note on the earlier wording:
  - "A target appearance channel enum" just meant a code field like `Skin` or `Hair`.
  - For this mod, that can be simplified to a plain XML value such as `skin` or `hair`, or even separate defs if that ends up cleaner.

## Color Math
- Do not start with direct RGB interpolation as the primary system.
- For v1, prefer HSV-driven ranges because Unity already exposes `Color.RGBToHSV` and `Color.HSVToRGB`, which keeps the implementation simple and avoids custom HSL conversion code.
- Proposed v1 approach:
  - Let XML define min/max hue, saturation, and value for a gene.
  - On pawn generation, randomly sample within those HSV bounds and convert the result to RGB for the actual pawn color.
  - On birth, derive the child's allowed HSV range from both parents, then sample once from that inherited range.
- Palette-stop mode can still exist later for authored palettes with hand-picked anchor colors.
- HSL is still a reasonable design idea, but HSV is the better first implementation choice here because RimWorld/Unity already use HSV helpers in multiple places.

## Pawn Creation Situations To Cover
- Initial pawn generation through `PawnGenerator`:
  - Starting colonists
  - World pawns
  - Raids
  - Refugees and quest joiners
  - Ritual or event joins
  - Dev-mode generated pawns
- Newborns created through the pregnancy/birth pipeline.
- Gene changes after creation:
  - Xenogerm implantation
  - Dev-mode gene edits
  - Any mod-added gene injection that uses the normal gene tracker APIs
- Situations that should not reroll:
  - Save/load
  - Caravan transfers
  - Map despawn/respawn

## Implementation Phases
1. Use the dnSpy decompile at `F:\Development\rimworld-decomp` to confirm the exact RimWorld 1.6 hook points for skin assignment, birth inheritance, gene add/remove, and graphics refresh.
2. Implement skin-only first:
   - Gene extension
   - HSV range parser
   - Random sampling helpers
   - Save-backed selected color storage
3. Patch the main pawn generation flow so first-time generated pawns resolve skin color once their genes exist.
4. Patch the birth flow so newborns inherit a skin-color range from both parents after their endogenes are finalized.
5. Handle runtime gene changes so adding or removing the skin gene updates the pawn correctly.
6. Add XML defs for a sample germline skin gene and verify it appears in xenotype/gene tooling.
7. Extend the same system to hair color once skin is stable and the hook points are confirmed clean.
8. Test repeated generation, parent combinations, save/load stability, and mod compatibility.

## Technical Notes
- The exact Harmony targets should be verified against the RimWorld 1.6 decompile before coding.
- Confirmed useful vanilla reference points include:
  - `RimWorld.Pawn_GeneTracker.EnsureCorrectSkinColorOverride()`
  - `RimWorld.PregnancyUtility` logic for inheriting melanin and hair-color genes
  - `RimWorld.Building_GrowthVat.EmbryoColor()` for embryo-preview skin handling
- Likely touchpoints include `PawnGenerator`, pregnancy/birth methods, and `Pawn_GeneTracker`.
- The chosen color should be stored on the gene instance or another save-backed pawn object, not recomputed every time graphics resolve.
- Visual updates will need a graphics/story refresh after the color is assigned.
- Hair may need slightly different handling than skin because vanilla already treats hair-color genes as a distinct endogene category.

## Testing Matrix
- Spawn 100 pawns with the skin gene and confirm HSV range sampling behaves as expected.
- Breed parents with identical ranges and confirm the child stays within that range space.
- Breed parents with different ranges and confirm child colors are sampled from the combined inherited range.
- Save/load a colony and confirm assigned colors remain stable.
- Add/remove the gene in dev mode and confirm the pawn updates once without duplicate rerolls.
- Test with pawns generated off-map and then arriving on-map.
- After skin is stable, repeat the same matrix for hair.
