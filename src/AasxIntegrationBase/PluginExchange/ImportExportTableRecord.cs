/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AdminShellNS;
using Newtonsoft.Json;

namespace AasxIntegrationBase
{
    public class ImportExportTableRecord
    {
        //
        // Types
        //

        public enum FormatEnum { TSF = 0, LaTex, Word, Excel }
        public static string[] FormatNames = new string[] { "Tab separated", "LaTex", "Word", "Excel" };

        //
        // Members
        //

        public string Name = "";

        public int Format = 0;

        public int RowsTop = 1, RowsBody = 1, RowsGap = 2, Cols = 1;

        [JsonIgnore]
        public int RealRowsTop { get { return 1 + RowsTop; } }

        [JsonIgnore]
        public int RealRowsBody { get { return 1 + RowsBody; } }

        [JsonIgnore]
        public int RealCols { get { return 1 + Cols; } }

        public bool ReplaceFailedMatches = false;
        public string FailText = "";

        public bool ActInHierarchy = false;

        // Note: the records contains elements for 1 + Rows, 1 + Columns fields
        public List<string> Top = new List<string>();
        public List<string> Body = new List<string>();

        public bool IsValid()
        {
            return RowsTop >= 1 && RowsBody >= 1 && Cols >= 1
                && Top != null && Top.Count >= RealRowsTop * RealCols
                && Body != null && Body.Count >= RealRowsBody * RealCols;
        }

        //
        // Constructurs
        //

        public ImportExportTableRecord() { }

        public ImportExportTableRecord(
            int rowsTop, int rowsBody, int cols, string name = "", IEnumerable<string> header = null,
            IEnumerable<string> elements = null)
        {
            this.RowsTop = rowsTop;
            this.RowsBody = rowsBody;
            this.Cols = cols;
            if (name != null)
                this.Name = name;
            if (header != null)
                foreach (var h in header)
                    this.Top.Add(h);
            if (elements != null)
                foreach (var e in elements)
                    this.Body.Add(e);
        }

        public void SaveToFile(string fn)
        {
            using (StreamWriter file = File.CreateText(fn))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Formatting = Formatting.Indented;
                serializer.Serialize(file, this);
            }
        }

        public static ImportExportTableRecord LoadFromFile(string fn)
        {
            using (StreamReader file = File.OpenText(fn))
            {
                JsonSerializer serializer = new JsonSerializer();
                var res = (ImportExportTableRecord)serializer.Deserialize(file, typeof(ImportExportTableRecord));
                return res;
            }
        }
    }
}
