using GeneColorInheritance.Data;
using GeneColorInheritance.UI;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GeneColorInheritance.Patches
{
    [HarmonyPatch(typeof(Dialog_CreateXenotype), "DrawGene")]
    public static class Patch_Dialog_CreateXenotype_DrawGene
    {
        private static readonly System.Reflection.FieldInfo GeneSizeField = AccessTools.Field(
            typeof(GeneCreationDialogBase),
            "GeneSize"
        );

        public static bool Prefix(
            Dialog_CreateXenotype __instance,
            GeneDef geneDef,
            bool selectedSection,
            float curX,
            float curY,
            Rect containingRect,
            ref bool __result,
            out float __state
        )
        {
            __state = curX;
            if (
                !selectedSection
                || !DesignedGeneProfileStore.IsConfigurableTemplateGene(geneDef)
            )
            {
                return true;
            }

            if (TryInterceptIconClick(__instance, curX, curY, containingRect))
            {
                __result = false;
                return false;
            }

            return true;
        }

        public static void Postfix(
            Dialog_CreateXenotype __instance,
            GeneDef geneDef,
            bool selectedSection,
            float curY,
            Rect containingRect,
            float __state
        )
        {
            if (
                !selectedSection
                || !DesignedGeneProfileStore.IsConfigurableTemplateGene(geneDef)
            )
            {
                return;
            }

            Vector2 geneSize = GetGeneSize();
            Rect geneRect = new Rect(__state, curY, geneSize.x, geneSize.y);
            if (!geneRect.Overlaps(containingRect))
            {
                return;
            }

            Rect iconRect = new Rect(geneRect.xMax - 22f, geneRect.y + 4f, 18f, 18f);
            if (Mouse.IsOver(iconRect))
            {
                Widgets.DrawHighlight(iconRect);
            }

            if (Widgets.ButtonImage(iconRect, TexButton.Rename))
            {
                OpenDesigner(__instance);
            }

            TooltipHandler.TipRegion(
                iconRect,
                "Edit skin color design\n" + DesignedGeneProfileStore.DialogProfileSummary(__instance)
            );
        }

        private static void OpenDesigner(Dialog_CreateXenotype dialog)
        {
            GeneDef? templateGene = DesignedGeneProfileStore.ConfigurableTemplateGene();
            if (templateGene == null)
            {
                return;
            }

            Find.WindowStack.Add(
                new Dialog_ConfigureColorGene(
                    templateGene,
                    DesignedGeneProfileStore.GetOrCreateDialogProfile(dialog),
                    profile => DesignedGeneProfileStore.SetDialogProfile(dialog, profile)
                )
            );
        }

        private static bool TryInterceptIconClick(
            Dialog_CreateXenotype dialog,
            float curX,
            float curY,
            Rect containingRect
        )
        {
            Event currentEvent = Event.current;
            if (currentEvent == null)
            {
                return false;
            }

            if (
                currentEvent.type != EventType.MouseDown
                && currentEvent.type != EventType.MouseUp
            )
            {
                return false;
            }

            if (currentEvent.button != 0)
            {
                return false;
            }

            Vector2 geneSize = GetGeneSize();
            Rect geneRect = new Rect(curX, curY, geneSize.x, geneSize.y);
            if (!geneRect.Overlaps(containingRect))
            {
                return false;
            }

            Rect iconRect = new Rect(geneRect.xMax - 22f, geneRect.y + 4f, 18f, 18f);
            if (!iconRect.Contains(currentEvent.mousePosition))
            {
                return false;
            }

            if (currentEvent.type == EventType.MouseUp)
            {
                OpenDesigner(dialog);
            }

            currentEvent.Use();
            return true;
        }

        private static Vector2 GetGeneSize()
        {
            try
            {
                return (Vector2)GeneSizeField.GetValue(null);
            }
            catch
            {
                return new Vector2(90f, 116f);
            }
        }
    }
}
