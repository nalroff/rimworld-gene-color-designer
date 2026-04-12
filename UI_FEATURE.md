# Configurable Germline Gene UI Strategy

## Goal

Add support for a configurable template germline gene that can be designed in RimWorld's custom xenotype UI and then applied by this mod's existing color inheritance system.

The feature should let a player:

- Add a template skin-color germline gene in the xenotype editor
- Open a configuration UI for that gene
- Author a palette or HSV range for the gene
- Save that design with the custom xenotype
- Have pawns with that gene resolve and keep a stable color from the designed profile
- Have babies continue to use the mod's inherited color behavior

## Scope

### In Scope For v1

- One configurable template skin-color germline gene
- Configuration UI inside or alongside `Dialog_CreateXenotype`
- Palette mode
- HSV range mode
- Save/load of designed profiles for custom xenotypes
- Copying a designed profile onto pawn gene instances
- Full compatibility with the current runtime color-resolution pipeline

### Out Of Scope For v1

- Hair-color template genes
- Multiple configurable template genes
- Custom icons, custom biostats, or custom gene categories per design
- Identity-level heredity for profile definitions
- Runtime editing of existing pawns outside xenotype creation

## Current Mod Baseline

The existing mod already handles the runtime parts needed for this feature:

- `Source/GeneColorInheritance/Genes/Gene_SkinColorRange.cs`
  - Stores a save-backed resolved color
- `Source/GeneColorInheritance/Genes/GeneColorInheritanceUtility.cs`
  - Samples colors, applies skin overrides, and blends parent colors
- Existing Harmony patches
  - Cover pawn generation, birth, and gene refresh

The implementation work for this feature is primarily about attaching designer-authored profile data to the template gene and carrying that data cleanly from xenotype creation into gameplay.

## User Experience

### Xenotype Editor Flow

1. The player opens the vanilla custom xenotype editor.
2. The player adds the configurable template germline gene.
3. When that gene is selected, the UI exposes a `Configure` action.
4. The action opens a custom window such as `Dialog_ConfigureColorGene`.
5. The player chooses one of:
   - `Palette`
   - `HSV Range`
6. The player edits the profile and confirms it.
7. The design is saved with the custom xenotype.

### v1 Editor Controls

- Mode selector: `Palette` or `HSV Range`
- Palette color list with add/remove
- Color entry using hex or RGB
- HSV min/max fields
- Preview swatches or sample colors
- Reset to template defaults

For v1, the gene should keep its fixed XML label, icon, and stats. The only configurable content is the color profile.

## Data Model

Add a saveable profile object:

- `DesignedGeneColorProfile : IExposable`

Suggested fields:

- `string templateGeneDefName`
- `string designId`
- `ColorDesignMode mode`
- `List<Color> paletteColors`
- `FloatRange hueRange`
- `FloatRange saturationRange`
- `FloatRange valueRange`

Add a saveable mod-owned xenotype database:

- `DesignedGeneProfileDatabase : IExposable`

Suggested storage shape:

- First key: custom xenotype identity
- Second key: template gene def name
- Value: `DesignedGeneColorProfile`

Recommended xenotype key:

- `CustomXenotype.fileName` when available
- Fallback to xenotype name if needed

## Pawn-Side Storage

Extend `Gene_SkinColorRange` so the gene instance can store the designed profile that was selected during xenotype creation.

Suggested additions:

- `DesignedGeneColorProfile? designedProfile`
- Save/load of `designedProfile` in `ExposeData()`
- A helper to resolve the effective source profile for sampling

This is required so that:

- Pawn behavior remains stable after generation
- Save/load does not rely on repeated database lookups
- Existing pawns remain valid even if the source xenotype is later edited

## Runtime Resolution Rules

Update `GeneColorInheritanceUtility` to resolve color sources in this order:

1. Use the gene instance's `designedProfile` if present
2. Otherwise use the XML `GeneColorRangeExtension`
3. Otherwise warn and fail safely

This preserves compatibility with the current XML-authored genes while enabling the configurable template gene.

## Vanilla Integration Points

Primary vanilla touchpoints:

- `RimWorld.Dialog_CreateXenotype`
- `RimWorld.GeneUIUtility`
- `RimWorld.Pawn_GeneTracker.CustomXenotype`

