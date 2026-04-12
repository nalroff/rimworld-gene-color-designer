using UnityEngine;
using Verse;

namespace GeneColorInheritance.Genes
{
    public class GeneColorRangeExtension : DefModExtension
    {
        public List<Color> paletteColors = new();

        public FloatRange hueRange = new FloatRange(-1f, -1f);

        public FloatRange saturationRange = new FloatRange(-1f, -1f);

        public FloatRange valueRange = new FloatRange(-1f, -1f);

        public bool HasPaletteColors => paletteColors != null && paletteColors.Count > 0;

        public bool HasCompleteHsvRange =>
            hueRange.TrueMin >= 0f && saturationRange.TrueMin >= 0f && valueRange.TrueMin >= 0f;
    }
}
