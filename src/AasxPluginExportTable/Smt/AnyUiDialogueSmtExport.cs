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
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using AnyUi;
using AasxPluginExportTable.Uml;

namespace AasxPluginExportTable.Smt
{
    /// <summary>
    /// This class allows exporting a Submodel to various UML formats.
    /// Note: it is a little misplaced in the "export table" plugin, however the
    /// domain is quite the same and maybe special file format dependencies will 
    /// be re equired in the future.
    /// </summary>
    public static class AnyUiDialogueSmtExport
    {
        public static async Task ExportSmtDialogBased(
            LogInstance log,
            AasxMenuActionTicket ticket,
            AnyUiContextPlusDialogs displayContext,
            ExportTableOptions pluginOptionsTable)
        {
            // access
            if (ticket == null || displayContext == null)
                return;

            // check preconditions
            if (ticket.Env == null || ticket.Submodel == null || ticket.SubmodelElement != null)
            {
                log?.Error("Export AsciiDoc SMT spec: A Submodel has to be selected!");
                return;
            }

            // ask for parameter record?
            var record = ticket["Record"] as ExportSmtRecord;
            if (record == null)
                record = new ExportSmtRecord();

            // try set correct table preset index
            for (int tpi = 0; tpi < pluginOptionsTable.Presets.Count; tpi++)
                if (pluginOptionsTable.Presets[tpi].Name?.ToLower().Contains("ascii") == true)
                {
                    record.PresetTables = tpi;
                    break;
                }

            // arguments by reflection
            ticket?.ArgValue?.PopulateObjectFromArgs(record);

            // ok, go on ..
            var uc = new AnyUiDialogueDataModalPanel("Export SMT spec as AsciiDoc ..");
            uc.ActivateRenderPanel(record,
                (uci) =>
                {
                    // create panel
                    var panel = new AnyUiStackPanel();
                    var helper = new AnyUiSmallWidgetToolkit();

                    var g = helper.AddSmallGrid(5, 2, new[] { "220:", "*" },
                                padding: new AnyUiThickness(0, 5, 0, 5));
                    panel.Add(g);

                    // Row 0 : Format tables
                    if (pluginOptionsTable?.Presets != null)
                    {
                        helper.AddSmallLabelTo(g, 0, 0, content: "Preset tables:",
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);

                        AnyUiComboBox cbPreset = null;
                        cbPreset = AnyUiUIElement.RegisterControl(
                            helper.Set(
                                helper.AddSmallComboBoxTo(g, 0, 2,
                                    items: pluginOptionsTable.Presets.Select((pr) => "" + pr.Name).ToArray(),
                                    selectedIndex: record.PresetTables),
                                minWidth: 350, maxWidth: 400),
                                (o) =>
                                {
                                    if (!cbPreset.SelectedIndex.HasValue)
                                        return new AnyUiLambdaActionNone();
                                    var ndx = cbPreset.SelectedIndex.Value;
                                    if (ndx < 0 || ndx >= pluginOptionsTable.Presets.Count)
                                        return new AnyUiLambdaActionNone();
                                    record.PresetTables = ndx;
                                    return new AnyUiLambdaActionNone();
                                });

                    }

                    // Row 1 : Word wrap
                    helper.AddSmallLabelTo(g, 1, 0, content: "Word wrap column:",
                        verticalAlignment: AnyUiVerticalAlignment.Center,
                        verticalContentAlignment: AnyUiVerticalAlignment.Center);

                    AnyUiUIElement.SetIntFromControl(
                        helper.Set(
                            helper.AddSmallTextBoxTo(g, 1, 1,
                                margin: new AnyUiThickness(0, 2, 2, 2),
                                text: $"{record.WrapLines:D}",
                                verticalAlignment: AnyUiVerticalAlignment.Center,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                minWidth: 70, maxWidth: 70,
                                horizontalAlignment: AnyUiHorizontalAlignment.Left),
                                (i) => { record.WrapLines = i; });

                    // Row 2 : Include Tables
                    helper.AddSmallLabelTo(g, 2, 0, content: "Include tables:",
                        verticalAlignment: AnyUiVerticalAlignment.Center,
                        verticalContentAlignment: AnyUiVerticalAlignment.Center);
                    AnyUiUIElement.SetBoolFromControl(
                        helper.Set(
                            helper.AddSmallCheckBoxTo(g, 2, 1,
                                content: "(generated code for tables will be in main file)",
                                isChecked: record.IncludeTables,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                colSpan: 2),
                            (b) => { record.IncludeTables = b; });

                    // Row 3 : Export HTML
                    helper.AddSmallLabelTo(g, 3, 0, content: "Export HTML:",
                        verticalAlignment: AnyUiVerticalAlignment.Center,
                        verticalContentAlignment: AnyUiVerticalAlignment.Center);
                    AnyUiUIElement.SetBoolFromControl(
                        helper.Set(
                            helper.AddSmallCheckBoxTo(g, 3, 1,
                                content: "(export command given by options will be executed)",
                                isChecked: record.ExportHtml,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                colSpan: 2),
                            (b) => { record.ExportHtml = b; });

                    // Row 4 : Export PDF
                    helper.AddSmallLabelTo(g, 4, 0, content: "Export PDF:",
                        verticalAlignment: AnyUiVerticalAlignment.Center,
                        verticalContentAlignment: AnyUiVerticalAlignment.Center);
                    AnyUiUIElement.SetBoolFromControl(
                        helper.Set(
                            helper.AddSmallCheckBoxTo(g, 4, 1,
                                content: "(export command given by options will be executed)",
                                isChecked: record.ExportPdf,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                colSpan: 2),
                            (b) => { record.ExportPdf = b; });

                    // give back
                    return panel;
                });

            // scriptmode or ui?
            if (!(ticket?.ScriptMode == true && ticket["File"] != null))
            {
                if (!(await displayContext.StartFlyoverModalAsync(uc)))
                    return;

                // stop
                await Task.Delay(2000);
            }

            // ask for filename?
            if (!(await displayContext.MenuSelectSaveFilenameToTicketAsync(
                        ticket, "File",
                        "Select file for SMT specification to AsciiDoc ..",
                        "new.zip",
                        "AsciiDoc ZIP archive (*.zip)|*.zip|Single AsciiDoc file (*.adoc)|*.adoc|All files (*.*)|*.*",
                        "SMT specification to AsciiDoc: No valid filename.",
                        argLocation: "Location",
                        reworkSpecialFn: true)))
                return;

            var fn = ticket["File"] as string;
            var loc = ticket["Location"];

            // the Submodel elements need to have parents
            var sm = ticket.Submodel;
            sm.SetAllParents();

            // export
            var export = new ExportSmt();
            export.ExportSmtToFile(
                log, displayContext, ticket.Package, 
                sm, pluginOptionsTable, record, fn);

            // persist
            await displayContext.CheckIfDownloadAndStart(log, loc, fn);           

            log.Info($"Export \"SMT specification to AsciiDoc file: {fn}");
        }
    }
}
