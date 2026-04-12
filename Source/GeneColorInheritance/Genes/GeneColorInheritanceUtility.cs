using RimWorld;
using UnityEngine;
using Verse;

namespace GeneColorInheritance.Genes
{
    public static class GeneColorInheritanceUtility
    {
        public static bool IsSupportedPawn(Pawn? pawn)
        {
            return pawn != null
                && pawn.RaceProps != null
                && pawn.RaceProps.Humanlike
                && pawn.genes != null
                && pawn.story != null;
        }

        public static GeneColorRangeExtension? GetExtension(GeneDef? def)
        {
            return def?.GetModExtension<GeneColorRangeExtension>();
        }

        public static Gene_SkinColorRange? GetActiveSkinGene(Pawn? pawn)
        {
            if (!IsSupportedPawn(pawn))
            {
                return null;
            }

            List<Gene> genes = pawn!.genes!.GenesListForReading;
            for (int i = 0; i < genes.Count; i++)
            {
                if (
                    genes[i] is Gene_SkinColorRange skinGene
                    && skinGene.Active
                    && GetExtension(skinGene.def) != null
                )
                {
                    return skinGene;
                }
            }

            return null;
        }

        public static void ResolveAndApply(Gene_SkinColorRange gene, bool forceResample = false)
        {
            if (!IsSupportedPawn(gene.pawn) || !gene.Active)
            {
                return;
            }

            if (forceResample || gene.ResolvedColor == null)
            {
                GeneColorRangeExtension? extension = GetExtension(gene.def);
                if (extension == null)
                {
                    Log.Warning(
                        $"[Gene Color Designer] Missing GeneColorRangeExtension on {gene.def.defName}."
                    );
                    return;
                }

                gene.SetResolvedColor(SampleColor(extension));
                return;
            }

            gene.ApplyResolvedColor();
        }

        public static void AssignFromParents(
            Gene_SkinColorRange gene,
            Pawn? firstParent,
            Pawn? secondParent
        )
        {
            if (!IsSupportedPawn(gene.pawn) || !gene.Active)
            {
                return;
            }

            if (
                TryGetVisibleSkinColor(firstParent, out Color firstColor)
                && TryGetVisibleSkinColor(secondParent, out Color secondColor)
            )
            {
                gene.SetResolvedColor(InterpolateColorsHsv(firstColor, secondColor, Rand.Value));
                return;
            }

            ResolveAndApply(gene, forceResample: gene.ResolvedColor == null);
        }

        public static bool TryGetBirthBlend(Pawn? firstParent, Pawn? secondParent, out Color color)
        {
            color = default;
            if (
                TryGetVisibleSkinColor(firstParent, out Color firstColor)
                && TryGetVisibleSkinColor(secondParent, out Color secondColor)
            )
            {
                color = InterpolateColorsHsv(firstColor, secondColor, Rand.Value);
                return true;
            }

            return false;
        }

        public static Color SampleColor(GeneColorRangeExtension extension)
        {
            if (extension.HasPaletteColors)
            {
                return SampleFromPalette(extension.paletteColors);
            }

            if (extension.HasCompleteHsvRange)
            {
                float hue = SampleRangeWrapped01(extension.hueRange);
                float saturation = Mathf.Clamp01(extension.saturationRange.RandomInRange);
                float value = Mathf.Clamp01(extension.valueRange.RandomInRange);
                return Color.HSVToRGB(hue, saturation, value);
            }

            Log.Warning(
                "[Gene Color Designer] Gene color extension has neither palette colors nor a complete HSV range. Falling back to white."
            );
            return Color.white;
        }

        public static Color InterpolateColorsHsv(Color first, Color second, float t)
        {
            Color.RGBToHSV(first, out float hueA, out float satA, out float valA);
            Color.RGBToHSV(second, out float hueB, out float satB, out float valB);

            float hue = Mathf.Repeat(hueA + ShortestHueDelta(hueA, hueB) * Mathf.Clamp01(t), 1f);
            float saturation = Mathf.Lerp(satA, satB, Mathf.Clamp01(t));
            float value = Mathf.Lerp(valA, valB, Mathf.Clamp01(t));

            return Color.HSVToRGB(hue, saturation, value);
        }

        public static void MarkPawnGraphicsDirty(Pawn? pawn)
        {
            if (pawn?.Drawer?.renderer != null)
            {
                pawn.Drawer.renderer.SetAllGraphicsDirty();
            }
        }

        public static void ApplyDirectSkinOverride(Pawn? pawn, Color color)
        {
            if (!IsSupportedPawn(pawn))
            {
                return;
            }

            pawn!.story!.skinColorOverride = new Color?(color);
            MarkPawnGraphicsDirty(pawn);
        }

        public static bool HasColorInheritanceExtension(GeneDef? def)
        {
            return GetExtension(def) != null;
        }

        public static bool EquivalentIgnoringCosmeticGenes(Pawn? first, Pawn? second)
        {
            if (first?.genes == null || second?.genes == null)
            {
                return false;
            }

            HashSet<GeneDef> firstDefs = first
                .genes.Endogenes.Select(gene => gene.def)
                .Where(ShouldCountForHeritableCompatibility)
                .ToHashSet();

            HashSet<GeneDef> secondDefs = second
                .genes.Endogenes.Select(gene => gene.def)
                .Where(ShouldCountForHeritableCompatibility)
                .ToHashSet();

            return firstDefs.SetEquals(secondDefs);
        }

        private static bool TryGetVisibleSkinColor(Pawn? pawn, out Color color)
        {
            color = default;
            if (!IsSupportedPawn(pawn))
            {
                return false;
            }

            color = pawn!.story!.SkinColor;
            return true;
        }

        private static Color SampleFromPalette(List<Color> paletteColors)
        {
            if (paletteColors.Count == 1)
            {
                return paletteColors[0];
            }

            float sample = Rand.Range(0f, paletteColors.Count - 1f);
            int lowerIndex = Mathf.Clamp(Mathf.FloorToInt(sample), 0, paletteColors.Count - 2);
            float t = sample - lowerIndex;
            return InterpolateColorsHsv(
                paletteColors[lowerIndex],
                paletteColors[lowerIndex + 1],
                t
            );
        }

        private static float SampleRangeWrapped01(FloatRange range)
        {
            float min = range.TrueMin;
            float max = range.TrueMax;
            if (min < 0f || max < 0f)
            {
                return 0f;
            }

            return Mathf.Repeat(Rand.Range(min, max), 1f);
        }

        private static float ShortestHueDelta(float fromHue, float toHue)
        {
            float delta = toHue - fromHue;
            if (delta > 0.5f)
            {
                delta -= 1f;
            }
            else if (delta < -0.5f)
            {
                delta += 1f;
            }

            return delta;
        }

        private static bool ShouldCountForHeritableCompatibility(GeneDef def)
        {
            return def != GeneDefOf.Inbred
                && def.endogeneCategory != EndogeneCategory.Melanin
                && def.endogeneCategory != EndogeneCategory.HairColor
                && !HasColorInheritanceExtension(def);
        }
    }
}
