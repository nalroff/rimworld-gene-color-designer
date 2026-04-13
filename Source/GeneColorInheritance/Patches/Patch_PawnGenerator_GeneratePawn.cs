using GeneColorInheritance.Data;
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
        public static void Postfix(Pawn __result, PawnGenerationRequest request)
        {
            Gene_SkinColorRange? gene = GeneColorInheritanceUtility.GetActiveSkinGene(__result);
            if (gene != null)
            {
                bool profileApplied = false;
                if (request.ForcedCustomXenotype != null)
                {
                    profileApplied = DesignedGeneProfileStore.TryApplyProfileFromCustomXenotype(
                        gene,
                        request.ForcedCustomXenotype
                    );
                }

                if (!profileApplied)
                {
                    DesignedGeneProfileStore.TryApplyFallbackProfileFromPawn(gene);
                }

                GeneColorInheritanceUtility.ResolveAndApply(gene);
            }
        }
    }
}
