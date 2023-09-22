/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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

namespace AasxPluginExportTable
{
    public class ImportExportTableRecord
    {
        //
        // Types
        //

        public enum FormatEnum { TSF = 0, LaTex, Word, Excel, MarkdownGH, AsciiDoc }
        public static string[] FormatNames = new string[] {
            "Tab separated", "LaTex", "Word", "Excel", "Markdown (GH)", "AsciiDoc"
        };

        //
        // Members
        //

        public string Name = "";

        public int Format = 0;

        public int RowsTop = 1, RowsBody = 1, RowsGap = 2, Cols = 2;

        [JsonIgnore]
        public int RealRowsTop { get { return 1 + RowsTop; } }

        [JsonIgnore]
        public int RealRowsBody { get { return 1 + RowsBody; } }

        [JsonIgnore]
        public int RealCols { get { return 1 + Cols; } }

        public bool ReplaceFailedMatches = false;
        public string FailText = "";

        public bool ActInHierarchy = false;

        /// <summary>
        /// If set, will NOT add any headings before each generate table
        /// </summary>
        public bool NoHeadings = false;

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
                serializer.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
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

        //
        // List vs. Matrix management
        //

        /// <summary>
        /// Get a cell content.
        /// </summary>
        /// <param name="top"><c>True if to access Top table.</c></param>
        /// <param name="row">0 for head, 1..n for other rows</param>
        /// <param name="col">0 for head, 1..n for other cols</param>
        /// <returns></returns>
        public string GetCell(bool top, int row, int col)
        {
            // which list?
            var list = top ? Top : Body;

            // which index
            var ndx = col + row * RealCols;

            // any?
            if (list == null || ndx >= list.Count)
                return "";

            // ok
            return list[ndx];
        }

        /// <summary>
        /// Put a cell content. If the list is not existing or to small, it will be created.
        /// </summary>
        /// <param name="top"><c>True if to access Top table.</c></param>
        /// <param name="row">0 for head, 1..n for other rows</param>
        /// <param name="col">0 for head, 1..n for other cols</param>
        /// <returns></returns>
        public void PutCell(bool top, int row, int col, string content)
        {
            // which list?
            var list = Top;
            if (top)
            {
                if (Top == null)
                {
                    Top = new List<string>();
                    list = Top;
                }
            }
            else
            {
                if (Body == null)
                    Body = new List<string>();
                list = Body;
            }

            // which index
            var ndx = col + row * RealCols;

            // enlarge?
            while (list.Count <= ndx)
                list.Add("");

            // ok
            list[ndx] = content;
        }

        /// <summary>
        /// Changes the matrix dimensions. Will set the new values to Rows/ Cols.
        /// </summary>
        public void ReArrange(
            int oldRowsTop, int oldRowsBody, int oldCols,
            int newRowsTop, int newRowsBody, int newCols)
        {
            // save lists
            var oldTop = Top;
            var oldBody = Body;

            // create new ones
            Top = new List<string>();
            while (Top.Count < (1 + newRowsTop) * (1 + newCols))
                Top.Add("");

            Body = new List<string>();
            while (Body.Count < (1 + newRowsBody) * (1 + newCols))
                Body.Add("");

            // commit dimension as well
            RowsTop = newRowsTop;
            RowsBody = newRowsBody;
            Cols = newCols;

            // now copy the old values into the new ones
            for (int or = 0; or < oldRowsTop + 1; or++)
                for (int oc = 0; oc < oldCols + 1; oc++)
                    if (or < (newRowsTop + 1) && oc < (newCols + 1))
                    {
                        var cnt = "";
                        var ondx = or * (1 + oldCols) + oc;
                        if (ondx < oldTop.Count)
                            cnt = oldTop[ondx];
                        PutCell(true, or, oc, cnt);
                    }

            for (int or = 0; or < oldRowsBody + 1; or++)
                for (int oc = 0; oc < oldCols + 1; oc++)
                    if (or < (newRowsBody + 1) && oc < (newCols + 1))
                    {
                        var cnt = "";
                        var ondx = or * (1 + oldCols) + oc;
                        if (ondx < oldBody.Count)
                            cnt = oldBody[ondx];
                        PutCell(false, or, oc, cnt);
                    }
        }

    }
}