Required patch behaviors:

- Patch `Dialog_CreateXenotype` to expose a configuration action for the template gene
- Patch `Dialog_CreateXenotype.AcceptInner()` to save the current working profile into the mod-owned xenotype database
- Extend pawn generation logic so a pawn with the template gene copies the designed profile from its custom xenotype before resolving color
- Keep newborn handling driven by the active parent gene instances

## Recommended Implementation Shape

### New Files

- `Source/GeneColorInheritance/Data/DesignedGeneColorProfile.cs`
- `Source/GeneColorInheritance/Data/DesignedGeneProfileDatabase.cs`
- `Source/GeneColorInheritance/UI/Dialog_ConfigureColorGene.cs`
- `Source/GeneColorInheritance/UI/Widgets_ColorProfileEditor.cs`
- `Source/GeneColorInheritance/Patches/Patch_Dialog_CreateXenotype_DrawGene.cs`
- `Source/GeneColorInheritance/Patches/Patch_Dialog_CreateXenotype_AcceptInner.cs`

### Existing Files To Update

- `Source/GeneColorInheritance/Genes/Gene_SkinColorRange.cs`
- `Source/GeneColorInheritance/Genes/GeneColorInheritanceUtility.cs`
- `Source/GeneColorInheritance/Patches/Patch_PawnGenerator_GeneratePawn.cs`
- `Source/GeneColorInheritance/Patches/Patch_PregnancyUtility_ApplyBirthOutcome.cs`
- `Defs/GeneDefs/GeneColorInheritance_Genes.xml`

## Inheritance Model

For v1, keep the current behavior model:

- The profile defines what colors a pawn may initially sample from
- A pawn resolves one stable personal skin color
- Birth logic still blends the parents' visible skin colors

This keeps the feature aligned with the current mod and avoids introducing profile-level heredity in the first implementation.

## Compatibility Rule

Keep the current xenotype compatibility behavior:

- The configurable template gene remains cosmetic for:
  - `GeneUtility.SameHeritableXenotype`
  - `ChildRelationUtility.XenotypesCompatible`
  - `PregnancyUtility.ShouldByHybrid`

Different configurations of the same template gene should still count as the same compatibility-wise in v1.

## Persistence Strategy

Use two persistence layers:

- Xenotype design persistence
  - Save designed profiles in a mod-owned database keyed by custom xenotype
- Pawn runtime persistence
  - Copy the chosen profile onto the gene instance and save it with `Gene_SkinColorRange`

This avoids fragile runtime lookups and keeps gameplay behavior stable.

## Implementation Order

1. Add the configurable template gene to `Defs/GeneDefs/GeneColorInheritance_Genes.xml`.
2. Add `DesignedGeneColorProfile`.
3. Add `DesignedGeneProfileDatabase`.
4. Extend `Gene_SkinColorRange` to store an optional designed profile.
5. Update `GeneColorInheritanceUtility` to sample from `designedProfile` first.
6. Implement `Dialog_ConfigureColorGene`.
7. Implement reusable profile-editing widgets.
8. Patch `Dialog_CreateXenotype` to expose the configuration flow.
9. Patch `Dialog_CreateXenotype.AcceptInner()` to persist the designed profile.
10. Extend pawn generation to copy the designed profile from the pawn's custom xenotype.
11. Verify birth behavior with configured parent genes.
12. Test save/load, xenotype reload, and dev-mode gene changes.

## Acceptance Criteria

- A player can add the template germline gene in the custom xenotype editor.
- The gene exposes a working configuration UI.
- The player can save a palette or HSV range with the xenotype.
- A pawn generated from that xenotype resolves a stable skin color from the saved design.
- Save/load preserves the pawn's resolved color and designed profile.
- Babies born from configured parents continue to receive inherited colors through the current birth logic.
- Existing XML-authored genes continue to work unchanged.

## Risks

- `Dialog_CreateXenotype` patching may be brittle because it is UI internals rather than a formal extension point.
- Custom xenotype keying must be stable enough to survive save/load and xenotype edits.
- The first version should stay limited to one template gene to avoid overcomplicating storage and UI flow too early.
