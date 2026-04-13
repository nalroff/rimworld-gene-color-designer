using GeneColorInheritance.Data;
using HarmonyLib;
using RimWorld;

namespace GeneColorInheritance.Patches
{
    [HarmonyPatch(typeof(Dialog_CreateXenotype), "AcceptInner")]
    public static class Patch_Dialog_CreateXenotype_AcceptInner
    {
        public static void Prefix(Dialog_CreateXenotype __instance)
        {
            DesignedGeneProfileStore.SaveDialogProfile(__instance);
        }
    }
}
