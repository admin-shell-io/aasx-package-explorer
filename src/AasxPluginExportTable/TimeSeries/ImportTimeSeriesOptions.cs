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

namespace AasxPluginExportTable.TimeSeries
{
    public class ImportTimeSeriesOptions
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

        public string StartTime = "";

        public int RowHeader = 1;
        public int RowData = 2;
        public int ColTime = 1;
        public int ColData = 2;
        public int NumData = 1;

        public bool SetSmSemantic = true;
    }
}
