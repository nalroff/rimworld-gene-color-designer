using System;
using GeneColorInheritance.Data;
using RimWorld;
using UnityEngine;
using Verse;

namespace GeneColorInheritance.UI
{
    public class Dialog_ConfigureColorGene : Window
    {
        private readonly GeneDef templateGene;

        private readonly Action<DesignedGeneColorProfile> onAccept;

        private readonly DesignedGeneColorProfile templateDefaults;

        private readonly Widgets_ColorProfileEditor.State editorState = new();

        private readonly DesignedGeneColorProfile workingProfile;

        public override Vector2 InitialSize => new Vector2(760f, 700f);

        public Dialog_ConfigureColorGene(
            GeneDef templateGene,
            DesignedGeneColorProfile initialProfile,
            Action<DesignedGeneColorProfile> onAccept
        )
        {
            this.templateGene = templateGene;
            this.onAccept = onAccept;
            workingProfile = initialProfile.Clone();
            templateDefaults = DesignedGeneColorProfile.FromExtension(templateGene);
            closeOnClickedOutside = false;
            doCloseButton = false;
            absorbInputAroundWindow = true;
            forcePause = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 34f), "Configure Color Gene");
            Text.Font = GameFont.Small;

            Rect labelRect = new Rect(inRect.x, inRect.y + 36f, inRect.width, Text.LineHeight);
            Widgets.Label(labelRect, templateGene.LabelCap);

            Rect helpRect = new Rect(
                inRect.x,
                labelRect.yMax + 8f,
                inRect.width,
                Text.LineHeight * 2f
            );
            Widgets.Label(
                helpRect,
                "Pick 2 to 4 control colors. Pawns resolve a stable skin color from the blended space between those colors."
            );

            Rect contentRect = new Rect(
                inRect.x,
                helpRect.yMax + 12f,
                inRect.width,
                inRect.yMax - helpRect.yMax - 64f
            );
            Widgets_ColorProfileEditor.Draw(contentRect, workingProfile, editorState, templateDefaults);

            Rect cancelRect = new Rect(inRect.x, inRect.yMax - 38f, 150f, 38f);
            Rect acceptRect = new Rect(inRect.xMax - 150f, inRect.yMax - 38f, 150f, 38f);

            if (Widgets.ButtonText(cancelRect, "Cancel"))
            {
                Close(false);
            }

            if (Widgets.ButtonText(acceptRect, "Use Settings"))
            {
                onAccept(workingProfile.Clone());
                Close(true);
            }
        }
    }
}
