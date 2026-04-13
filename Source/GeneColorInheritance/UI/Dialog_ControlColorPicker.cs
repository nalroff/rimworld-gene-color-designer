using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace GeneColorInheritance.UI
{
    public class Dialog_ControlColorPicker : Window
    {
        private const float LabelWidth = 90f;

        private const float FieldX = 96f;

        private const float FieldWidth = 56f;

        private const float SliderX = 166f;

        private const float SliderHeight = 24f;

        private readonly string header;

        private readonly List<Color> pickableColors;

        private readonly Action<Color> onApply;

        private readonly Color originalColor;

        private Color workingColor;

        private readonly string[] hsvBuffers = new string[3];

        private Color lastSyncedColor;

        private bool hueDragging;

        private bool saturationDragging;

        private bool valueDragging;

        public override Vector2 InitialSize => new Vector2(700f, 560f);

        public Dialog_ControlColorPicker(
            string header,
            Color selectedColor,
            List<Color> pickableColors,
            Action<Color> onApply
        )
        {
            this.header = header;
            this.pickableColors = pickableColors;
            this.onApply = onApply;
            originalColor = selectedColor;
            workingColor = selectedColor;
            lastSyncedColor = selectedColor;
            optionalTitle = header;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            doCloseButton = false;
            SyncBuffersFromColor();
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (workingColor != lastSyncedColor)
            {
                SyncBuffersFromColor();
            }

            float curY = 0f;

            Widgets.Label(new Rect(0f, curY, inRect.width, Text.LineHeight), "Choose a color");
            curY += Text.LineHeight + 8f;

            DrawPresetRow(inRect.width, ref curY);
            DrawPreviewRow(inRect.width, ref curY);
            DrawHsvEditors(inRect.width, ref curY);

            Rect cancelRect = new Rect(0f, inRect.height - 38f, 150f, 38f);
            Rect acceptRect = new Rect(inRect.width - 150f, inRect.height - 38f, 150f, 38f);

            if (Widgets.ButtonText(cancelRect, "Cancel"))
            {
                Close(false);
            }

            if (Widgets.ButtonText(acceptRect, "Accept"))
            {
                onApply(workingColor);
                Close(true);
            }
        }

        private void DrawPresetRow(float width, ref float curY)
        {
            Widgets.Label(new Rect(0f, curY, width, Text.LineHeight), "Control colors");
            curY += Text.LineHeight + 4f;

            float x = 0f;
            for (int i = 0; i < pickableColors.Count; i++)
            {
                Rect swatchRect = new Rect(x, curY, 34f, 34f);
                Color outline = pickableColors[i] == workingColor ? Color.white : Color.black;
                Widgets.DrawBoxSolidWithOutline(swatchRect, pickableColors[i], outline);
                if (Widgets.ButtonInvisible(swatchRect))
                {
                    workingColor = pickableColors[i];
                    SyncBuffersFromColor();
                }

                x += swatchRect.width + 6f;
            }

            curY += 44f;
        }

        private void DrawPreviewRow(float width, ref float curY)
        {
            Rect currentLabelRect = new Rect(0f, curY, 120f, Text.LineHeight);
            Rect currentSwatchRect = new Rect(0f, currentLabelRect.yMax + 4f, 90f, 90f);
            Rect oldLabelRect = new Rect(currentSwatchRect.xMax + 24f, curY, 120f, Text.LineHeight);
            Rect oldSwatchRect = new Rect(oldLabelRect.x, oldLabelRect.yMax + 4f, 90f, 90f);

            Widgets.Label(currentLabelRect, "Current color");
            Widgets.DrawBoxSolidWithOutline(currentSwatchRect, workingColor, Color.white);

            Widgets.Label(oldLabelRect, "Original color");
            Widgets.DrawBoxSolidWithOutline(oldSwatchRect, originalColor, Color.white);

            Rect infoRect = new Rect(
                oldSwatchRect.xMax + 24f,
                curY,
                width - oldSwatchRect.xMax - 24f,
                Text.LineHeight * 3f
            );
            Widgets.Label(
                infoRect,
                $"HEX: #{ColorUtility.ToHtmlStringRGB(workingColor)}\n"
                    + $"RGB: {Mathf.RoundToInt(workingColor.r * 255f)}, {Mathf.RoundToInt(workingColor.g * 255f)}, {Mathf.RoundToInt(workingColor.b * 255f)}\n"
                    + "Edit with HSV controls below."
            );

            curY += 110f;
        }

        private void DrawHsvEditors(float width, ref float curY)
        {
            Widgets.Label(new Rect(0f, curY, width, Text.LineHeight), "HSV");
            curY += Text.LineHeight + 4f;

            DrawHsvSlider(width, ref curY, "Hue", 0);
            DrawHsvSlider(width, ref curY, "Saturation", 1);
            DrawHsvSlider(width, ref curY, "Value", 2);
        }

        private void DrawHsvSlider(float width, ref float curY, string label, int index)
        {
            Color.RGBToHSV(workingColor, out float hue, out float saturation, out float value);
            int currentValue = GetCurrentComponentValue(index, hue, saturation, value);
            int maxValue = GetComponentMaxValue(index);
            float normalizedValue = Mathf.Clamp01(currentValue / (float)maxValue);

            Widgets.Label(new Rect(0f, curY, LabelWidth, 28f), label);
            Rect fieldRect = new Rect(FieldX, curY, FieldWidth, 28f);
            Rect sliderRect = new Rect(SliderX, curY + 4f, width - SliderX, SliderHeight);

            string buffer = Widgets.TextField(fieldRect, hsvBuffers[index]);
            hsvBuffers[index] = buffer;
            if (int.TryParse(buffer, out int parsed))
            {
                int clamped = Mathf.Clamp(parsed, 0, maxValue);
                if (clamped != currentValue)
                {
                    SetHsvComponent(index, clamped);
                }
            }

            DrawTrack(sliderRect, hue, saturation, value, index);
            HandleSliderInput(sliderRect, index, maxValue);
            DrawSliderMarker(sliderRect, normalizedValue);

            curY += 34f;
        }

        private void HandleSliderInput(Rect sliderRect, int index, int maxValue)
        {
            Event currentEvent = Event.current;
            if (currentEvent == null)
            {
                return;
            }

            ref bool dragging = ref GetDraggingState(index);
            if (
                currentEvent.type == EventType.MouseDown
                && currentEvent.button == 0
                && sliderRect.Contains(currentEvent.mousePosition)
            )
            {
                dragging = true;
                UpdateComponentFromMouse(sliderRect, index, maxValue, currentEvent.mousePosition.x);
                currentEvent.Use();
                return;
            }

            if (dragging && currentEvent.type == EventType.MouseDrag)
            {
                UpdateComponentFromMouse(sliderRect, index, maxValue, currentEvent.mousePosition.x);
                currentEvent.Use();
                return;
            }

            if (dragging && currentEvent.type == EventType.MouseUp)
            {
                UpdateComponentFromMouse(sliderRect, index, maxValue, currentEvent.mousePosition.x);
                dragging = false;
                currentEvent.Use();
            }
        }

        private void UpdateComponentFromMouse(Rect sliderRect, int index, int maxValue, float mouseX)
        {
            float normalized = Mathf.InverseLerp(sliderRect.x, sliderRect.xMax, mouseX);
            SetHsvComponent(index, Mathf.RoundToInt(Mathf.Clamp01(normalized) * maxValue));
        }

        private ref bool GetDraggingState(int index)
        {
            if (index == 0)
            {
                return ref hueDragging;
            }

            if (index == 1)
            {
                return ref saturationDragging;
            }

            return ref valueDragging;
        }

        private void DrawTrack(
            Rect sliderRect,
            float hue,
            float saturation,
            float value,
            int index
        )
        {
            int segments = Mathf.Max(2, Mathf.RoundToInt(sliderRect.width));
            float segmentWidth = sliderRect.width / segments;

            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)(segments - 1);
                Color color = index switch
                {
                    0 => Color.HSVToRGB(t, 1f, 1f),
                    1 => Color.HSVToRGB(hue, t, value),
                    _ => Color.HSVToRGB(hue, saturation, t),
                };
                Rect segmentRect = new Rect(
                    sliderRect.x + i * segmentWidth,
                    sliderRect.y,
                    segmentWidth + 1f,
                    sliderRect.height
                );
                Widgets.DrawBoxSolid(segmentRect, color);
            }
        }

        private void DrawSliderMarker(Rect sliderRect, float normalizedValue)
        {
            float markerX = Mathf.Lerp(sliderRect.x, sliderRect.xMax, normalizedValue);
            Rect markerRect = new Rect(markerX - 2f, sliderRect.y - 2f, 4f, sliderRect.height + 4f);
            Widgets.DrawBoxSolid(markerRect, Color.white);
        }

        private static int GetCurrentComponentValue(
            int index,
            float hue,
            float saturation,
            float value
        )
        {
            return index switch
            {
                0 => Mathf.RoundToInt(hue * 360f),
                1 => Mathf.RoundToInt(saturation * 100f),
                _ => Mathf.RoundToInt(value * 100f),
            };
        }

        private static int GetComponentMaxValue(int index)
        {
            return index == 0 ? 360 : 100;
        }

        private void SetHsvComponent(int index, int value)
        {
            Color.RGBToHSV(workingColor, out float hue, out float saturation, out float brightness);

            switch (index)
            {
                case 0:
                    hue = value / 360f;
                    break;
                case 1:
                    saturation = value / 100f;
                    break;
                default:
                    brightness = value / 100f;
                    break;
            }

            workingColor = Color.HSVToRGB(
                Mathf.Repeat(hue, 1f),
                Mathf.Clamp01(saturation),
                Mathf.Clamp01(brightness)
            );
            SyncBuffersFromColor();
        }

        private void SyncBuffersFromColor()
        {
            lastSyncedColor = workingColor;

            Color.RGBToHSV(workingColor, out float hue, out float saturation, out float value);
            hsvBuffers[0] = Mathf.RoundToInt(hue * 360f).ToString();
            hsvBuffers[1] = Mathf.RoundToInt(saturation * 100f).ToString();
            hsvBuffers[2] = Mathf.RoundToInt(value * 100f).ToString();
        }
    }
}
