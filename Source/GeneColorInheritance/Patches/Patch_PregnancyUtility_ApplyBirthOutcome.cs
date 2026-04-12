using GeneColorInheritance.Genes;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GeneColorInheritance.Patches
{
    [HarmonyPatch(typeof(PregnancyUtility), nameof(PregnancyUtility.ApplyBirthOutcome))]
    public static class Patch_PregnancyUtility_ApplyBirthOutcome
    {
        public static void Postfix(
            Thing __result,
            Pawn? geneticMother,
            Thing birtherThing,
            Pawn? father = null
        )
        {
            if (__result is not Pawn baby)
            {
                return;
            }

            Pawn? mother = geneticMother ?? birtherThing as Pawn;
            if (
                !GeneColorInheritanceUtility.TryGetBirthBlend(
                    mother,
                    father,
                    out UnityEngine.Color blendedColor
                )
            )
            {
                return;
            }

            Gene_SkinColorRange? gene = GeneColorInheritanceUtility.GetActiveSkinGene(baby);
            if (gene != null)
            {
                gene.SetResolvedColor(blendedColor);
                return;
            }

            GeneColorInheritanceUtility.ApplyDirectSkinOverride(baby, blendedColor);
        }
    }
}
