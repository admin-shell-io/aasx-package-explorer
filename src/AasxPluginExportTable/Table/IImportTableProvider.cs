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

namespace AasxPluginExportTable.Table
{
    /// <summary>
    /// This interface provides an abstraction of a table with string-valued cells
    /// in rows and cols. Idea is to have file-format specific factories for this
    /// providers. 
    /// </summary>
    public interface IImportTableProvider
    {
        /// <summary>
        /// Potential maximum values for rows and cols. Can be much larger than
        /// the real dimensions!
        /// </summary>
        int MaxRows { get; }

        /// <summary>
        /// Potential maximum values for rows and cols. Can be much larger than
        /// the real dimensions!
        /// </summary>
        int MaxCols { get; }

        /// <summary>
        /// Returns cell value or <c>null</c>. Coordinates <c>row, col </c> are zero-based.
        /// </summary>
        /// <returns></returns>
        string Cell(int row, int col);
    }
}
