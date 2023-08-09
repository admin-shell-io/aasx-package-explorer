/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AdminShellNS;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using Aas = AasCore.Aas3_0;

namespace AasxPluginExportTable.Table
{
    /// <summary>
    /// Provides services to the calling application of the plugin 
    /// </summary>
    public class InteropUtils
    {
        public static bool ExportExcel(string fn, AasxPluginExportTableInterop.InteropTable table)
        {
            // access
            if (fn?.HasContent() != true || table == null)
                return false;

            // Excel init
            // Excel with pure OpenXML is very complicated, therefore ClosedXML was used on the top
            // see: https://github.com/closedxml/closedxml/wiki/Basic-Table

            var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Export");

            // cellwise export
            for (int ri = 0; ri < table.Rows.Count; ri++)
            {
                var row = table.Rows[ri];
                for (int ci = 0; ci < row.Cells.Count; ci++)
                {
                    // source
                    var srcCell = row.Cells[ci];

                    // basic cell
                    var dstCell = ws.Cell(1 + ri, 1 + ci);

                    // always wrapping text
                    dstCell.Value = "" + srcCell.Text;
                    dstCell.Style.Alignment.WrapText = srcCell.Wrap;

                    // attributes
                    if (srcCell.Bold)
                        dstCell.Style.Font.Bold = true;
                }
            }

            // save the new worksheet.
            wb.SaveAs(fn);

            // end
            return true;
        }
    }
}
