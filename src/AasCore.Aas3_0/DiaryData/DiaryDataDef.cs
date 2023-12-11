using AasCore.Aas3_0;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace AdminShellNS.DiaryData
{
    public class DiaryDataDef
    {
        public enum TimeStampKind { Create, Update }

        [XmlIgnore]
        [JsonIgnore]
        private DateTime[] _timeStamp = new DateTime[2];

        [XmlIgnore]
        [JsonIgnore]
        public DateTime[] TimeStamp { get { return _timeStamp; } }

        /// <summary>
        /// List of entries, timewise one after each other (entries are timestamped).
        /// Note: Default is <c>Entries = null</c>, as handling of many many AAS elements does not
        /// create additional overhead of creating empty lists. An empty list shall be avoided.
        /// </summary>
        public List<IAasDiaryEntry> Entries = null;

        public static void AddAndSetTimestamps(IReferable element, IAasDiaryEntry de, bool isCreate = false)
        {
            // trivial
            if (element == null || de == null || element.DiaryData == null)
                return;

            // add entry
            if (element.DiaryData.Entries == null)
                element.DiaryData.Entries = new List<IAasDiaryEntry>();
            element.DiaryData.Entries.Add(de);

            // figure out which timestamp
            var tsk = TimeStampKind.Update;
            if (isCreate)
            {
                tsk = TimeStampKind.Create;
            }

            // set this timestamp (and for the parents, as well)
            IDiaryData el = element;
            while (el?.DiaryData != null)
            {
                // itself
                el.DiaryData.TimeStamp[(int)tsk] = DateTime.UtcNow;

                // go up
                el = (el as IReferable)?.Parent as IDiaryData;
            }
        }
    }
}
