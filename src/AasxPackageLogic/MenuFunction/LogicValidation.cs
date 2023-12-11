/*
Copyright (c) 2019 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

Copyright (c) 2019 Phoenix Contact GmbH & Co. KG <>
Author: Andreas Orzelski

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxPackageLogic;
using AnyUi;
using Extensions;
using Jose;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.Webpki.JsonCanonicalizer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using Aas = AasCore.Aas3_0;

namespace AasxPackageExplorer
{
    /// <summary>
    /// This is a record for validations based on business logic descriptions.
    /// </summary>
    public class LogicValidationRecBase
    {
        /// <summary>
        /// Translates the record to a (character-width) formatted text line
        /// </summary>
        public virtual string ToTextLine()
        {
            return "";
        }

        /// <summary>
        /// Translates the record to a table row with dedicated columns
        /// </summary>
        public virtual AasxPluginExportTableInterop.InteropRow ToTableRow()
        {
            return new AasxPluginExportTableInterop.InteropRow();
        }
    }

    /// <summary>
    /// Textual comment when validating some business logic
    /// </summary>
    public class LogicValidationRecComment : LogicValidationRecBase
    {
        /// <summary>
        /// Any comment
        /// </summary>
        public string Text = "";

        public override string ToTextLine()
        {
            return "# " + Text;
        }

        public override AasxPluginExportTableInterop.InteropRow ToTableRow()
        {
            return new AasxPluginExportTableInterop.InteropRow("# " + Text).Set(wrap: false);
        }
    }

    /// <summary>
    /// Line of (tabbed) cells with text contents
    /// </summary>
    public class LogicValidationRecTabbedCells : LogicValidationRecBase
    {
        /// <summary>
        /// Any comment
        /// </summary>
        public string[] Cells = null;

        public override string ToTextLine()
        {
            if (Cells == null)
                return "";
            return "" + string.Join('\t', Cells);
        }

        public override AasxPluginExportTableInterop.InteropRow ToTableRow()
        {
            if (Cells == null)
                return new AasxPluginExportTableInterop.InteropRow();

            return new AasxPluginExportTableInterop.InteropRow(Cells).Set(wrap: false);
        }
    }
    /// <summary>
    /// Conclusive or summary statement when validating some business logic
    /// </summary>
    public class LogicValidationRecStatement : LogicValidationRecBase
    {
        /// <summary>
        /// Id (<6 chars) to correlate with report, form, visual
        /// </summary>
        public string Id = "";

        /// <summary>
        /// Textual flag of the outcome (<10 chars)
        /// </summary>
        public string OutcomeText = "";

        /// <summary>
        /// True, if outcome means a failing of the report
        /// </summary>
        public bool OutcomeFail = false;

        /// <summary>
        /// Verbal report of the statement
        /// </summary>
        public string Text = "";

        public override string ToTextLine()
        {
            return String.Format("{0,-6} {1,-10} {2}", Id, OutcomeText, Text);
        }

        public override AasxPluginExportTableInterop.InteropRow ToTableRow()
        {
            return new AasxPluginExportTableInterop.InteropRow(Id, OutcomeText, Text).Set(bold: true);
        }
    }

    /// <summary>
    /// statement when validating some business logic, which is based on a specific
    /// AAS element
    /// </summary>
    public class LogicValidationRecElementDetail : LogicValidationRecStatement
    {
        public Aas.IReference Reference = null;

        public override string ToTextLine()
        {
            var refText = "-";
            if (Reference != null)
                refText = Reference.ToStringExtended(2);
            return String.Format("{0,-6} {1,-10} {2} Reference={3}", Id, OutcomeText, Text, refText);
        }

        public override AasxPluginExportTableInterop.InteropRow ToTableRow()
        {
            var refText = "-";
            if (Reference != null)
                refText = Reference.ToStringExtended(2);
            return new AasxPluginExportTableInterop.InteropRow(Id, OutcomeText, Text, refText)
                .Set(bold: true);
        }
    }

    /// <summary>
    /// Statement, that some specific value between two "versions" 1/2 of 
    /// an element are different
    /// </summary>
    public class LogicValidationRecDifference : LogicValidationRecStatement
    {
        public string Value1 = "";
        public string Value2 = "";

        public override string ToTextLine()
        {
            return String.Format("{0,-6} {1,-10} {2} Difference 1={3} 2={4}",
                Id, OutcomeText, Text, Value1, Value2);
        }

        public override AasxPluginExportTableInterop.InteropRow ToTableRow()
        {
            return new AasxPluginExportTableInterop.InteropRow(Id, OutcomeText, Text, Value1, Value2)
                .Set(bold: true);
        }
    }

    /// <summary>
    /// Holds a list of logic validation records
    /// </summary>
    public class LogicValidationRecordList : List<LogicValidationRecBase>
    {
        public void AddComment(string text)
        {
            this.Add(new LogicValidationRecComment() { Text = text });
        }

        public void AddTabbedCells(params string[] cells)
        {
            this.Add(new LogicValidationRecTabbedCells() { Cells = cells });
        }

        public void AddStatement(string id, string outcome, bool isFail, string text)
        {
            this.Add(new LogicValidationRecStatement()
            {
                Id = id,
                OutcomeText = outcome,
                OutcomeFail = isFail,
                Text = text
            });
        }

        public void AddElemDetail(string id, string outcome, bool isFail, string text, Aas.IReference rf)
        {
            this.Add(new LogicValidationRecElementDetail()
            {
                Id = id,
                OutcomeText = outcome,
                OutcomeFail = isFail,
                Text = text,
                Reference = rf
            });
        }

        public void AddDifference(string id, string outcome, bool isFail, string text, string value1, string value2)
        {
            this.Add(new LogicValidationRecDifference()
            {
                Id = id,
                OutcomeText = outcome,
                OutcomeFail = isFail,
                Text = text,
                Value1 = value1,
                Value2 = value2
            });
        }

        public string ToText()
        {
            var sb = new StringBuilder();
            foreach (var rec in this)
                sb.AppendLine(rec.ToTextLine());
            return sb.ToString();
        }

        public AasxPluginExportTableInterop.InteropTable ToTable()
        {
            var res = new AasxPluginExportTableInterop.InteropTable();
            foreach (var rec in this)
                res.Rows.Add(rec.ToTableRow());
            return res;
        }

        public bool IsAnyFailForId(string id)
        {
            var allFail = false;
            foreach (var r in this)
                if (r is LogicValidationRecStatement state
                    && state.Id.Trim().Equals(id.Trim(), StringComparison.InvariantCultureIgnoreCase)
                    && state.OutcomeFail)
                    allFail = true;
            return allFail;
        }

        public void AddAnyFailStatement(string id, string text)
        {
            var anyFail = IsAnyFailForId(id);
            if (anyFail)
                AddStatement(id, "FAIL", true, text);
            else
                AddStatement(id, "PASS", false, text);
        }
    }

    /// <summary>
    /// Tools to valdiate a Submodel template (incl. UI menu function)
    /// </summary>
    public class LogicValidationMenuFuncBase
    {
        /// <summary>
        /// Generated report
        /// </summary>
        public LogicValidationRecordList Recs = new LogicValidationRecordList();

        //
        // Outer UI
        //

        protected bool WriteTargetFile(int fmt, string targetFn)
        {
            if (fmt == 0)
            {
                // try call export
                try
                {
                    // pretty easy
                    System.IO.File.WriteAllText(targetFn,
                        Recs.ToText());

                    // state success
                    Log.Singleton.Info("Result file written to: " + targetFn);
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, "Generate SMT assessment Text report to: " + targetFn);
                    return false;
                }

                // ok
                return true;
            }

            if (fmt == 1)
            {
                // try call export
                try
                {
                    // find table export plug-in
                    var pi = Plugins.FindPluginInstance("AasxPluginExportTable");
                    if (pi == null || !pi.HasAction("interop-export"))
                    {
                        Log.Singleton.Error(
                            "No plug-in 'AasxPluginExportTable' with appropriate " +
                            "action 'interop-export()' found.");
                        return false;
                    }

                    // create client
                    // ReSharper disable ConditionIsAlwaysTrueOrFalse
                    var resClient =
                        pi.InvokeAction(
                            "interop-export", "excel", targetFn, Recs.ToTable())
                        as AasxPluginResultBaseObject;
                    // ReSharper enable ConditionIsAlwaysTrueOrFalse
                    if (resClient == null || ((bool)resClient.obj) != true)
                    {
                        Log.Singleton.Error(
                            "Plug-in 'AasxPluginExportTable' cannot export!");
                        return false;
                    }

                    // state success
                    Log.Singleton.Info("Result file written to: " + targetFn);
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, "Generate SMT assessment Excel report to: " + targetFn);
                    return false;
                }

                // ok
                return true;
            }

            return false;
        }

        public async Task PerformDialogue(
            AasxMenuActionTicket ticket,
            AnyUiContextBase displayContext,
            string caption)
        {
            // if a target file is given, a headless operation occurs
            if (ticket != null && ticket["Target"] is string targetFn)
            {
                var exportFmt = -1;
                var targExt = System.IO.Path.GetExtension(targetFn).ToLower();
                if (targExt == ".txt")
                    exportFmt = 0;
                if (targExt == ".xlsx")
                    exportFmt = 1;
                if (exportFmt < 0)
                {
                    MainWindowLogic.LogErrorToTicketStatic(ticket, null,
                        $"For operation '{caption}', the target format could not be " +
                        $"determined by filename '{targetFn}'. Aborting.");
                    return;
                }

                try
                {
                    WriteTargetFile(exportFmt, targetFn);
                }
                catch (Exception ex)
                {
                    MainWindowLogic.LogErrorToTicketStatic(ticket, ex,
                        $"While performing '{caption}'");
                    return;
                }

                // ok
                Log.Singleton.Info("Performed '{0}' and writing report to '{1}'.",
                    caption, targetFn);
                return;
            }

            // reserve some states for the inner viewing routine
            bool wrap = false;

            // ok, go on ..
            var uc = new AnyUiDialogueDataModalPanel(caption);
            uc.ActivateRenderPanel(this,
                disableScrollArea: true,
                dialogButtons: AnyUiMessageBoxButton.OK,
                extraButtons: new[] { "Save as text report ..", "Save as Excel report .." },
                renderPanel: (uci) =>
                {
                    // create grid (no panel!!)
                    var helper = new AnyUiSmallWidgetToolkit();
                    var g = helper.AddSmallGrid(4, 2, new[] { "100:", "*" },
                                padding: new AnyUiThickness(0, 5, 0, 5));
                    g.RowDefinitions[0].Height = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);

                    // Row 0 : report itself
                    helper.AddSmallLabelTo(g, 0, 0, content: "Report:",
                        verticalAlignment: AnyUiVerticalAlignment.Top,
                        verticalContentAlignment: AnyUiVerticalAlignment.Top);

                    // Output view
                    var tb = helper.AddSmallTextBoxTo(g, 0, 1);
                    tb.MultiLine = true;
                    tb.MaxLines = null;
                    tb.FontMono = true;
                    tb.TextWrapping = wrap ? AnyUiTextWrapping.Wrap : AnyUiTextWrapping.NoWrap;
                    tb.IsReadOnly = true;
                    tb.FontSize = 0.8f;

                    if (displayContext is AnyUiContextPlusDialogs dcpd
                        && dcpd.HasCapability(AnyUiContextCapability.Blazor))
                    {
                        // web browser needs a scrollable element
                        tb.MinHeight = 400;
                    }

                    // put report into it
                    tb.Text = Recs.ToText();

                    // Row 1 : some viewing options
                    helper.AddSmallLabelTo(g, 1, 0, content: "Utils:");

                    var utilsGrid = helper.AddSmallGridTo(g, 1, 1, 1, 4, new[] { "#", "#", "#", "#" });

                    AnyUiUIElement.RegisterControl(
                        helper.AddSmallCheckBoxTo(utilsGrid, 0, 0, content: "Wrap", isChecked: wrap,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center),
                        setValue: (o) =>
                        {
                            wrap = !wrap;
                            tb.TextWrapping = wrap ? AnyUiTextWrapping.Wrap : AnyUiTextWrapping.NoWrap;
                            return new AnyUiLambdaActionModalPanelReRender(uc);
                        });

                    // give back
                    return g;
                });

            if (!(await displayContext.StartFlyoverModalAsync(uc)))
                return;

            //
            // Generation of reports outside of the dialogue because
            // of web interface
            //

            if (displayContext is AnyUiContextPlusDialogs dcpd)
            {
                // for later use
                AnyUiDialogueDataSaveFile ucsf = null;
                int exportFmt = -1;

                // text?
                if (uc.ResultButton == AnyUiMessageBoxResult.Extra0)
                {
                    // ask for filename
                    ucsf = await dcpd.MenuSelectSaveFilenameAsync(
                        ticket: null, argName: null,
                        caption: "Select Text file to save ..",
                        proposeFn: "new.txt",
                        filter: "Text file (*.txt)|*.txt|All files (*.*)|*.*",
                        msg: "Not found",
                        reworkSpecialFn: true);
                    exportFmt = 0;
                }

                // excel
                if (uc.ResultButton == AnyUiMessageBoxResult.Extra1)
                {
                    // ask for filename
                    ucsf = await dcpd.MenuSelectSaveFilenameAsync(
                        ticket: null, argName: null,
                        caption: "Select Excel file to save ..",
                        proposeFn: "new.xlsx",
                        filter: "Excel file (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                        msg: "Not found",
                        reworkSpecialFn: true);
                    exportFmt = 1;
                }

                if (ucsf != null && exportFmt >= 0)
                {
                    if (ucsf?.Result != true)
                        return;

                    WriteTargetFile(exportFmt, ucsf.TargetFileName);

                    string fileWritten = null;
                    if (ucsf.Location == AnyUiDialogueDataSaveFile.LocationKind.Download
                        && dcpd.WebBrowserServicesAllowed())
                        fileWritten = ucsf.TargetFileName;

                    // if it is a download, provide link
                    if (fileWritten != null)
                    {
                        try
                        {
                            await dcpd.WebBrowserDisplayOrDownloadFile(
                                fileWritten, "application/octet-stream");
                            Log.Singleton.Info("Download initiated.");
                        }
                        catch (Exception ex)
                        {
                            Log.Singleton.Error(
                                ex, $"When downloading written file");
                            return;
                        }
                    }
                }
            }
        }
    }
}
