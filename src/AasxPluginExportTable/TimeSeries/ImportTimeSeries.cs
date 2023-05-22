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
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using ClosedXML.Excel;
using Newtonsoft.Json;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using AasxPluginExportTable.Table;

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
            Aas.Environment env,
            Aas.ISubmodel submodel,
            ImportTimeSeriesRecord options,
            string fn, LogInstance log = null)
        {
            // access
            if (options == null | !fn.HasContent())
            {
                log?.Error("Import time series: no valid filename!");
                return;
            }

            // safe
            try
            {
                // which importer?
                var imp = new ImportTimeSeries();

                if (options.Format == ImportTimeSeriesRecord.FormatEnum.Excel)
                {
                    imp._log = log;
                    imp._provider = ImportTableExcelProvider.CreateProviders(fn).FirstOrDefault();
                    if (!imp.ImportExcel(options))
                    {
                        log?.Error($"Error accessing table {fn} !");
                        return;
                    }
                }

                if (!imp.WriteSeries(options, submodel))
                {
                    log?.Error($"Error writing timeseries data !");
                }
            }
            catch (Exception ex)
            {
                log?.Error(ex, $"importing time series file {fn}");
            }
        }

        //
        // Helpers
        //

        public static string ConvertToIso8601(DateTime? dt, bool withMilliSeconds = false)
        {
            if (!dt.HasValue)
                return "";
            if (!withMilliSeconds)
                return dt.Value.ToString("yyyy-MM-dd'T'HH:mm:ssZ");
            return dt.Value.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffffZ");
        }

        public static DateTime? ConvertFromIso8601(string input)
        {
            if (DateTime.TryParseExact(input,
                "yyyy-MM-dd'T'HH:mm:ssZ", CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal, out DateTime dt))
                return dt;
            return null;
        }

        //
        // Internal
        //

        protected class TimeSeriesRow
        {
            public DateTime? TimeStamp = null;
            public List<double> Data = new List<double>();
        }

        protected class ListOfTimeSeriesRow : List<TimeSeriesRow>
        {
            public string GenerateJsonColumn(
                int colIndex, // -1 = time stamp!
                int indexOffset = 0,
                int startOfList = -1,
                int endOfList = -1)
            {
                // access
                if (colIndex < -1)
                    return "";
                if (startOfList < 0)
                    startOfList = 0;
                if (endOfList < 0)
                    endOfList = this.Count - 1;
                if (endOfList >= this.Count)
                    endOfList = this.Count - 1;

                // sum of strings
                var sb = new List<string>();
                for (int i = startOfList; i <= endOfList; i++)
                {
                    // access
                    var row = this[i];
                    string data = null;
                    if (colIndex >= 0 && row.Data != null && row.Data.Count > colIndex)
                        data = row.Data[colIndex].ToString("G", CultureInfo.InvariantCulture);
                    if (colIndex == -1 && row.TimeStamp.HasValue)
                        data = ConvertToIso8601(row.TimeStamp, withMilliSeconds: true);
                    if (data == null)
                        continue;

                    // add
                    sb.Add(String.Format("[ {0}, {1} ]",
                        i - startOfList + indexOffset,
                        data));
                }

                // return
                return string.Join(", ", sb);
            }
        }

        protected LogInstance _log = null;

        protected List<string> _columnNames = null;

        protected ListOfTimeSeriesRow _rows = null;

        protected IImportTableProvider _provider = null;

        protected bool ImportExcel(
            ImportTimeSeriesRecord options)
        {
            // access
            if (_provider == null || options == null)
                return false;
            if (_provider.MaxRows < 1 || _provider.MaxCols < 1)
                return false;

            // results
            _columnNames = new List<string>();
            _rows = new ListOfTimeSeriesRow();

            // start time
            var startTimeDT = ConvertFromIso8601(options.StartTime);
            if (!startTimeDT.HasValue)
            {
                _log?.Error("No start time specified in ISO8601 UTC format");
                return false;
            }
            var startTimeOfs = 0.0;

            // read column names
            if (options.RowHeader > 0 && options.NumData > 0)
                for (int di = 0; di < options.NumData; di++)
                    _columnNames.Add("" + _provider.Cell(options.RowHeader - 1, options.ColData - 1 + di));

            // read data rows
            var rowI = options.RowData - 1;
            while (true)
            {
                // try gather delta
                double ofs = 0.0;
                if (options.ColTime > 0)
                {
                    var st = "" + _provider.Cell(rowI, options.ColTime - 1);
                    st = st.Replace(',', '.');
                    if (double.TryParse(st, NumberStyles.Float, CultureInfo.InvariantCulture, out double fd))
                        ofs = fd;
                }

                // try gather data
                var dataPresent = false;
                var data = new List<double>();
                for (int di = 0; di < options.NumData; di++)
                {
                    var st = "" + _provider.Cell(rowI, options.ColData - 1 + di);
                    st = st.Replace(',', '.');
                    double db = 0.0;
                    if (double.TryParse(st, NumberStyles.Float, CultureInfo.InvariantCulture, out double f))
                    {
                        db = f;
                        dataPresent = true;
                    }
                    data.Add(db);
                }

                // successful?
                if (!dataPresent)
                    break;

                // ok, add
                var rec = new TimeSeriesRow()
                {
                    TimeStamp = (options.ColTime > 0)
                        ? startTimeDT + TimeSpan.FromSeconds(ofs)
                        : startTimeDT + TimeSpan.FromSeconds(startTimeOfs),
                    Data = data
                };
                _rows.Add(rec);

                // further
                rowI++;
                startTimeOfs += 1;
            }

            // done
            return true;
        }

        protected bool WriteSeries(
            ImportTimeSeriesRecord options,
            Aas.ISubmodel submodel)
        {
            // access 
            if (options == null || submodel == null || _rows == null || _columnNames == null)
                return false;

            // definitions
            var defs = AasxPredefinedConcepts.ZveiTimeSeriesDataV10.Static;

            // set semanticId
            if (options.SetSmSemantic)
                submodel.SemanticId = defs.SM_TimeSeriesData?.SemanticId?.Copy();

            // time series
            var smcTimeSeries = submodel.SubmodelElements.CreateSMEForCD<Aas.SubmodelElementCollection>(
                    defs.CD_TimeSeries, addSme: true);

            // attributes for this

            smcTimeSeries.Value.CreateSMEForCD<Aas.MultiLanguageProperty>(defs.CD_Name, addSme: true)?
                .Set("en", "To be defined");

            smcTimeSeries.Value.CreateSMEForCD<Aas.MultiLanguageProperty>(defs.CD_Description, addSme: true)?
                .Set("en", "To be defined");

            while (true)
            {
                // segment
                var smcSegment = smcTimeSeries.Value.CreateSMEForCD<Aas.SubmodelElementCollection>(
                    defs.CD_TimeSeriesSegment, addSme: true);

                // chunk of records (idea: simply copy!)
                var chunk = _rows;

                // attributes for this

                smcSegment.Value.CreateSMEForCD<Aas.MultiLanguageProperty>(defs.CD_Name, addSme: true)?
                    .Set("en", "To be defined");

                smcSegment.Value.CreateSMEForCD<Aas.MultiLanguageProperty>(defs.CD_Description, addSme: true)?
                    .Set("en", "To be defined");

                smcSegment.Value.CreateSMEForCD<Aas.Property>(defs.CD_RecordCount, addSme: true)?
                    .Set(Aas.DataTypeDefXsd.Integer, "" + chunk.Count);

                smcSegment.Value.CreateSMEForCD<Aas.Property>(defs.CD_StartTime, addSme: true)?
                    .Set(Aas.DataTypeDefXsd.DateTime, "");

                smcSegment.Value.CreateSMEForCD<Aas.Property>(defs.CD_EndTime, addSme: true)?
                    .Set(Aas.DataTypeDefXsd.DateTime, "");

                // Time Stamps? == TimeSeriesVariable

                if (options.ColTime >= 1)
                {
                    // variable
                    var smcVar = smcSegment.Value.CreateSMEForCD<Aas.SubmodelElementCollection>(
                        defs.CD_TimeSeriesVariable, idShort: "TimeSeriesVariable_TimeStamps", addSme: true);

                    // attributes for this

                    smcVar.Value.CreateSMEForCD<Aas.Property>(defs.CD_RecordId, addSme: true, isTemplate: true)?
                        .Set(Aas.DataTypeDefXsd.Integer, "" + (0));

                    smcVar.Value.CreateSMEForCD<Aas.Property>(defs.CD_UtcTime, addSme: true, isTemplate: true);

                    var va = smcVar.Value.CreateSMEForCD<Aas.Blob>(defs.CD_ValueArray, addSme: true);
                    if (va != null)
                    {
                        va.Value = Encoding.Default.GetBytes(
                            chunk.GenerateJsonColumn(
                                colIndex: -1, // for time stamp
                                indexOffset: 0));
                    }
                }

                // each Column == TimeSeriesVariable

                for (int di = 0; di < options.NumData; di++)
                {
                    // variable
                    // ReSharper disable once UseFormatSpecifierInInterpolation
                    var ids = $"TimeSeriesVariable_{(1 + di).ToString("D2")}";
                    if (options.RowHeader >= 1 && _columnNames.Count > di)
                        ids += "_" + _columnNames[di];
                    var smcVar = smcSegment.Value.CreateSMEForCD<Aas.SubmodelElementCollection>(
                        defs.CD_TimeSeriesVariable, idShort: ids, addSme: true);

                    // attributes for this

                    smcVar.Value.CreateSMEForCD<Aas.Property>(defs.CD_RecordId, addSme: true, isTemplate: true)?
                        .Set(Aas.DataTypeDefXsd.Integer, "" + (1 + di));

                    var dp = new Aas.Property(Aas.DataTypeDefXsd.Double, idShort: "DataPoint");
                    smcVar.Value.Add(dp);

                    var va = smcVar.Value.CreateSMEForCD<Aas.Blob>(defs.CD_ValueArray, addSme: true);
                    if (va != null)
                    {
                        va.Value = Encoding.Default.GetBytes(
                            chunk.GenerateJsonColumn(
                                colIndex: di,
                                indexOffset: 0));
                    }
                }

                // not more
                break;
            }

            // ok
            return true;
        }
    }
}
