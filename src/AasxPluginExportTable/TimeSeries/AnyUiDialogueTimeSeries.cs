/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using Newtonsoft.Json;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using AnyUi;
using System.Reflection;

namespace AasxPluginExportTable.TimeSeries
{
    /// <summary>
    /// This class allows exporting a Submodel to various UML formats.
    /// Note: it is a little misplaced in the "export table" plugin, however the
    /// domain is quite the same and maybe special file format dependencies will 
    /// be re equired in the future.
    /// </summary>
    public static class AnyUiDialogueTimeSeries
    {
        public static async Task ImportTimeSeriesDialogBased(
            LogInstance log,
            AasxMenuActionTicket ticket,
            AnyUiContextPlusDialogs displayContext)
        {
            // access
            if (ticket == null || displayContext == null)
                return;

            // check preconditions
            if (ticket.Env == null || ticket.Submodel == null || ticket.SubmodelElement != null)
            {
                log?.Error("Import time series: A Submodel has to be selected!");
                return;
            }

            // ask for parameter record?
            var record = ticket["Record"] as ImportTimeSeriesRecord;
            if (record == null)
                record = new ImportTimeSeriesRecord();

            // arguments by reflection
            ticket?.ArgValue?.PopulateObjectFromArgs(record);

            // maybe given a format name?
            if (ticket["Format"] is string fmt)
                for (int i = 0; i < ImportTimeSeriesRecord.FormatNames.Length; i++)
                    if (ImportTimeSeriesRecord.FormatNames[i].ToLower()
                            .Contains(fmt.ToLower()))
                        record.Format = (ImportTimeSeriesRecord.FormatEnum)i;

            // ok, go on ..
            var uc = new AnyUiDialogueDataModalPanel("Import time series from table ..");
            uc.ActivateRenderPanel(record,
                (uci) =>
                {
                    // create panel
                    var panel = new AnyUiStackPanel();
                    var helper = new AnyUiSmallWidgetToolkit();

                    var g = helper.AddSmallGrid(25, 3, new[] { "220:", "#", "2:" },
                                padding: new AnyUiThickness(0, 5, 0, 5));
                    panel.Add(g);

                    // Row 0 : Format
                    helper.AddSmallLabelTo(g, 0, 0, content: "Format:",
                        verticalAlignment: AnyUiVerticalAlignment.Center,
                        verticalContentAlignment: AnyUiVerticalAlignment.Center);
                    AnyUiUIElement.SetIntFromControl(
                        helper.Set(
                            helper.AddSmallComboBoxTo(g, 0, 1,
                                items: ImportTimeSeriesRecord.FormatNames,
                                selectedIndex: (int)record.Format),
                            minWidth: 200, maxWidth: 200),
                            (i) => { record.Format = (ImportTimeSeriesRecord.FormatEnum)i; });

                    // Row 1..n : automatic generation
                    displayContext.AutoGenerateUiFieldsFor(record, helper, g, startRow: 2);

                    // give back
                    return panel;
                });
            if (!(await displayContext.StartFlyoverModalAsync(uc)))
                return;

            // stop
            await Task.Delay(2000);

            // ask for filename?
            if (!(await displayContext.MenuSelectOpenFilenameToTicketAsync(
                        ticket, "File",
                        "Select file for time series import ..",
                        "",
                        "Tab separated file (*.txt)|*.txt|Tab separated file (*.tsf)|*.tsf|All files (*.*)|*.*",
                        "Import/ export UML: No valid filename.")))
                return;

            var fn = ticket["File"] as string;

            // the Submodel elements need to have parents
            var sm = ticket.Submodel;
            sm.SetAllParents();

            // Import
            ImportTimeSeries.ImportTimeSeriesFromFile(ticket.Env, sm, record, fn, log);

            log.Info($"Importing time series data from table {fn} finished.");
        }
        // dead-csharp off
        //public static bool AutoGenerateUiFieldsFor(
        //    object data, AnyUiContextPlusDialogs displayContext, AnyUiSmallWidgetToolkit helper,
        //    AnyUiGrid grid, int startRow = 0)
        //{
        //    // access
        //    if (data == null || displayContext == null || helper == null || grid == null)
        //        return false;

        //    int row = startRow;

        //    // find fields for this object
        //    var t = data.GetType();
        //    var l = t.GetFields(BindingFlags.Instance | BindingFlags.Public);
        //    foreach (var f in l)
        //    {
        //        var a = f.GetCustomAttribute<AasxMenuArgument>();
        //        if (a != null)
        //        {
        //            // access here
        //            if (a.UiHeader?.HasContent() != true)
        //                continue;

