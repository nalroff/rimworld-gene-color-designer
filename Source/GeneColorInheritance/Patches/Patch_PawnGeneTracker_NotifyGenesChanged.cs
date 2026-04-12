using GeneColorInheritance.Genes;
using HarmonyLib;
using RimWorld;

namespace GeneColorInheritance.Patches
{
    [HarmonyPatch(typeof(Pawn_GeneTracker), "Notify_GenesChanged")]
    public static class Patch_PawnGeneTracker_NotifyGenesChanged
    {
        public static void Postfix(Pawn_GeneTracker __instance)
        {
            Gene_SkinColorRange? gene = GeneColorInheritanceUtility.GetActiveSkinGene(
                __instance.pawn
            );
            if (gene != null)
            {
                gene.ApplyResolvedColor();
            }
        }
    }
}
