/*
Copyright (c) 2018-2023 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxPluginExportTable.Table;
using AnyUi;
using Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aas = AasCore.Aas3_0;

namespace AasxPluginExportTable.Table
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
            ExportTableOptions options,
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

            // maybe given a preset name?
            if (ticket["Preset"] is string pname && pluginOptions.Presets != null)
                for (int i = 0; i < pluginOptions.Presets.Count; i++)
                    if (pluginOptions.Presets[i].Name.ToLower()
                            .Contains(pname.ToLower()))
                    {
                        record = pluginOptions.Presets[i];
                    }

            // arguments by reflection
            ticket?.ArgValue?.PopulateObjectFromArgs(record);

            // maybe given a format name?
            if (ticket["Format"] is string fmt)
                for (int i = 0; i < ImportExportTableRecord.FormatNames.Length; i++)
                    if (ImportExportTableRecord.FormatNames[i].ToLower()
                            .Contains(fmt.ToLower()))
                        record.Format = i;

            // work rows, cols
            int workRowsTop, workRowsBody, workCols;

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

                    // (re-)set work rows, cols
                    workRowsTop = record.RowsTop;
                    workRowsBody = record.RowsBody;
                    workCols = record.Cols;

                    // TODO (???, 0000-00-00): Put this into above function!
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

                        var g2 = helper.AddSmallGridTo(g, 1, 1, 1, 4, new[] { "#", "#", "#", "#" },
                                    padding: new AnyUiThickness(0, 0, 4, 0));

                        if (displayContext.HasCapability(AnyUiContextCapability.DialogWithoutFlyover))
                        {

                            AnyUiUIElement.RegisterControl(
                                helper.AddSmallButtonTo(
                                    g2, 0, 0, content: "Load ..",
                                    padding: new AnyUiThickness(4, 0, 4, 0)),
                                setValueAsync: async (o) =>
                                {
                                    // ask for filename
                                    var ofData = await displayContext.MenuSelectOpenFilenameAsync(
                                        ticket: null, argName: null,
                                        caption: "Select preset JSON file to load ..",
                                        proposeFn: "",
                                        filter: "Preset JSON file (*.json)|*.json|All files (*.*)|*.*",
                                        msg: "Not found",
                                        requireNoFlyout: true);
                                    if (ofData?.Result != true)
                                        return new AnyUiLambdaActionNone();

                                    // load new data
                                    try
                                    {
                                        log?.Info("Loading new preset data {0} ..", ofData.TargetFileName);
                                        var newRec = ImportExportTableRecord.LoadFromFile(ofData.TargetFileName);
                                        record = newRec;
                                        uc.Data = newRec;
                                        return new AnyUiLambdaActionModalPanelReRender(uc);
                                    }
                                    catch (Exception ex)
                                    {
                                        log?.Error(ex, "when loading plugin preset data");
                                    }
                                    return new AnyUiLambdaActionNone();
                                });

                            AnyUiUIElement.RegisterControl(
                                helper.AddSmallButtonTo(
                                    g2, 0, 1, content: "Save ..",
                                    padding: new AnyUiThickness(4, 0, 4, 0)),
                                setValueAsync: async (o) =>
                                {
                                    // ask for filename
                                    var sfData = await displayContext.MenuSelectSaveFilenameAsync(
                                        ticket: null, argName: null,
                                        caption: "Select preset JSON file to save ..",
                                        proposeFn: "new.json",
                                        filter: "Preset JSON file (*.json)|*.json|All files (*.*)|*.*",
                                        msg: "Not found");
                                    if (sfData?.Result != true)
                                        return new AnyUiLambdaActionNone();

                                    // save new data
                                    try
                                    {
                                        record.SaveToFile(sfData.TargetFileName);
                                        log?.Info("Saved preset data to {0}.", sfData.TargetFileName);
                                    }
                                    catch (Exception ex)
                                    {
                                        log?.Error(ex, "when saving plugin preset data");
                                    }
                                    return new AnyUiLambdaActionNone();
                                });
                        }

                        if (pluginOptions?.Presets != null)
                        {
                            helper.AddSmallLabelTo(g2, 0, 2, content: "From options:",
                                verticalAlignment: AnyUiVerticalAlignment.Center,
                                verticalContentAlignment: AnyUiVerticalAlignment.Center);

                            AnyUiComboBox cbPreset = null;
                            cbPreset = AnyUiUIElement.RegisterControl(
                                helper.Set(
                                    helper.AddSmallComboBoxTo(g2, 0, 4,
                                        items: pluginOptions.Presets.Select((pr) => "" + pr.Name).ToArray(),
                                        text: "Please select preset to load .."),
                                    minWidth: 350, maxWidth: 400),
                                    (o) =>
                                    {
                                        if (!cbPreset.SelectedIndex.HasValue)
                                            return new AnyUiLambdaActionNone();
                                        var ndx = cbPreset.SelectedIndex.Value;
                                        if (ndx < 0 || ndx >= pluginOptions.Presets.Count)
                                            return new AnyUiLambdaActionNone();
                                        var newRec = pluginOptions.Presets[ndx];
                                        record = newRec;
                                        uc.Data = newRec;
                                        return new AnyUiLambdaActionModalPanelReRender(uc);
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
                                record.ReArrange(record.RowsTop, record.RowsBody, record.Cols,
                                    workRowsTop, workRowsBody, workCols);

                                return new AnyUiLambdaActionModalPanelReRender(uc);
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
                        var totalRows = 1 + record.RealRowsTop + record.RealRowsBody;
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

                        // column headers
                        for (int tc = 0; tc < totalCols; tc++)
                        {
                            // first columns label
                            var txtRowHead = (tc == 0) ? "Table"
                                    : ((tc == 1) ? "Column\u00bbHead" : $"Column {tc - 1}");

                            helper.Set(
                                helper.AddSmallLabelTo(g2, 0, tc,
                                    content: txtRowHead,
                                    verticalAlignment: AnyUiVerticalAlignment.Bottom,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Bottom),
                                horizontalAlignment: AnyUiHorizontalAlignment.Left,
                                horizontalContentAlignment: AnyUiHorizontalAlignment.Left);
                        }

                        // column bodies, row by row
                        for (int tr = 0; tr < totalRows - 1; tr++)
                            for (int tc = 0; tc < totalCols; tc++)
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
                                            + ((rIdx == 0) ? "Head" : $"Row {rIdx}") + ":";

                                    currElem = helper.AddSmallLabelTo(g2, 1 + tr, 0, content: txtRowHead,
                                        verticalAlignment: AnyUiVerticalAlignment.Top,
                                        verticalContentAlignment: AnyUiVerticalAlignment.Top);

                                }
                                else
                                {
                                    // any other cell = text box
                                    var cell = AnyUiUIElement.SetStringFromControl(
                                        helper.Set(
                                            helper.AddSmallTextBoxTo(g2, 1 + tr, tc,
                                                text: "" + record.GetCell(isTop, rIdx, cIdx),
                                                verticalAlignment: AnyUiVerticalAlignment.Stretch,
                                                verticalContentAlignment: AnyUiVerticalAlignment.Top,
                                                textWrap: AnyUiTextWrapping.Wrap,
                                                fontSize: 0.7, multiLine: true),
                                            minWidth: 100,
                                            minHeight: 60,
                                            horizontalAlignment: AnyUiHorizontalAlignment.Stretch),
                                        (s) =>
                                        {
                                            record.PutCell(isTop, rIdx, cIdx, s);
                                        });

                                    currElem = cell;
                                }

                                // make a vertical gap between Top & Body
                                if (!isTop && rIdx == 0 && currElem != null)
                                    currElem.Margin = new AnyUiThickness(0, 10, 0, 0);
                            }
                    }

                    // Row 5 : placeholders helping information

                    helper.AddSmallLabelTo(g, 5, 0, content: "Placeholders:",
                            verticalAlignment: AnyUiVerticalAlignment.Top,
                            verticalContentAlignment: AnyUiVerticalAlignment.Top);

                    helper.Set(
                        helper.AddSmallLabelTo(g, 5, 1,
                            margin: new AnyUiThickness(0, 2, 2, 2),
                            content: ImportExportPlaceholders.GetHelp(),
                            verticalAlignment: AnyUiVerticalAlignment.Top,
                            verticalContentAlignment: AnyUiVerticalAlignment.Top,
                            fontSize: 0.7,
                            wrapping: AnyUiTextWrapping.Wrap),
                        minHeight: 100,
                        horizontalAlignment: AnyUiHorizontalAlignment.Stretch);

                    // give back
                    return panel;
                });

            if (!ticket.ScriptMode)
            {
                // do the dialogue
                if (!(await displayContext.StartFlyoverModalAsync(uc)))
                    return;

                // stop
                await Task.Delay(2000);
            }

            // dome open/ save dialog base data
            var dlgFileName = "";
            var dlgFilter = "";

            if (record.Format == (int)ImportExportTableRecord.FormatEnum.TSF)
            {
                dlgFileName = "new.txt";
                dlgFilter =
                    "Tab separated file (*.txt)|*.txt|Tab separated file (*.tsf)|*.tsf|All files (*.*)|*.*";
            }
            if (record.Format == (int)ImportExportTableRecord.FormatEnum.LaTex)
            {
                dlgFileName = "new.tex";
                dlgFilter = "LaTex file (*.tex)|*.tex|All files (*.*)|*.*";
            }
            if (record.Format == (int)ImportExportTableRecord.FormatEnum.Excel)
            {
                dlgFileName = "new.xlsx";
                dlgFilter = "Microsoft Excel (*.xlsx)|*.xlsx|All files (*.*)|*.*";
            }
            if (record.Format == (int)ImportExportTableRecord.FormatEnum.Word)
            {
                dlgFileName = "new.docx";
                dlgFilter = "Microsoft Word (*.docx)|*.docx|All files (*.*)|*.*";
            }
            if (record.Format == (int)ImportExportTableRecord.FormatEnum.MarkdownGH)
            {
                dlgFileName = "new.md";
                dlgFilter = "Markdown (*.md)|*.md|All files (*.*)|*.*";
            }
            if (record.Format == (int)ImportExportTableRecord.FormatEnum.AsciiDoc)
            {
                dlgFileName = "new.adoc";
                dlgFilter = "AsciiDic (*.adoc)|*.adoc|All files (*.*)|*.*";
            }

            // ask for filename?
            if (!doImport)
            {
                // Export
                if (!(await displayContext.MenuSelectSaveFilenameToTicketAsync(
                            ticket, "File",
                            "Select file to be exported",
                            dlgFileName,
                            dlgFilter,
                            "Export table: No valid filename.",
                            argLocation: "Location",
                            reworkSpecialFn: true)))
                    return;
            }
            else
            {
                if (!(await displayContext.MenuSelectOpenFilenameToTicketAsync(
                            ticket, "File",
                            "Select file to be imported ..",
                            dlgFileName,
                            dlgFilter,
                            "Import table: No valid filename.")))
                    return;
            }

            var fn = ticket["File"] as string;

            // the Submodel elements need to have parents
            var sm = ticket.Submodel;
            sm.SetAllParents();

            if (!doImport)
            {
                // Export
                Export(options, record, fn, sm, ticket?.Env, ticket, log);

                // persist
                await displayContext.CheckIfDownloadAndStart(log, ticket["Location"], fn);

                // done
                log.Info($"Exporting table data to table {fn} finished.");
            }
            else
            {
                // Import
                Import(options, record, fn, sm, ticket?.Env, ticket, log);
                log.Info($"Importing table data from table {fn} finished.");
            }

        }

        private static void ExportTable_EnumerateSubmodel(
            List<ExportTableAasEntitiesList> list, Aas.Environment env,
            bool broadSearch, bool actInHierarchy, int depth,
            Aas.IReferable coll,
            int maxDepth)
        {
            // check
            if (list == null || env == null || coll == null)
                return;

            // prepare listItem
            ExportTableAasEntitiesList listItem = null;
            if (!actInHierarchy)
            {
                // add everything in one list
                if (list.Count < 1)
                    list.Add(new ExportTableAasEntitiesList());
                listItem = list[0];
            }
            else
            {
                // create a new list for each recursion
                listItem = new ExportTableAasEntitiesList();
                list.Add(listItem);
            }

            // pass 1: process value
            if (coll != null)
                foreach (var ci in coll.EnumerateChildren())
                {
                    // gather data for this entity
                    var sme2 = ci;
                    var cd = env.FindConceptDescriptionByReference(sme2?.SemanticId);

                    // add
                    listItem.Add(new ExportTableAasEntitiesItem(depth, sme2, cd,
                        parent: coll as Aas.IReferable));

                    // go directly deeper?
                    if (!broadSearch && ci != null &&
                        ci is Aas.IReferable
                        && depth < maxDepth)
                        ExportTable_EnumerateSubmodel(
                            list, env, broadSearch: false, actInHierarchy,
                            depth: 1 + depth, ci, maxDepth);
                }

            // pass 2: go for recursion AFTER?
            if (broadSearch)
            {
                if (coll != null)
                    foreach (var ci in coll.EnumerateChildren())
                        if (ci != null && ci is Aas.IReferable
                            && depth < maxDepth)
                            ExportTable_EnumerateSubmodel(
                                list, env, broadSearch: true, actInHierarchy,
                                depth: 1 + depth, ci, maxDepth);
            }
        }

        public static void Export(
            ExportTableOptions options,
            AasxPluginExportTable.ImportExportTableRecord record,
            string fn,
            Aas.IReferable rf, Aas.Environment env,
            AasxMenuActionTicket ticket = null,
            LogInstance log = null,
            int maxDepth = int.MaxValue)
        {
            // prepare list of items to be exported
            var list = new List<ExportTableAasEntitiesList>();
            ExportTable_EnumerateSubmodel(list, env, broadSearch: false,
                actInHierarchy: record.ActInHierarchy, depth: 1, rf, maxDepth);

            if (fn == null)
                return;

            // filter list for empty lists
            list = list.Where((li) => li != null && li.Count > 0).ToList();

            // iterate
            try
            {
                log.Info("Exporting table: {0}", fn);
                var success = false;
                try
                {
                    var proc = new ExportTableProcessor(record);
                    if (record.Format == (int)ImportExportTableRecord.FormatEnum.TSF)
                        success = proc.ExportTabSeparated(fn, list);
                    if (record.Format == (int)ImportExportTableRecord.FormatEnum.LaTex)
                        success = proc.ExportLaTex(fn, list);
                    if (record.Format == (int)ImportExportTableRecord.FormatEnum.Excel)
                        success = proc.ExportExcel(fn, list);
                    if (record.Format == (int)ImportExportTableRecord.FormatEnum.Word)
                        success = proc.ExportWord(fn, list);
                    if (record.Format == (int)ImportExportTableRecord.FormatEnum.MarkdownGH)
                        success = proc.ExportMarkdownGithub(fn, list);
                    if (record.Format == (int)ImportExportTableRecord.FormatEnum.AsciiDoc)
                        success = proc.ExportAsciiDoc(fn, list);
                }
                catch (Exception ex)
                {
                    log?.Error(ex, "performing data format export");
                    success = false;
                }

                if (!success && ticket?.ScriptMode != true)
                    log?.Error(
                        "Export table: Some error occured while exporting the table. " +
                        "Please refer to the log messages.");
            }
            catch (Exception ex)
            {
                log?.Error(ex, "When exporting table, an error occurred");
            }
        }

        private static void Import(
            ExportTableOptions options,
            AasxPluginExportTable.ImportExportTableRecord record,
            string fn,
            Aas.ISubmodel sm, Aas.Environment env,
            AasxMenuActionTicket ticket = null,
            LogInstance log = null)
        {
            // get the import file
            if (fn == null)
                return;

            // try import
            try
            {
                log.Info("Importing table: {0}", fn);
                var success = false;
                try
                {
                    if (record.Format == (int)ImportExportTableRecord.FormatEnum.Word)
                    {
                        success = true;
                        var pop = new ImportPopulateByTable(log, record, sm, env, options);
                        using (var stream = System.IO.File.Open(fn, FileMode.Open,
                                    FileAccess.Read, FileShare.ReadWrite))
                            foreach (var tp in ImportTableWordProvider.CreateProviders(stream))
                                pop.PopulateBy(tp);
                    }

                    if (record.Format == (int)ImportExportTableRecord.FormatEnum.Excel)
                    {
                        success = true;
                        var pop = new ImportPopulateByTable(log, record, sm, env, options);
                        foreach (var tp in ImportTableExcelProvider.CreateProviders(fn))
                            pop.PopulateBy(tp);
                    }
                }
                catch (Exception ex)
                {
                    log?.Error(ex, "importing table");
                    success = false;
                }

                if (!success && ticket?.ScriptMode != true)
                    log?.Error(
                        "Table import: Some error occured while importing the table. " +
                        "Please refer to the log messages.");
            }
            catch (Exception ex)
            {
                log?.Error(ex, "When exporting table, an error occurred");
            }

        }

    }
}
