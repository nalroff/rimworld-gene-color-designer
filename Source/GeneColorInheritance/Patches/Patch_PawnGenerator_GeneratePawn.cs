using GeneColorInheritance.Genes;
using HarmonyLib;
using Verse;

namespace GeneColorInheritance.Patches
{
    [HarmonyPatch(
        typeof(PawnGenerator),
        nameof(PawnGenerator.GeneratePawn),
        typeof(PawnGenerationRequest)
    )]
    public static class Patch_PawnGenerator_GeneratePawn
    {
        public static void Postfix(Pawn __result)
        {
            Gene_SkinColorRange? gene = GeneColorInheritanceUtility.GetActiveSkinGene(__result);
            if (gene != null)
            {
                GeneColorInheritanceUtility.ResolveAndApply(gene);
            }
        }
    }
}
