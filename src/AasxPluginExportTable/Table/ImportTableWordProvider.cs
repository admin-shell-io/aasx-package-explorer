/*
Copyright (c) 2018-2023 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace AasxPluginExportTable.Table
{
    public class ImportTableWordProvider : IImportTableProvider
    {
        //
        // Interface
        //

        private int _maxRows = 0;
        public int MaxRows => _maxRows;

        private int _maxCols = 0;
        public int MaxCols => _maxCols;

        public string Cell(int row, int col)
        {
            if (_table == null)
                return null;

            if (row < 0 || row >= _maxRows)
                return null;

            var cells = _table.Elements<TableRow>()?.ElementAt(row);
            if (cells == null)
                return null;
            if (col < 0 || col >= cells.Count())
                return null;

            var cell = _table
                        .Elements<TableRow>()?.ElementAt(row)?
                        .Elements<TableCell>()?.ElementAt(col);

            if (cell == null)
                return null;

            var sb = new StringBuilder();
            var paras = cell?.Elements<Paragraph>();
            if (paras != null)
                foreach (var p in paras)
                {
                    var runs = p?.Elements<Run>();
                    if (runs != null)
                        foreach (var r in runs)
                        {
                            if (r.ChildElements == null)
                                continue;

                            foreach (var rc in r.ChildElements)
                            {
                                if (rc is Text rct)
                                    sb.Append(rct.Text);
                                if (rc is Break)
                                    sb.Append("\n");
                            }
                        }
                    sb.Append("\n");
                }

            var txt = sb.ToString().TrimEnd('\n');
            return txt;
        }

        //
        // Internal members
        //

        protected DocumentFormat.OpenXml.Wordprocessing.Table _table;

        //
        // Factory
        //

        public static IEnumerable<ImportTableWordProvider> CreateProviders(Stream stream)
        {
            // open word
            var document = WordprocessingDocument.Open(stream, isEditable: false);
            var docDoc = document.MainDocumentPart.Document;
            var tables = docDoc.Body.Elements<DocumentFormat.OpenXml.Wordprocessing.Table>();
            if (tables == null)
                yield break;

            foreach (var table in tables)
            {
                // query table dimensions
                if (table == null)
                    continue;
                var mr = table?.Elements<TableRow>()?.Count() ?? -1;
                if (mr < 0)
                    continue;

                var mc = -1;
                foreach (var row in table.Elements<TableRow>())
                {
                    var cls = row?.Elements<TableCell>()?.Count() ?? -1;
                    if (cls > mc)
                        mc = cls;
                }
                if (mc < 0)
                    continue;

                // yield
                var tp = new ImportTableWordProvider()
                {
                    _table = table,
                    _maxRows = mr,
                    _maxCols = mc
                };
                yield return tp;
            }
        }
    }
}
