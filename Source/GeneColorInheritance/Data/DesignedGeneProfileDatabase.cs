using System.Collections.Generic;
using System.Linq;
using Verse;

namespace GeneColorInheritance.Data
{
    public class DesignedGeneProfileRecord : IExposable
    {
        public string templateGeneDefName = string.Empty;

        public DesignedGeneColorProfile? profile;

        public void ExposeData()
        {
            Scribe_Values.Look(ref templateGeneDefName, "templateGeneDefName", string.Empty, false);
            Scribe_Deep.Look(ref profile, "profile");
        }
    }

    public class DesignedGeneProfileDatabase : IExposable
    {
        public string xenotypeKey = string.Empty;

        public List<DesignedGeneProfileRecord> records = new();

        public bool HasRecords => records.Count > 0;

        public void ExposeData()
        {
            Scribe_Values.Look(ref xenotypeKey, "xenotypeKey", string.Empty, false);
            Scribe_Collections.Look(ref records, "records", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                records ??= new List<DesignedGeneProfileRecord>();
                records.RemoveAll(record =>
                    record == null
                    || string.IsNullOrEmpty(record.templateGeneDefName)
                    || record.profile == null
                );
            }
        }

        public DesignedGeneColorProfile? GetProfile(string templateGeneDefName)
        {
            return records
                .FirstOrDefault(record => record.templateGeneDefName == templateGeneDefName)
                ?.profile
                ?.Clone();
        }

        public void SetProfile(string templateGeneDefName, DesignedGeneColorProfile profile)
        {
            DesignedGeneProfileRecord? record = records.FirstOrDefault(existing =>
                existing.templateGeneDefName == templateGeneDefName
            );
            if (record == null)
            {
                record = new DesignedGeneProfileRecord
                {
                    templateGeneDefName = templateGeneDefName,
                };
                records.Add(record);
            }

            record.profile = profile.Clone();
        }

        public void RemoveProfile(string templateGeneDefName)
        {
            records.RemoveAll(record => record.templateGeneDefName == templateGeneDefName);
        }
    }
}
