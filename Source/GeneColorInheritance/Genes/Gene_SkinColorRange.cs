using GeneColorInheritance.Data;
using UnityEngine;
using Verse;

namespace GeneColorInheritance.Genes
{
    public class Gene_SkinColorRange : Gene
    {
        private Color? resolvedColor;

        private DesignedGeneColorProfile? designedProfile;

        public Color? ResolvedColor => resolvedColor;

        public DesignedGeneColorProfile? DesignedProfile => designedProfile;

        public override void PostAdd()
        {
            base.PostAdd();
            GeneColorInheritanceUtility.ResolveAndApply(this);
        }

        public override void PostRemove()
        {
            base.PostRemove();
            Gene_SkinColorRange? activeGene = GeneColorInheritanceUtility.GetActiveSkinGene(pawn);
            if (activeGene != null)
            {
                activeGene.ApplyResolvedColor();
                return;
            }

            GeneColorInheritanceUtility.MarkPawnGraphicsDirty(pawn);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref resolvedColor, "resolvedColor", null, false);
            Scribe_Deep.Look(ref designedProfile, "designedProfile");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                GeneColorInheritanceUtility.ResolveAndApply(
                    this,
                    forceResample: resolvedColor == null
                );
            }
        }

        public void SetResolvedColor(Color color)
        {
            resolvedColor = color;
            ApplyResolvedColor();
        }

        public void SetDesignedProfile(DesignedGeneColorProfile profile)
        {
            designedProfile = profile.Clone();
            resolvedColor = null;
        }

        public void ApplyResolvedColor()
        {
            if (!Active || resolvedColor == null || pawn?.story == null)
            {
                return;
            }

            pawn.story.skinColorOverride = new Color?(resolvedColor.Value);
            GeneColorInheritanceUtility.MarkPawnGraphicsDirty(pawn);
        }
    }
}
