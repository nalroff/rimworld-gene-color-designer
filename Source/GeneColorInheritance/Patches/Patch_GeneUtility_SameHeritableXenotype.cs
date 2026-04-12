using GeneColorInheritance.Genes;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GeneColorInheritance.Patches
{
    [HarmonyPatch(typeof(GeneUtility), nameof(GeneUtility.SameHeritableXenotype))]
    public static class Patch_GeneUtility_SameHeritableXenotype
    {
        public static void Postfix(Pawn pawn1, Pawn pawn2, ref bool __result)
        {
            if (
                !__result
                && GeneColorInheritanceUtility.EquivalentIgnoringCosmeticGenes(pawn1, pawn2)
            )
            {
                __result = true;
            }
        }
    }
}
