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
using Newtonsoft.Json;
using Aas = AasCore.Aas3_0_RC02;
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
    public static class AnyUiDialogueTable
    {
        public static async Task ImportExportTableDialogBased(
            LogInstance log,
            AasxMenuActionTicket ticket,
            AnyUiContextPlusDialogs displayContext,
            ExportTableOptions pluginOptions,
            bool doImport)
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
            var record = ticket["Record"] as ImportExportTableRecord;
            if (record == null)
                record = new ImportExportTableRecord();

            // arguments by reflection
            ticket?.ArgValue?.PopulateObjectFromArgs(record);

            // maybe given a format name?
            if (ticket["Format"] is string fmt)
                for (int i = 0; i < ImportExportTableRecord.FormatNames.Length; i++)
                    if (ImportExportTableRecord.FormatNames[i].ToLower()
                            .Contains(fmt.ToLower()))
                        record.Format = i;

            // work rows, cols
            var workRowsTop = record.RowsTop;
            var workRowsBody = record.RowsBody;
            var workCols = record.Cols;

            // ok, go on ..
            var uc = new AnyUiDialogueDataModalPanel(
                (!doImport) 
                    ? "Export SubmodelElements as Table …"
                    : "Import SubmodelElements from Table …");
            uc.ActivateRenderPanel(record,
                (uci) =>
                {
                    // create panel
                    var panel = new AnyUiStackPanel();
                    var helper = new AnyUiSmallWidgetToolkit();

                    var g = helper.AddSmallGrid(25, 2, new[] { "120:", "*" },
                                padding: new AnyUiThickness(0, 5, 0, 5));

                    // TODO: Put this into above function!
                    g.ColumnDefinitions[0].MinWidth = 120;
                    g.ColumnDefinitions[0].MaxWidth = 120;

                    panel.Add(g);

                    // Row 0 : Format
                    helper.AddSmallLabelTo(g, 0, 0, content: "Format:",
                        verticalAlignment: AnyUiVerticalAlignment.Center,
                        verticalContentAlignment: AnyUiVerticalAlignment.Center);
                    AnyUiUIElement.SetIntFromControl(
                        helper.Set(
                            helper.AddSmallComboBoxTo(g, 0, 1,
                                items: ImportExportTableRecord.FormatNames,
                                selectedIndex: (int)record.Format),
                            minWidth: 200, maxWidth: 200),
                            (i) => { record.Format = i; });

                    // Row 1 : Presets
                    {
                        helper.AddSmallLabelTo(g, 1, 0, content: "Presets:",
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                        var g2 = helper.AddSmallGridTo(g, 1, 1, 1, 4, new[] { "#", "#", "#", "*" },
                                    padding: new AnyUiThickness(0, 0, 4, 0));

                        AnyUiUIElement.RegisterControl(
                            helper.AddSmallButtonTo(
                                g2, 0, 0, content: "Load ..",
                                padding: new AnyUiThickness(4, 0, 4, 0)),
                            setValue: (o) =>
                            {
                                return new AnyUiLambdaActionModalPanelReRender();
                            });

                        AnyUiUIElement.RegisterControl(
                            helper.AddSmallButtonTo(
                                g2, 0, 1, content: "Save ..",
                                padding: new AnyUiThickness(4, 0, 4, 0)),
                            setValue: (o) =>
                            {
                                return new AnyUiLambdaActionModalPanelReRender();
                            });

                        if (pluginOptions?.Presets != null)
                        {

                            helper.AddSmallLabelTo(g2, 0, 2, content: "From options:",
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                            AnyUiUIElement.RegisterControl(
                            helper.Set(
                                helper.AddSmallComboBoxTo(g2, 0, 4,
                                    items: pluginOptions.Presets.Select((pr) => "" + pr.Name).ToArray(),
                                    selectedIndex: 0),
                                minWidth: 350, maxWidth: 400),
                                (o) => { 
                                    ;
                                    return new AnyUiLambdaActionModalPanelReRender();
                                });
                        
                        }
                    }

                    // Row 2 : General (1)
                    {
                        helper.AddSmallLabelTo(g, 2, 0, content: "General:",
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                        var g2 = helper.AddSmallGridTo(g, 2, 1, 1, 9, 
                                    new[] { "#", "#", "#", "#", "#", "#", "#", "#", "#" },
                                    padding: new AnyUiThickness(0, 0, 4, 0));

                        // Rows Top

                        helper.AddSmallLabelTo(g2, 0, 0, content: "Rows Top:",
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                        AnyUiUIElement.SetIntFromControl(
                            helper.Set(
                                helper.AddSmallTextBoxTo(g2, 0, 1,
                                    margin: new AnyUiThickness(0, 2, 2, 2),
                                    text: $"{workRowsTop:D}",
                                    verticalAlignment: AnyUiVerticalAlignment.Center,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                    minWidth: 70, maxWidth: 70,
                                    horizontalAlignment: AnyUiHorizontalAlignment.Left),
                                    (i) => { workRowsTop = i; });

                        // Rows Body

                        helper.AddSmallLabelTo(g2, 0, 2, content: "Body:",
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                        AnyUiUIElement.SetIntFromControl(
                            helper.Set(
                                helper.AddSmallTextBoxTo(g2, 0, 3,
                                    margin: new AnyUiThickness(0, 2, 2, 2),
                                    text: $"{workRowsBody:D}",
                                    verticalAlignment: AnyUiVerticalAlignment.Center,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                    minWidth: 70, maxWidth: 70,
                                    horizontalAlignment: AnyUiHorizontalAlignment.Left),
                                    (i) => { workRowsBody = i; });

                        // Gap

                        helper.AddSmallLabelTo(g2, 0, 4, content: "Gap:",
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                        AnyUiUIElement.SetIntFromControl(
                            helper.Set(
                                helper.AddSmallTextBoxTo(g2, 0, 5,
                                    margin: new AnyUiThickness(0, 2, 2, 2),
                                    text: $"{record.RowsGap:D}",
                                    verticalAlignment: AnyUiVerticalAlignment.Center,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                    minWidth: 70, maxWidth: 70,
                                    horizontalAlignment: AnyUiHorizontalAlignment.Left),
                                    (i) => { record.RowsGap = i; });

                        // Columns

                        helper.AddSmallLabelTo(g2, 0, 6, content: "Cols:",
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                        AnyUiUIElement.SetIntFromControl(
                            helper.Set(
                                helper.AddSmallTextBoxTo(g2, 0, 7,
                                    margin: new AnyUiThickness(0, 2, 2, 2),
                                    text: $"{workCols:D}",
                                    verticalAlignment: AnyUiVerticalAlignment.Center,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                    minWidth: 70, maxWidth: 70,
                                    horizontalAlignment: AnyUiHorizontalAlignment.Left),
                                    (i) => { workCols = i; });

                        // Resize

                        var resize = AnyUiUIElement.RegisterControl(
                            helper.AddSmallButtonTo(
                                g2, 0, 8, content: "Resize",
                                padding: new AnyUiThickness(4, 0, 4, 0)),
                            setValue: (o) =>
                            {
                                record.RowsTop = workRowsTop;
                                record.RowsBody = workRowsBody;
                                record.Cols = workCols;

                                return new AnyUiLambdaActionModalPanelReRender();
                            });

                        resize.DirectInvoke = true;
                    }

                    // Row 3 : General (2)
                    {
                        var g2 = helper.AddSmallGridTo(g, 3, 1, 1, 3,
                                    new[] { "#", "#", "#" },
                                    padding: new AnyUiThickness(0, 0, 4, 0));

                        // Act in hierarchy

                        AnyUiUIElement.SetBoolFromControl(
                            helper.Set(
                                helper.AddSmallCheckBoxTo(g2, 0, 0,
                                    content: "Act in hierarchy",
                                    isChecked: record.ActInHierarchy,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center)),
                                (b) => { record.ActInHierarchy = b; });

                        // Replace failed matches

                        AnyUiUIElement.SetBoolFromControl(
                            helper.Set(
                                helper.AddSmallCheckBoxTo(g2, 0, 1,
                                    content: "Replace failed matches: ",
                                    margin: new AnyUiThickness(15, 0, 0, 0),
                                    isChecked: record.ReplaceFailedMatches,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center)),
                                (b) => { record.ReplaceFailedMatches = b; });

                        // Matches Subst

                        AnyUiUIElement.SetStringFromControl(
                            helper.Set(
                                helper.AddSmallTextBoxTo(g2, 0, 2,
                                    text: $"{record.FailText}",
                                    verticalAlignment: AnyUiVerticalAlignment.Center,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                    minWidth: 150, maxWidth: 300,
                                    horizontalAlignment: AnyUiHorizontalAlignment.Left),
                                    (s) => { record.FailText = s; });
                    }

                    // Row (4) : Grid with boxes
                    // For Top & Body, with 1 + 1 + 1 * Cols, 1 + 1 * Rows 
                    {
                        // dimensions
                        var totalRows = record.RealRowsTop + record.RealRowsBody;                        
                        var totalCols = 1 + record.RealCols;

                        // prepare grid
                        var colDef = new List<string>();
                        colDef.Add("120:");
                        for (int i = 0; i < totalCols - 1; i++)
                            colDef.Add("*");

                        var g2 = helper.Set(
                            helper.AddSmallGridTo(g, 4, 0, totalRows, totalCols,
                                colDef.ToArray(),
                                padding: new AnyUiThickness(1, 1, 1, 1)),
                            colSpan: 2);

                        for (int tr=0; tr<totalRows; tr++)
                            for (int tc=0; tc<totalCols; tc++)
                            {
                                // calculate indexes
                                var isTop = tr < record.RealRowsTop;
                                var isFirst = tc == 0;
                                var rIdx = (tr % record.RealRowsTop);
                                var cIdx = (tc - 1);

                                AnyUiFrameworkElement currElem = null;

                                // first col
                                if (isFirst)
                                {
                                    // first columns label
                                    var txtRowHead = (isTop ? "Top" : "Body") + "\u00bb"
                                            + ((rIdx == 0) ? "Head" : $"Row {rIdx}") + ":" ;

                                    currElem = helper.AddSmallLabelTo(g2, tr, 0, content: txtRowHead,
                                        verticalAlignment: AnyUiVerticalAlignment.Top,
                                        verticalContentAlignment: AnyUiVerticalAlignment.Top);                                    
                                
                                }
                                else
                                {
                                    // any other cell = text box
                                    var cell = AnyUiUIElement.SetStringFromControl(
                                        helper.Set(
                                            helper.AddSmallTextBoxTo(g2, tr, tc,
                                                text: "xxx",
                                                verticalAlignment: AnyUiVerticalAlignment.Stretch,
                                                verticalContentAlignment: AnyUiVerticalAlignment.Top),
                                            minWidth: 100,
                                            minHeight: 50,
                                            horizontalAlignment: AnyUiHorizontalAlignment.Stretch),
                                        (s) => { ; });

                                    currElem = cell;

                                    // smaller text
                                    cell.FontSize = 0.7f;
                                }

                                // make a vertical gap between Top & Body
                                if (!isTop && rIdx == 0 && currElem != null)
                                    currElem.Margin = new AnyUiThickness(0, 10, 0, 0);
                            }
                    }


                    // Row 1..n : automatic generation
                    // displayContext.AutoGenerateUiFieldsFor(record, helper, g, startRow: 2);

                    // give back
                    return panel;
                });
            if (!(await displayContext.StartFlyoverModalAsync(uc)))
                return;
            
            //// stop
            //await Task.Delay(2000);

            //// ask for filename?
            //if (!(await displayContext.MenuSelectOpenFilenameToTicketAsync(
            //            ticket, "File",
            //            "Select file for time series import ..",
            //            "",
            //            "Tab separated file (*.txt)|*.txt|Tab separated file (*.tsf)|*.tsf|All files (*.*)|*.*",
            //            "Import/ export UML: No valid filename.")))
            //    return;

            //var fn = ticket["File"] as string;

            //// the Submodel elements need to have parents
            //var sm = ticket.Submodel;
            //sm.SetAllParents();

            //// Import
            //ImportTimeSeries.ImportTimeSeriesFromFile(ticket.Env, sm, record, fn, log);

            //log.Info($"Importing time series data from table {fn} finished.");
        }

    }
}
