# Gene Color Designer

Gene Color Designer is a RimWorld 1.6 Biotech mod focused on configurable germline skin-color genes.

## Current Status

- Stable skin-color inheritance system is implemented
- Configurable template gene UI is planned in [UI_FEATURE.md](UI_FEATURE.md)

## Repository Layout

- `About/`
  - RimWorld mod metadata
- `Assemblies/net472/`
  - Built mod assembly loaded by the game
- `Defs/`
  - XML defs for genes and related content
- `Source/`
  - C# project and solution

## Local Development

1. Open `Source/GeneColorInheritance.sln` in Visual Studio or Rider.
2. Build the `Release` configuration for the `net472` project.
3. The output is written into `Assemblies/`, which RimWorld loads directly from this mod folder.

## Requirements

- RimWorld 1.6
- Biotech
- Harmony

## GitHub

Primary repository:

- <https://github.com/nalroff/rimworld-gene-color-designer>
