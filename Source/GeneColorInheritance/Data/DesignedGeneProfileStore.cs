using System;
using System.Collections.Generic;
using System.IO;
using GeneColorInheritance.Genes;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace GeneColorInheritance.Data
{
    public static class DesignedGeneProfileStore
    {
        public const string ConfigurableTemplateGeneDefName = "Skin_CustomDesigned";

        private const string SidecarRootNode = "geneColorDesignerProfiles";

        private static readonly Dictionary<Dialog_CreateXenotype, DesignedGeneColorProfile> dialogProfiles =
            new();

        private static readonly Dictionary<string, DesignedGeneColorProfile> cachedProfilesByKey =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly System.Reflection.FieldInfo SelectedGenesField = AccessTools.Field(
            typeof(Dialog_CreateXenotype),
            "selectedGenes"
        );

        private static readonly System.Reflection.FieldInfo XenotypeNameField = AccessTools.Field(
            typeof(GeneCreationDialogBase),
            "xenotypeName"
        );

        public static bool IsConfigurableTemplateGene(GeneDef? geneDef)
        {
            return geneDef?.defName == ConfigurableTemplateGeneDefName;
        }

        public static GeneDef? ConfigurableTemplateGene()
        {
            return DefDatabase<GeneDef>.GetNamedSilentFail(ConfigurableTemplateGeneDefName);
        }

        public static bool DialogHasConfigurableGene(Dialog_CreateXenotype dialog)
        {
            GeneDef? templateGene = ConfigurableTemplateGene();
            return templateGene != null && DialogSelectedGenes(dialog).Contains(templateGene);
        }

        public static DesignedGeneColorProfile GetOrCreateDialogProfile(Dialog_CreateXenotype dialog)
        {
            if (dialogProfiles.TryGetValue(dialog, out DesignedGeneColorProfile? existing))
            {
                return existing;
            }

            GeneDef? templateGene = ConfigurableTemplateGene();
            DesignedGeneColorProfile profile;
            if (templateGene == null)
            {
                profile = new DesignedGeneColorProfile();
            }
            else
            {
                string? xenotypeKey = DialogXenotypeKey(dialog);
                profile = TryLoadProfile(xenotypeKey, templateGene)
                    ?? DesignedGeneColorProfile.FromExtension(templateGene);
            }

            dialogProfiles[dialog] = profile;
            return profile;
        }

        public static void SetDialogProfile(
            Dialog_CreateXenotype dialog,
            DesignedGeneColorProfile profile
        )
        {
            DesignedGeneColorProfile cloned = profile.Clone();
            dialogProfiles[dialog] = cloned;

            if (DialogXenotypeKey(dialog) is string key && key.Length > 0)
            {
                cachedProfilesByKey[key] = cloned.Clone();
            }
        }

        public static string DialogProfileSummary(Dialog_CreateXenotype dialog)
        {
            return ProfileSummary(GetOrCreateDialogProfile(dialog));
        }

        public static IEnumerable<Color> PreviewColors(DesignedGeneColorProfile profile, int count)
        {
            count = Mathf.Max(1, count);
            if (profile.HasPaletteColors)
            {
                List<Color> palette = profile.PaletteColorValues();
                if (palette.Count == 1)
                {
                    yield return palette[0];
                    yield break;
                }

                for (int i = 0; i < count; i++)
                {
                    float sample = count == 1 ? 0f : i * (palette.Count - 1f) / (count - 1f);
                    int lowerIndex = Mathf.Clamp(Mathf.FloorToInt(sample), 0, palette.Count - 2);
                    float t = sample - lowerIndex;
                    yield return GeneColorInheritanceUtility.InterpolateColorsHsv(
                        palette[lowerIndex],
                        palette[lowerIndex + 1],
                        t
                    );
                }
                yield break;
            }
        }

        public static void SaveDialogProfile(Dialog_CreateXenotype dialog)
        {
            GeneDef? templateGene = ConfigurableTemplateGene();
            string? xenotypeKey = DialogXenotypeKey(dialog);
            if (templateGene == null || xenotypeKey == null || xenotypeKey.Length == 0)
            {
                return;
            }

            string key = xenotypeKey;
            DesignedGeneProfileDatabase database = LoadDatabase(key)
                ?? new DesignedGeneProfileDatabase { xenotypeKey = key };

            if (DialogHasConfigurableGene(dialog))
            {
                DesignedGeneColorProfile profile = GetOrCreateDialogProfile(dialog).Clone();
                profile.templateGeneDefName = templateGene.defName;
                database.SetProfile(templateGene.defName, profile);
                cachedProfilesByKey[key] = profile.Clone();
            }
            else
            {
                database.RemoveProfile(templateGene.defName);
                cachedProfilesByKey.Remove(key);
            }

            dialogProfiles.Remove(dialog);
            QueueDatabaseSave(key, database);
        }

        public static bool TryApplyProfileFromCustomXenotype(
            Gene_SkinColorRange gene,
            CustomXenotype? xenotype
        )
        {
            if (!IsConfigurableTemplateGene(gene.def))
            {
                return false;
            }

            DesignedGeneColorProfile? profile = TryLoadProfile(XenotypeKey(xenotype), gene.def);
            if (profile == null)
            {
                return false;
            }

            gene.SetDesignedProfile(profile);
            return true;
        }

        public static bool TryApplyFallbackProfileFromPawn(Gene_SkinColorRange gene)
        {
            if (!IsConfigurableTemplateGene(gene.def) || gene.pawn?.genes == null)
            {
                return false;
            }

            string? xenotypeName = gene.pawn.genes.xenotypeName;
            if (!string.IsNullOrEmpty(xenotypeName))
            {
                DesignedGeneColorProfile? profileFromName = TryLoadProfile(
                    GenFile.SanitizedFileName(xenotypeName.Trim()),
                    gene.def
                );
                if (profileFromName != null)
                {
                    gene.SetDesignedProfile(profileFromName);
                    return true;
                }
            }

            return TryApplyProfileFromCustomXenotype(gene, gene.pawn.genes.CustomXenotype);
        }

        public static string ProfileSummary(DesignedGeneColorProfile profile)
        {
            return $"{profile.paletteColors.Count} control color(s)";
        }

        private static List<GeneDef> DialogSelectedGenes(Dialog_CreateXenotype dialog)
        {
            return (List<GeneDef>)SelectedGenesField.GetValue(dialog);
        }

        private static string? DialogXenotypeKey(Dialog_CreateXenotype dialog)
        {
            string? xenotypeName = (string?)XenotypeNameField.GetValue(dialog);
            if (xenotypeName == null || xenotypeName.Trim().Length == 0)
            {
                return null;
            }

            string trimmedName = xenotypeName.Trim();
            return GenFile.SanitizedFileName(trimmedName);
        }

        private static string? XenotypeKey(CustomXenotype? xenotype)
        {
            if (xenotype == null)
            {
                return null;
            }

            if (!xenotype.fileName.NullOrEmpty())
            {
                return xenotype.fileName;
            }

            if (!xenotype.name.NullOrEmpty())
            {
                return GenFile.SanitizedFileName(xenotype.name.Trim());
            }

            return null;
        }

        private static DesignedGeneColorProfile? TryLoadProfile(string? xenotypeKey, GeneDef? geneDef)
        {
            if (geneDef == null || xenotypeKey == null || xenotypeKey.Length == 0)
            {
                return null;
            }

            string key = xenotypeKey;
            if (cachedProfilesByKey.TryGetValue(key, out DesignedGeneColorProfile? cached))
            {
                return cached.Clone();
            }

            DesignedGeneProfileDatabase? database = LoadDatabase(key);
            DesignedGeneColorProfile? profile = database?.GetProfile(geneDef.defName);
            if (profile != null)
            {
                cachedProfilesByKey[key] = profile.Clone();
            }

            return profile;
        }

        private static DesignedGeneProfileDatabase? LoadDatabase(string xenotypeKey)
        {
            string path = SidecarPath(xenotypeKey);
            if (!File.Exists(path))
            {
                return null;
            }

            DesignedGeneProfileDatabase? database = null;
            try
            {
                Scribe.loader.InitLoading(path);
                try
                {
                    if (!Scribe.EnterNode(SidecarRootNode))
                    {
                        return null;
                    }

                    try
                    {
                        Scribe_Deep.Look(ref database, "database");
                    }
                    finally
                    {
                        Scribe.ExitNode();
                    }

                    Scribe.loader.FinalizeLoading();
                }
                catch
                {
                    Scribe.ForceStop();
                    throw;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(
                    $"[Gene Color Designer] Failed to load profile sidecar for xenotype '{xenotypeKey}': {ex}"
                );
                Scribe.ForceStop();
                return null;
            }

            return database;
        }

        private static void SaveDatabase(string xenotypeKey, DesignedGeneProfileDatabase database)
        {
            string path = SidecarPath(xenotypeKey);

            if (!database.HasRecords)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                return;
            }

            try
            {
                SafeSaver.Save(
                    path,
                    SidecarRootNode,
                    delegate
                    {
                        DesignedGeneProfileDatabase db = database;
                        Scribe_Deep.Look(ref db, "database");
                    },
                    leaveOldFile: false
                );
            }
            catch (Exception ex)
            {
                Log.Warning(
                    $"[Gene Color Designer] Failed to save profile sidecar for xenotype '{xenotypeKey}': {ex}"
                );
            }
        }

        private static void QueueDatabaseSave(string xenotypeKey, DesignedGeneProfileDatabase database)
        {
            DesignedGeneProfileDatabase databaseToSave = database;
            LongEventHandler.ExecuteWhenFinished(
                delegate
                {
                    TrySaveWhenSafe(xenotypeKey, databaseToSave);
                }
            );
        }

        private static void TrySaveWhenSafe(string xenotypeKey, DesignedGeneProfileDatabase database)
        {
            if (Scribe.mode != LoadSaveMode.Inactive)
            {
                LongEventHandler.ExecuteWhenFinished(
                    delegate
                    {
                        TrySaveWhenSafe(xenotypeKey, database);
                    }
                );
                return;
            }

            SaveDatabase(xenotypeKey, database);
        }

        private static string SidecarPath(string xenotypeKey)
        {
            string xenotypePath = GenFilePaths.AbsFilePathForXenotype(xenotypeKey);
            string directory = Path.GetDirectoryName(xenotypePath) ?? GenFilePaths.SaveDataFolderPath;
            return Path.Combine(directory, xenotypeKey + ".gcd.xml");
        }
    }
}
