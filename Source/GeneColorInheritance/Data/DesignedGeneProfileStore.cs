using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
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
                if (profile.paletteColors.Count == 1)
                {
                    yield return profile.paletteColors[0].color;
                    yield break;
                }

                for (int i = 0; i < count; i++)
                {
                    float sample =
                        count == 1 ? 0f : i * (profile.paletteColors.Count - 1f) / (count - 1f);
                    int lowerIndex = Mathf.Clamp(
                        Mathf.FloorToInt(sample),
                        0,
                        profile.paletteColors.Count - 2
                    );
                    float t = sample - lowerIndex;
                    yield return GeneColorInheritanceUtility.InterpolateColorsHsv(
                        profile.paletteColors[lowerIndex].color,
                        profile.paletteColors[lowerIndex + 1].color,
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
            SaveDatabase(key, database);
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
                XDocument document = XDocument.Load(path);
                XElement? root = document.Element(SidecarRootNode);
                XElement? databaseNode = root?.Element("database");
                if (databaseNode == null)
                {
                    return null;
                }

                database = DeserializeDatabase(databaseNode);
            }
            catch (Exception ex)
            {
                Log.Warning(
                    $"[Gene Color Designer] Failed to load profile sidecar for xenotype '{xenotypeKey}': {ex}"
                );
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
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                XDocument document = new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    new XElement(SidecarRootNode, SerializeDatabase(database))
                );
                File.WriteAllText(path, document.ToString());
            }
            catch (Exception ex)
            {
                Log.Warning(
                    $"[Gene Color Designer] Failed to save profile sidecar for xenotype '{xenotypeKey}': {ex}"
                );
            }
        }

        private static XElement SerializeDatabase(DesignedGeneProfileDatabase database)
        {
            return new XElement(
                "database",
                new XElement("xenotypeKey", database.xenotypeKey),
                new XElement(
                    "records",
                    database.records
                        .Where(record =>
                            record != null
                            && !string.IsNullOrEmpty(record.templateGeneDefName)
                            && record.profile != null
                        )
                        .Select(SerializeRecord)
                )
            );
        }

        private static XElement SerializeRecord(DesignedGeneProfileRecord record)
        {
            return new XElement(
                "li",
                new XElement("templateGeneDefName", record.templateGeneDefName),
                record.profile != null ? SerializeProfile(record.profile) : null
            );
        }

        private static XElement SerializeProfile(DesignedGeneColorProfile profile)
        {
            return new XElement(
                "profile",
                new XElement("templateGeneDefName", profile.templateGeneDefName),
                new XElement("designId", profile.designId),
                new XElement(
                    "paletteColors",
                    profile.paletteColors.Select(entry =>
                        new XElement("li", new XElement("color", SerializeColor(entry.color)))
                    )
                ),
                new XElement("hueRange", SerializeRange(profile.hueRange)),
                new XElement("saturationRange", SerializeRange(profile.saturationRange)),
                new XElement("valueRange", SerializeRange(profile.valueRange))
            );
        }

        private static DesignedGeneProfileDatabase DeserializeDatabase(XElement element)
        {
            DesignedGeneProfileDatabase database = new DesignedGeneProfileDatabase
            {
                xenotypeKey = (string?)element.Element("xenotypeKey") ?? string.Empty,
            };

            foreach (XElement recordElement in element.Element("records")?.Elements("li") ?? Enumerable.Empty<XElement>())
            {
                DesignedGeneProfileRecord? record = DeserializeRecord(recordElement);
                if (record != null)
                {
                    database.records.Add(record);
                }
            }

            return database;
        }

        private static DesignedGeneProfileRecord? DeserializeRecord(XElement element)
        {
            string? templateGeneDefName = (string?)element.Element("templateGeneDefName");
            XElement? profileElement = element.Element("profile");
            if (string.IsNullOrEmpty(templateGeneDefName) || profileElement == null)
            {
                return null;
            }

            DesignedGeneColorProfile profile = DeserializeProfile(profileElement);
            return new DesignedGeneProfileRecord
            {
                templateGeneDefName = templateGeneDefName!,
                profile = profile,
            };
        }

        private static DesignedGeneColorProfile DeserializeProfile(XElement element)
        {
            DesignedGeneColorProfile profile = new DesignedGeneColorProfile
            {
                templateGeneDefName = (string?)element.Element("templateGeneDefName") ?? string.Empty,
                designId = (string?)element.Element("designId") ?? Guid.NewGuid().ToString("N"),
                hueRange = ParseRange((string?)element.Element("hueRange")),
                saturationRange = ParseRange((string?)element.Element("saturationRange")),
                valueRange = ParseRange((string?)element.Element("valueRange")),
            };

            foreach (XElement colorElement in element.Element("paletteColors")?.Elements("li") ?? Enumerable.Empty<XElement>())
            {
                string? rawColor = (string?)colorElement.Element("color");
                if (TryParseColor(rawColor, out Color color))
                {
                    profile.paletteColors.Add(new DesignedColorEntry(color));
                }
            }

            profile.Normalize();
            return profile;
        }

        private static string SerializeRange(FloatRange range)
        {
            return range.TrueMin.ToString(CultureInfo.InvariantCulture)
                + "~"
                + range.TrueMax.ToString(CultureInfo.InvariantCulture);
        }

        private static FloatRange ParseRange(string? raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return FloatRange.ZeroToOne;
            }

            string text = raw!;
            string[] parts = text.Split('~');
            if (
                parts.Length == 2
                && float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float min)
                && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float max)
            )
            {
                return new FloatRange(min, max);
            }

            return FloatRange.ZeroToOne;
        }

        private static string SerializeColor(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGBA(color);
        }

        private static bool TryParseColor(string? raw, out Color color)
        {
            color = Color.white;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            string text = raw!.Trim();
            if (!text.StartsWith("#") && (text.Length == 6 || text.Length == 8))
            {
                text = "#" + text;
            }

            if (ColorUtility.TryParseHtmlString(text, out color))
            {
                return true;
            }

            if (!text.StartsWith("RGBA(", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string inner = text.Substring(5, text.Length - 6);
            string[] parts = inner.Split(',');
            if (
                parts.Length == 4
                && float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float r)
                && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float g)
                && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float b)
                && float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float a)
            )
            {
                color = new Color(r, g, b, a);
                return true;
            }

            return false;
        }

        private static string SidecarPath(string xenotypeKey)
        {
            string xenotypePath = GenFilePaths.AbsFilePathForXenotype(xenotypeKey);
            string directory = Path.GetDirectoryName(xenotypePath) ?? GenFilePaths.SaveDataFolderPath;
            return Path.Combine(directory, xenotypeKey + ".gcd.xml");
        }
    }
}
