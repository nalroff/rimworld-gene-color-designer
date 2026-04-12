using HarmonyLib;
using Verse;

namespace GeneColorInheritance.Patches
{
    [StaticConstructorOnStartup]
    public static class GeneColorInheritanceBootstrap
    {
        static GeneColorInheritanceBootstrap()
        {
            var harmony = new Harmony("nalroff.GeneColorDesigner");
            harmony.PatchAll();
            Log.Message("[Gene Color Designer] Harmony bootstrap initialized.");
        }
    }
}
