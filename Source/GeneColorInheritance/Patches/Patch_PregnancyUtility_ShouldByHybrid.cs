using GeneColorInheritance.Genes;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GeneColorInheritance.Patches
{
    [HarmonyPatch(typeof(PregnancyUtility), "ShouldByHybrid")]
    public static class Patch_PregnancyUtility_ShouldByHybrid
    {
        public static void Postfix(Pawn mother, Pawn father, ref bool __result)
        {
            if (
                __result
                && GeneColorInheritanceUtility.EquivalentIgnoringCosmeticGenes(mother, father)
            )
            {
                __result = false;
            }
        }
    }
}
