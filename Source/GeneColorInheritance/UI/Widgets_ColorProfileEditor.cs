using System.Collections.Generic;
using GeneColorInheritance.Data;
using UnityEngine;
using Verse;

namespace GeneColorInheritance.UI
{
    public static class Widgets_ColorProfileEditor
    {
        public class State
        {
            public Vector2 scrollPosition = Vector2.zero;

            public int selectedIndex;
        }

        public static void Draw(
            Rect inRect,
            DesignedGeneColorProfile profile,
            State state,
            DesignedGeneColorProfile templateDefaults
        )
        {
            state.selectedIndex = Mathf.Clamp(state.selectedIndex, 0, profile.paletteColors.Count - 1);

            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, ViewHeight());
            Widgets.BeginScrollView(inRect, ref state.scrollPosition, viewRect);

            float curY = 0f;
            DrawPreviewRow(viewRect.width, ref curY, profile);
            DrawControlPointEditor(viewRect.width, ref curY, profile, state);

            Rect resetRect = new Rect(0f, curY, 180f, 30f);
            if (Widgets.ButtonText(resetRect, "Reset To Template"))
            {
                profile.ResetFrom(templateDefaults);
                state.selectedIndex = Mathf.Clamp(state.selectedIndex, 0, profile.paletteColors.Count - 1);
            }

            Widgets.EndScrollView();
        }

        private static void DrawPreviewRow(float width, ref float curY, DesignedGeneColorProfile profile)
        {
            Widgets.Label(new Rect(0f, curY, width, Text.LineHeight), "Preview Range");
            curY += Text.LineHeight + 4f;

            float x = 0f;
            foreach (Color color in DesignedGeneProfileStore.PreviewColors(profile, 6))
            {
                Rect swatchRect = new Rect(x, curY, 44f, 44f);
                Widgets.DrawBoxSolidWithOutline(swatchRect, color, Color.black);
                x += swatchRect.width + 6f;
            }

            curY += 54f;
        }

        private static void DrawControlPointEditor(
            float width,
            ref float curY,
            DesignedGeneColorProfile profile,
            State state
        )
        {
            Widgets.Label(new Rect(0f, curY, width, Text.LineHeight), "Control Colors");
            curY += Text.LineHeight + 4f;

            Widgets.Label(
                new Rect(0f, curY, width, Text.LineHeight * 2f),
                "Use 2 to 4 control colors. Click any color box to edit it with RimWorld's color picker."
            );
            curY += Text.LineHeight * 2f + 8f;

            float x = 0f;
            for (int i = 0; i < profile.paletteColors.Count; i++)
            {
                Rect swatchRect = new Rect(x, curY, 52f, 52f);
                Color outline = i == state.selectedIndex ? Color.white : Color.black;
                Widgets.DrawBoxSolidWithOutline(swatchRect, profile.paletteColors[i].color, outline);

                if (Widgets.ButtonInvisible(swatchRect))
                {
                    state.selectedIndex = i;
                    OpenColorPicker(profile, state);
                }

                Rect numberRect = new Rect(swatchRect.x, swatchRect.yMax + 2f, swatchRect.width, Text.LineHeight);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(numberRect, (i + 1).ToString());
                Text.Anchor = TextAnchor.UpperLeft;
                x += swatchRect.width + 10f;
            }

            curY += 76f;

            Rect editRect = new Rect(0f, curY, 140f, 28f);
            if (Widgets.ButtonText(editRect, "Edit Selected"))
            {
                OpenColorPicker(profile, state);
            }

            Rect addRect = new Rect(editRect.xMax + 8f, curY, 120f, 28f);
            if (Widgets.ButtonText(addRect, "Add Color") && profile.paletteColors.Count < 4)
            {
                Color newColor = profile.paletteColors[state.selectedIndex].color;
                int insertIndex = Mathf.Clamp(state.selectedIndex + 1, 0, profile.paletteColors.Count);
                profile.paletteColors.Insert(insertIndex, new DesignedColorEntry(newColor));
                state.selectedIndex = insertIndex;
            }

            Rect removeRect = new Rect(addRect.xMax + 8f, curY, 120f, 28f);
            if (Widgets.ButtonText(removeRect, "Remove Color") && profile.paletteColors.Count > 2)
            {
                profile.paletteColors.RemoveAt(state.selectedIndex);
                state.selectedIndex = Mathf.Clamp(state.selectedIndex, 0, profile.paletteColors.Count - 1);
            }

            curY += 38f;

            Color currentColor = profile.paletteColors[state.selectedIndex].color;
            Rect currentRect = new Rect(0f, curY, 80f, 80f);
            Widgets.DrawBoxSolidWithOutline(currentRect, currentColor, Color.white);

            Rect infoRect = new Rect(currentRect.xMax + 14f, curY, width - currentRect.width - 14f, Text.LineHeight * 4f);
            Widgets.Label(
                infoRect,
                $"Selected color: {state.selectedIndex + 1} of {profile.paletteColors.Count}\n"
                + $"HEX: #{ColorUtility.ToHtmlStringRGB(currentColor)}\n"
                + $"RGB: {Mathf.RoundToInt(currentColor.r * 255f)}, {Mathf.RoundToInt(currentColor.g * 255f)}, {Mathf.RoundToInt(currentColor.b * 255f)}\n"
                + "The preview swatches above show the blended range between all control colors."
            );

            curY += 94f;
        }

        private static void OpenColorPicker(DesignedGeneColorProfile profile, State state)
        {
            int index = Mathf.Clamp(state.selectedIndex, 0, profile.paletteColors.Count - 1);
            Color selectedColor = profile.paletteColors[index].color;

            List<Color> choices = new List<Color>(profile.paletteColors.Count);
            for (int i = 0; i < profile.paletteColors.Count; i++)
            {
                choices.Add(profile.paletteColors[i].color);
            }

            if (choices.Count == 0)
            {
                choices.Add(selectedColor);
            }

            Find.WindowStack.Add(
                new Dialog_ControlColorPicker(
                    $"Control Color {index + 1}",
                    selectedColor,
                    choices,
                    color => profile.paletteColors[index].color = color
                )
            );
        }

        private static float ViewHeight()
        {
            return 320f;
        }
    }
}
