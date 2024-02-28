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
using static AasxPluginExportTable.Uml.ExportUmlRecord;

namespace AasxPluginExportTable.Uml
{
    /// <summary>
    /// This class allows exporting a Submodel to various UML formats.
    /// Note: it is a little misplaced in the "export table" plugin, however the
    /// domain is quite the same and maybe special file format dependencies will 
    /// be re equired in the future.
    /// </summary>
    public static class AnyUiDialogueUmlExport
    {
        public static async Task ExportUmlDialogBased(
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
                log?.Error("Export UML: A Submodel has to be selected!");
                return;
            }

            // ask for parameter record?
            var record = ticket["Record"] as ExportUmlRecord;
            if (record == null)
                record = new ExportUmlRecord();

            // arguments by reflection
            ticket?.ArgValue?.PopulateObjectFromArgs(record);

            // maybe given a format name?
            if (ticket["Format"] is string fmt)
                for (int i = 0; i < ExportUmlRecord.FormatNames.Length; i++)
                    if (ExportUmlRecord.FormatNames[i].ToLower()
                            .Contains(fmt.ToLower()))
                        record.Format = (ExportUmlRecord.ExportFormat)i;

            // ok, go on ..
            var uc = new AnyUiDialogueDataModalPanel("Export UML ..");
            uc.ActivateRenderPanel(record,
                (uci) =>
                {
                    // create panel
                    var panel = new AnyUiStackPanel();
                    var helper = new AnyUiSmallWidgetToolkit();

                    var g = helper.AddSmallGrid(10, 2, new[] { "220:", "*" },
                                padding: new AnyUiThickness(0, 5, 0, 5));
                    panel.Add(g);

                    // Row 0 : Format
                    helper.AddSmallLabelTo(g, 0, 0, content: "Format:",
                        verticalAlignment: AnyUiVerticalAlignment.Center,
                        verticalContentAlignment: AnyUiVerticalAlignment.Center);
                    AnyUiUIElement.SetIntFromControl(
                        helper.Set(
                            helper.AddSmallComboBoxTo(g, 0, 1,
                                items: ExportUmlRecord.FormatNames,
                                selectedIndex: (int)record.Format),
                                minWidth: 600, maxWidth: 600),
                        (i) => { record.Format = (ExportUmlRecord.ExportFormat)i; });

#if __not_promising
                    // Row 1 : PlantUML Format
                    helper.AddSmallLabelTo(g, 1, 0, content: "PlantUML format:",
                        verticalAlignment: AnyUiVerticalAlignment.Center,
                        verticalContentAlignment: AnyUiVerticalAlignment.Center);
                    AnyUiUIElement.SetIntFromControl(
                        helper.Set(
                            helper.AddSmallComboBoxTo(g, 1, 1,
                                items: ExportUmlRecord.PlantFormatNames,
                                selectedIndex: (int)record.PlantFormat),
                                minWidth: 600, maxWidth: 600),
                        (i) => { record.PlantFormat = (PlantUmlFormat) i; });
#endif

                    // Row 2 : Suppress elements
                    helper.AddSmallLabelTo(g, 2, 0, content: "Suppress elements:",
                        verticalAlignment: AnyUiVerticalAlignment.Center,
                        verticalContentAlignment: AnyUiVerticalAlignment.Center);
                    AnyUiUIElement.SetStringFromControl(
                        helper.Set(
                            helper.AddSmallTextBoxTo(g, 2, 1,
                                margin: new AnyUiThickness(0, 2, 2, 2),
                                text: record.Suppress,
                                verticalAlignment: AnyUiVerticalAlignment.Center,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center)),
                        (s) => { record.Suppress = s; });

                    // Row 3 : Depth
                    helper.AddSmallLabelTo(g, 3, 0, content: "Depth:",
                        verticalAlignment: AnyUiVerticalAlignment.Center,
                        verticalContentAlignment: AnyUiVerticalAlignment.Center);

                    var g11 = helper.AddSmallGridTo(g, 3, 1, 1, 2, new[] { "200:", "*" });
                    AnyUiUIElement.SetIntFromControl(
                        helper.Set(
                            helper.AddSmallTextBoxTo(g11, 0, 0,
                                margin: new AnyUiThickness(0, 2, 2, 2),
                                text: $"{record.Depth:D}",
                                verticalAlignment: AnyUiVerticalAlignment.Center,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center)),
                        (i) => { record.Depth = i; });
                    helper.AddSmallLabelTo(g11, 0, 1,
                        content: "(0 = all, 1 = only top level, ..)",
                        margin: new AnyUiThickness(10, 0, 0, 0),
                        verticalAlignment: AnyUiVerticalAlignment.Center,
                        verticalContentAlignment: AnyUiVerticalAlignment.Center);

                    // Row 4 : limiting of values im UML
                    helper.AddSmallLabelTo(g, 4, 0, content: "Limit values:",
                        verticalAlignment: AnyUiVerticalAlignment.Center,
                        verticalContentAlignment: AnyUiVerticalAlignment.Center);

                    var g2 = helper.AddSmallGridTo(g, 4, 1, 1, 2, new[] { "200:", "*" });
                    AnyUiUIElement.SetIntFromControl(
                        helper.Set(
                            helper.AddSmallTextBoxTo(g2, 0, 0,
                                margin: new AnyUiThickness(0, 2, 2, 2),
                                text: $"{record.LimitInitialValue:D}",
                                verticalAlignment: AnyUiVerticalAlignment.Center,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center) /*,
                        minWidth: 400, maxWidth: 400,
                        horizontalAlignment: AnyUiHorizontalAlignment.Left */),
                        (i) => { record.LimitInitialValue = i; });
                    helper.AddSmallLabelTo(g2, 0, 1,
                        content: "(0 disables values, -1 = unlimited)",
                        margin: new AnyUiThickness(10, 0, 0, 0),
                        verticalAlignment: AnyUiVerticalAlignment.Center,
                        verticalContentAlignment: AnyUiVerticalAlignment.Center);

                    // Row 5 : Outline
                    helper.AddSmallLabelTo(g, 5, 0, content: "Outline:",
                        verticalAlignment: AnyUiVerticalAlignment.Center,
                        verticalContentAlignment: AnyUiVerticalAlignment.Center);
                    AnyUiUIElement.SetBoolFromControl(
                        helper.Set(
                            helper.AddSmallCheckBoxTo(g, 5, 1,
                                content: "(no members in classes, compact)",
                                isChecked: record.Outline,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                colSpan: 2),
                            (b) => { record.Outline = b; });

                    // Row 6 : SwapDirection
                    helper.AddSmallLabelTo(g, 6, 0, content: "Swap direction:",
                        verticalAlignment: AnyUiVerticalAlignment.Center,
                        verticalContentAlignment: AnyUiVerticalAlignment.Center);
                    AnyUiUIElement.SetBoolFromControl(
                        helper.Set(
                            helper.AddSmallCheckBoxTo(g, 6, 1,
                                content: "(changed direction for adding graphical elements)",
                                isChecked: record.SwapDirection,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                colSpan: 2),
                            (b) => { record.SwapDirection = b; });

                    // Row 7 : Copy to paste buffer
                    helper.AddSmallLabelTo(g, 7, 0, content: "Classes from concepts:",
                        verticalAlignment: AnyUiVerticalAlignment.Center,
                        verticalContentAlignment: AnyUiVerticalAlignment.Center);
                    AnyUiUIElement.SetBoolFromControl(
                        helper.Set(
                            helper.AddSmallCheckBoxTo(g, 7, 1,
                                content: "(will use concept idShort as names of classes)",
                                isChecked: record.ClassesFromConcepts,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                colSpan: 2),
                            (b) => { record.ClassesFromConcepts = b; });

                    // Row 8 : Copy to paste buffer
                    helper.AddSmallLabelTo(g, 8, 0, content: "No association names:",
                        verticalAlignment: AnyUiVerticalAlignment.Center,
                        verticalContentAlignment: AnyUiVerticalAlignment.Center);
                    AnyUiUIElement.SetBoolFromControl(
                        helper.Set(
                            helper.AddSmallCheckBoxTo(g, 8, 1,
                                content: "(will not name ends of associations, cleaner)",
                                isChecked: record.NoAssociationNames,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                colSpan: 2),
                            (b) => { record.NoAssociationNames = b; });

                    // Row 9 : Copy to paste buffer
                    helper.AddSmallLabelTo(g, 9, 0, content: "Copy to paste buffer:",
                        verticalAlignment: AnyUiVerticalAlignment.Center,
                        verticalContentAlignment: AnyUiVerticalAlignment.Center);
                    AnyUiUIElement.SetBoolFromControl(
                        helper.Set(
                            helper.AddSmallCheckBoxTo(g, 9, 1,
                                content: "(after export, file will be copied to paste buffer)",
                                isChecked: record.CopyToPasteBuffer,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                colSpan: 2),
                            (b) => { record.CopyToPasteBuffer = b; });

                    // give back
                    return panel;
                });

            // scriptmode or ui?
            if (!(ticket?.ScriptMode == true && ticket["File"] != null))
            {
                if (!(await displayContext.StartFlyoverModalAsync(uc)))
                    return;
            }

            // stop
            await Task.Delay(2000);

            // ask for filename?
            if (!(await displayContext.MenuSelectSaveFilenameToTicketAsync(
                        ticket, "File",
                        "Select file for UML export ..",
                        "new.puml",
                        "PlantUML text file (*.puml)|*.puml|UML text file (*.uml)|*.uml|All files (*.*)|*.*",
                        "Import/ export UML: No valid filename.",
                        argLocation: "Location",
                        reworkSpecialFn: true)))
                return;

            var fn = ticket["File"] as string;
            var loc = ticket["Location"];

            // the Submodel elements need to have parents
            var sm = ticket.Submodel;
            sm.SetAllParents();

            // export
            ExportUml.ExportUmlToFile(ticket.Env, sm, record, fn);

            // persist
            await displayContext.CheckIfDownloadAndStart(log, loc, fn);
            if (record.CopyToPasteBuffer)
            {
                var lines = System.IO.File.ReadAllText(fn);
                displayContext.ClipboardSet(new AnyUiClipboardData(lines));
            }

            log.Info($"Export UML data to file: {fn}");
        }
    }
}
