/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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

namespace AasxIntegrationBase
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
        public string StartTime = "";

        [AasxMenuArgument(help: "Specifies the 1-based row index for the header data of the column names resuting " +
            "in time series variables.")]
        public int RowHeader = 1;

        [AasxMenuArgument(help: "Specifies the 1-based starting row index for the actual data rows.")]
        public int RowData = 2;

        [AasxMenuArgument(help: "If greater zero, specifies the 1-based column index for the time offset value.")]
        public int ColTime = 1;

        [AasxMenuArgument(help: "Specifies the 1-based column index for the data columns.")]
        public int ColData = 2;

        [AasxMenuArgument(help: "Specifies the number of data columns.")]
        public int NumData = 1;

        [AasxMenuArgument(help: "Specifies if to set the semanticId of the given Submodel to the time series spec.")]
        public bool SetSmSemantic = true;
    }
}
