/*
Copyright (c) 2018-2022 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using System.Xml.Schema;
using ClosedXML.Excel;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AdminShellNS;
using Newtonsoft.Json;

namespace AasxPluginExportTable.TimeSeries
{
    /// <summary>
    /// This class allows importing a Submodel for TimeSeries from various formats.
    /// Note: it is a little misplaced in the "export table" plugin, however the
    /// domain is quite the same and maybe special file format dependencies will 
    /// be re equired in the future.
    /// </summary>
    public class ImportTimeSeries
    {
        //
        // Public interface
        //
        
        public static void ImportTimeSeriesFromFile(
            AdminShell.AdministrationShellEnv env,
            AdminShell.Submodel submodel,
            ImportTimeSeriesOptions options,
            string fn)
        {
            // access
            if (options == null | !fn.HasContent())
                return;

            // which writer?
            if (options.Format == ImportTimeSeriesOptions.FormatEnum.Excel)
                ;

        }

        //
        // Internal
        //

        protected class TimeSeriesRow
        {
            public DateTime? TimeStamp = null;
            public List<double> Data = new List<double>();
        }

        protected List<TimeSeriesRow> _rows = null;

        protected int _maxRows = 0, _maxCols = 0;

        protected List<TimeSeriesRow> ImportExcel(
            ImportTimeSeriesOptions options,
            string fn)
        {
            // access
            if (!fn.HasContent())
                return null;

            // open excel
            var wb = new XLWorkbook(fn);
            if (wb?.Worksheets == null || wb.Worksheets.Count < 1)
                return null;

            // only 1st worksheet
            var _worksheet = (wb.Worksheets).ToList()[0];

            // check rows, cols
            _maxRows = (_worksheet?
                .LastRowUsed(XLCellsUsedOptions.Contents) == null)
                ? 0 : _worksheet.LastRowUsed(XLCellsUsedOptions.Contents).RowNumber();

            _maxCols = (_worksheet?
                .LastColumnUsed(XLCellsUsedOptions.Contents) == null)
                ? 0 : _worksheet.LastColumnUsed(XLCellsUsedOptions.Contents).ColumnNumber();

            if (_maxRows < 1 || _maxCols < 1)
                return null;
        }
    }
}
