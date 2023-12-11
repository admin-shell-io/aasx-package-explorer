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
using AdminShellNS;
using AasxIntegrationBase;
using AnyUi;

namespace AasxPluginExportTable.TimeSeries
{
    public class ImportTimeSeriesRecord
    {
        //
        // Types
        //

        public enum FormatEnum { Excel }
        public static string[] FormatNames = new string[] { "Excel" };

        //
        // Members
        //

        public FormatEnum Format = 0;

        [AasxMenuArgument(help: "Offset time for time series values in ISO6801 format.")]
        [AnyUiEditField(uiHeader: "Start time",
            uiShowHelp: true, uiGroupHelp: true, minWidth: 300, maxWidth: 300)]
        public string StartTime = "";

        [AasxMenuArgument(help: "Specifies the 1-based row index for the header data of the column names resuting " +
            "in time series variables.")]
        [AnyUiEditField(uiHeader: "Row for header",
            uiShowHelp: true, uiGroupHelp: true, minWidth: 150, maxWidth: 150)]
        public int RowHeader = 1;

        [AasxMenuArgument(help: "Specifies the 1-based starting row index for the actual data rows.")]
        [AnyUiEditField(uiHeader: "Row for data",
            uiShowHelp: true, uiGroupHelp: true, minWidth: 150, maxWidth: 150)]
        public int RowData = 2;

        [AasxMenuArgument(help: "If greater zero, specifies the 1-based column index for the time offset value.")]
        [AnyUiEditField(uiHeader: "Col for time",
            uiShowHelp: true, uiGroupHelp: true, minWidth: 150, maxWidth: 150)]
        public int ColTime = 1;

        [AasxMenuArgument(help: "Specifies the 1-based column index for the data columns.")]
        [AnyUiEditField(uiHeader: "Col for data",
            uiShowHelp: true, uiGroupHelp: true, minWidth: 150, maxWidth: 150)]
        public int ColData = 2;

        [AasxMenuArgument(help: "Specifies the number of data columns.")]
        [AnyUiEditField(uiHeader: "# data columns",
            uiShowHelp: true, uiGroupHelp: true, minWidth: 150, maxWidth: 150)]
        public int NumData = 1;

        [AasxMenuArgument(help: "Specifies if to set the semanticId of the given Submodel to the time series spec.")]
        [AnyUiEditField(uiHeader: "Set SM semanticId",
            uiShowHelp: true, uiGroupHelp: true, minWidth: 150, maxWidth: 150)]
        public bool SetSmSemantic = true;
    }
}
