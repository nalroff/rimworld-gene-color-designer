using GeneColorInheritance.Genes;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GeneColorInheritance.Patches
{
    [HarmonyPatch(typeof(ChildRelationUtility), nameof(ChildRelationUtility.XenotypesCompatible))]
    public static class Patch_ChildRelationUtility_XenotypesCompatible
    {
        public static void Postfix(Pawn first, Pawn second, ref bool __result)
        {
            if (
                !__result
                && GeneColorInheritanceUtility.EquivalentIgnoringCosmeticGenes(first, second)
            )
            {
                __result = true;
            }
        }
    }
}
