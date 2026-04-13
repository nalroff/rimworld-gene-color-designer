using System;
using System.Collections.Generic;
using System.Linq;
using GeneColorInheritance.Genes;
using UnityEngine;
using Verse;

namespace GeneColorInheritance.Data
{
    public class DesignedColorEntry : IExposable
    {
        public Color color = Color.white;

        public DesignedColorEntry() { }

        public DesignedColorEntry(Color color)
        {
            this.color = color;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref color, "color", Color.white, false);
        }
    }

    public class DesignedGeneColorProfile : IExposable
    {
        public string templateGeneDefName = string.Empty;

        public string designId = Guid.NewGuid().ToString("N");

        public List<DesignedColorEntry> paletteColors = new();

        // Legacy fields are kept so older sidecars still deserialize cleanly.
        public FloatRange hueRange = new FloatRange(0f, 1f);

        public FloatRange saturationRange = FloatRange.ZeroToOne;

        public FloatRange valueRange = FloatRange.ZeroToOne;

        public bool HasPaletteColors => paletteColors.Count > 0;

        public bool HasCompleteHsvRange =>
            hueRange.TrueMin >= 0f && saturationRange.TrueMin >= 0f && valueRange.TrueMin >= 0f;

        public void ExposeData()
        {
            Scribe_Values.Look(ref templateGeneDefName, "templateGeneDefName", string.Empty, false);
            Scribe_Values.Look(ref designId, "designId", string.Empty, false);
            Scribe_Collections.Look(ref paletteColors, "paletteColors", LookMode.Deep);
            Scribe_Values.Look(ref hueRange, "hueRange", new FloatRange(0f, 1f), false);
            Scribe_Values.Look(ref saturationRange, "saturationRange", FloatRange.ZeroToOne, false);
            Scribe_Values.Look(ref valueRange, "valueRange", FloatRange.ZeroToOne, false);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                paletteColors ??= new List<DesignedColorEntry>();
                if (paletteColors.Count == 0 && HasCompleteHsvRange)
                {
                    AddPaletteFromRanges(this);
                }

                while (paletteColors.Count < 2)
                {
                    paletteColors.Add(new DesignedColorEntry(Color.white));
                }

                if (paletteColors.Count > 4)
                {
                    paletteColors.RemoveRange(4, paletteColors.Count - 4);
                }

                if (string.IsNullOrEmpty(designId))
                {
                    designId = Guid.NewGuid().ToString("N");
                }
            }
        }

        public DesignedGeneColorProfile Clone()
        {
            return new DesignedGeneColorProfile
            {
                templateGeneDefName = templateGeneDefName,
                designId = designId,
                hueRange = hueRange,
                saturationRange = saturationRange,
                valueRange = valueRange,
                paletteColors = paletteColors.Select(entry => new DesignedColorEntry(entry.color)).ToList(),
            };
        }

        public List<Color> PaletteColorValues()
        {
            return paletteColors.Select(entry => entry.color).ToList();
        }

        public static DesignedGeneColorProfile FromExtension(GeneDef? geneDef)
        {
            GeneColorRangeExtension? extension = GeneColorInheritanceUtility.GetExtension(geneDef);
            DesignedGeneColorProfile profile = new DesignedGeneColorProfile
            {
                templateGeneDefName = geneDef?.defName ?? string.Empty,
            };

            if (extension != null)
            {
                profile.hueRange = extension.hueRange;
                profile.saturationRange = extension.saturationRange;
                profile.valueRange = extension.valueRange;

                for (int i = 0; i < extension.paletteColors.Count; i++)
                {
                    profile.paletteColors.Add(new DesignedColorEntry(extension.paletteColors[i]));
                }

                if (!profile.HasPaletteColors && extension.HasCompleteHsvRange)
                {
                    AddPaletteFromRanges(profile);
                }
            }

            if (!profile.HasPaletteColors)
            {
                profile.paletteColors.Add(new DesignedColorEntry(new Color(1f, 0.9f, 0.8f)));
                profile.paletteColors.Add(new DesignedColorEntry(new Color(0.7f, 0.55f, 0.45f)));
            }

            while (profile.paletteColors.Count < 2)
            {
                profile.paletteColors.Add(new DesignedColorEntry(profile.paletteColors[0].color));
            }

            if (profile.paletteColors.Count > 4)
            {
                profile.paletteColors.RemoveRange(4, profile.paletteColors.Count - 4);
            }

            return profile;
        }

        private static void AddPaletteFromRanges(DesignedGeneColorProfile profile)
        {
            profile.paletteColors.Clear();
            for (int i = 0; i < 4; i++)
            {
                float t = i / 3f;
                float hue = Mathf.Repeat(
                    Mathf.Lerp(profile.hueRange.TrueMin, profile.hueRange.TrueMax, t),
                    1f
                );
                float saturation = Mathf.Lerp(
                    profile.saturationRange.TrueMin,
                    profile.saturationRange.TrueMax,
                    t
                );
                float value = Mathf.Lerp(profile.valueRange.TrueMin, profile.valueRange.TrueMax, t);
                profile.paletteColors.Add(
                    new DesignedColorEntry(
                        Color.HSVToRGB(hue, Mathf.Clamp01(saturation), Mathf.Clamp01(value))
                    )
                );
            }
        }
    }
}
