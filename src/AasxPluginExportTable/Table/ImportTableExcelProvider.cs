/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace AasxPluginExportTable.Table
{
    public class ImportTableExcelProvider : IImportTableProvider
    {
        //
        // Interface
        //

        public int MaxRows => _worksheet?
            .LastRowUsed(XLCellsUsedOptions.Contents) == null
            ? 0 : _worksheet.LastRowUsed(XLCellsUsedOptions.Contents).RowNumber();

        public int MaxCols => _worksheet?
            .LastColumnUsed(XLCellsUsedOptions.Contents) == null
            ? 0 : _worksheet.LastColumnUsed(XLCellsUsedOptions.Contents).ColumnNumber();

        public string Cell(int row, int col)
        {
            if (_worksheet == null)
                return null;
            return _worksheet.Cell(1 + row, 1 + col)?.Value?.ToString();
        }

        //
        // Internal members
        //

        protected IXLWorksheet _worksheet;

        //
        // Factory
        //

        public static IEnumerable<ImportTableExcelProvider> CreateProviders(string fn)
        {
            // open excel
            var wb = new XLWorkbook(fn);
            if (wb?.Worksheets == null || wb.Worksheets.Count < 1)
                yield break;

            foreach (var ws in wb.Worksheets)
            {
                var tp = new ImportTableExcelProvider()
                {
                    _worksheet = ws
                };
                yield return tp;
            }
        }
    }
}
