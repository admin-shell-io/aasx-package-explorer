/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AdminShellNS;
using AnyUi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Aas = AasCore.Aas3_0;

namespace AasxPluginExportTableInterop
{
    /// <summary>
    /// Minimal interface of a table cell
    /// </summary>
    public class InteropCell
    {
        public string Text = "";
        public bool Bold = false;
        public bool Wrap = true;

        public InteropCell() { }

        public InteropCell(string text, bool bold = false, bool wrap = true)
        {
            Text = text;
            Bold = bold;
            Wrap = wrap;
        }
    }

    /// <summary>
    /// Minimal interface of a row. Intention: Index operator
    /// </summary>
    public class InteropRow
    {
        public List<InteropCell> Cells = new List<InteropCell>();

        public InteropRow() { }

        public InteropRow(params string[] cellTexts)
        {
            if (cellTexts == null)
                return;
            foreach (var ct in cellTexts)
                Cells.Add(new InteropCell(ct));
        }

        public InteropRow Set(bool? bold = null, bool? wrap = null)
        {
            foreach (var cell in Cells)
            {
                if (bold.HasValue)
                    cell.Bold = bold.Value;
                if (wrap.HasValue)
                    cell.Wrap = wrap.Value;
            }
            return this;
        }
    }

    /// <summary>
    /// Minimal interface of a table as a set of rows. Intention: Index operator
    /// </summary>
    public class InteropTable
    {
        public List<InteropRow> Rows = new List<InteropRow>();
    }
}