        //            // some more layout options
        //            var gridToAdd = grid;
        //            var grpHelp = a.UiGroupHelp;
        //            var hAlign = AnyUiHorizontalAlignment.Stretch;
        //            int? minWidth = null;
        //            int? maxWidth = null;
        //            int helpGap = 0;
        //            if (a.UiMinWidth.HasValue)
        //            {
        //                grpHelp = true;
        //                minWidth = a.UiMinWidth.Value;
        //            }
        //            if (a.UiMaxWidth.HasValue)
        //            {
        //                grpHelp = true;
        //                maxWidth = a.UiMinWidth.Value;
        //            }
        //            if (grpHelp)
        //            {
        //                hAlign = AnyUiHorizontalAlignment.Left;
        //                helpGap = 10;
        //                gridToAdd = helper.AddSmallGridTo(grid, row, 1, 1, 3, new[] { "0:", "#", "*" });
        //            }

        //            // string
        //            if (f.FieldType == typeof(string)
        //                && f.GetValue(data) is string strVal)
        //            {
        //                AnyUiUIElement.SetStringFromControl(
        //                    helper.Set(
        //                        helper.AddSmallTextBoxTo(gridToAdd, row, 1,
        //                            margin: new AnyUiThickness(0, 2, 2, 2),
        //                            text: "" + strVal,
        //                            verticalAlignment: AnyUiVerticalAlignment.Center,
        //                            verticalContentAlignment: AnyUiVerticalAlignment.Center),
        //                            minWidth: minWidth, maxWidth: maxWidth,
        //                            horizontalAlignment: hAlign),
        //                    (i) =>
        //                    {
        //                        AdminShellUtil.SetFieldLazyValue(f, data, i);
        //                    });
        //            }
        //            else
        //            if (f.FieldType == typeof(bool)
        //                && f.GetValue(data) is bool boolVal)
        //            {
        //                AnyUiUIElement.SetBoolFromControl(
        //                    helper.Set(
        //                        helper.AddSmallCheckBoxTo(gridToAdd, row, 1,
        //                            content: "",
        //                            isChecked: boolVal,
        //                            verticalContentAlignment: AnyUiVerticalAlignment.Center)),
        //                    (b) =>
        //                    {
        //                        AdminShellUtil.SetFieldLazyValue(f, data, b);
        //                    });
        //            }
        //            else
        //            if ((f.FieldType == typeof(byte) || f.FieldType == typeof(sbyte)
        //                || f.FieldType == typeof(Int16) || f.FieldType == typeof(Int32)
        //                || f.FieldType == typeof(Int64) || f.FieldType == typeof(UInt16)
        //                || f.FieldType == typeof(UInt32) || f.FieldType == typeof(UInt64)
        //                || f.FieldType == typeof(Single) || f.FieldType == typeof(Double))
        //                && f.GetValue(data) is object objVal)
        //            {
        //                var valStr = objVal.ToString();
        //                if (objVal is Single fVal)
        //                    valStr = fVal.ToString(CultureInfo.InvariantCulture);
        //                if (objVal is Double dVal)
        //                    valStr = dVal.ToString(CultureInfo.InvariantCulture);

        //                AnyUiUIElement.RegisterControl(
        //                    helper.Set(
        //                        helper.AddSmallTextBoxTo(gridToAdd, row, 1,
        //                            margin: new AnyUiThickness(0, 2, 2, 2),
        //                            text: "" + valStr,
        //                            verticalAlignment: AnyUiVerticalAlignment.Center,
        //                            verticalContentAlignment: AnyUiVerticalAlignment.Center),
        //                            minWidth: minWidth, maxWidth: maxWidth,
        //                            horizontalAlignment: hAlign),
        //                    setValue: (o) =>
        //                    {
        //                        AdminShellUtil.SetFieldLazyValue(f, data, o);
        //                        return new AnyUiLambdaActionNone();
        //                    });
        //            }
        //            else
        //            {
        //                // if not found, no row
        //                continue;
        //            }

        //            // start the row with the header
        //            helper.AddSmallLabelTo(grid, row, 0, content: a.UiHeader + ":",
        //                verticalAlignment: AnyUiVerticalAlignment.Center,
        //                verticalContentAlignment: AnyUiVerticalAlignment.Center);

        //            // help text
        //            if (a.UiShowHelp && a.Help?.HasContent() == true)
        //            {
        //                helper.AddSmallLabelTo(gridToAdd, row, 2, content: "(" + a.Help + ")",
        //                    margin: new AnyUiThickness(helpGap, 0, 0, 0),
        //                    verticalAlignment: AnyUiVerticalAlignment.Center,
        //                    verticalContentAlignment: AnyUiVerticalAlignment.Center);
        //            }

        //            // advance row
        //            row++;
        //        }
        //    }

        //    // OK
        //    return true;
        //}
        // dead-csharp on
    }
}
