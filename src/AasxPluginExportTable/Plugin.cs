using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using AasxPluginExportTable;
using AdminShellNS;
using JetBrains.Annotations;

/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

The Newtonsoft.JSON serialization is licensed under the MIT License (MIT).

The Microsoft Microsoft Automatic Graph Layout, MSAGL, is licensed under the MIT license (MIT).
*/

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    [UsedImplicitlyAttribute]
    // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    public class AasxPlugin : IAasxPluginInterface
    {
        public LogInstance Log = new LogInstance();
        private PluginEventStack eventStack = new PluginEventStack();
        private AasxPluginExportTable.ExportTableOptions options = new AasxPluginExportTable.ExportTableOptions();

        public string GetPluginName()
        {
            return "AasxPluginExportTable";
        }

        public void InitPlugin(string[] args)
        {
            // start ..
            Log.Info("InitPlugin() called with args = {0}", (args == null) ? "" : string.Join(", ", args));

            // .. with built-in options
            options = AasxPluginExportTable.ExportTableOptions.CreateDefault();

            // try load defaults options from assy directory
            try
            {
                var newOpt =
                    AasxPluginOptionsBase.LoadDefaultOptionsFromAssemblyDir<AasxPluginExportTable.ExportTableOptions>(
                        this.GetPluginName(), Assembly.GetExecutingAssembly());
                if (newOpt != null)
                    this.options = newOpt;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception when reading default options {1}");
            }
        }

        public object CheckForLogMessage()
        {
            return Log.PopLastShortTermPrint();
        }

        public AasxPluginActionDescriptionBase[] ListActions()
        {
            Log.Info("ListActions() called");
            var res = new List<AasxPluginActionDescriptionBase>();
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "set-json-options", "Sets plugin-options according to provided JSON string."));
            res.Add(new AasxPluginActionDescriptionBase("get-json-options", "Gets plugin-options as a JSON string."));
            res.Add(new AasxPluginActionDescriptionBase("get-licenses", "Reports about used licenses."));
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "get-events", "Pops and returns the earliest event from the event stack."));
            res.Add(new AasxPluginActionDescriptionBase("export-submodel", "Exports a Submodel."));
            return res.ToArray();
        }

        public AasxPluginResultBase ActivateAction(string action, params object[] args)
        {
            if (action == "set-json-options" && args != null && args.Length >= 1 && args[0] is string)
            {
                var newOpt = Newtonsoft.Json.JsonConvert.DeserializeObject<AasxPluginExportTable.ExportTableOptions>(
                    (args[0] as string));
                if (newOpt != null)
                    this.options = newOpt;
            }

            if (action == "get-json-options")
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(
                    this.options, Newtonsoft.Json.Formatting.Indented);
                return new AasxPluginResultBaseObject("OK", json);
            }

            if (action == "get-licenses")
            {
                var lic = new AasxPluginResultLicense();
                lic.shortLicense = "The OpenXML SDK is under MIT license." + Environment.NewLine +
                    "The ClosedXML library is under MIT license.";

                lic.isStandardLicense = true;
                lic.longLicense = AasxPluginHelper.LoadLicenseTxtFromAssemblyDir(
                    "LICENSE.txt", Assembly.GetExecutingAssembly());

                return lic;
            }

            if (action == "get-events" && this.eventStack != null)
            {
                // try access
                return this.eventStack.PopEvent();
            }

            if (action == "export-submodel" && args != null && args.Length >= 3 &&
                args[0] is IFlyoutProvider && args[1] is AdminShell.AdministrationShellEnv &&
                args[2] is AdminShell.Submodel)
            {
                // flyout provider
                var fop = args[0] as IFlyoutProvider;

                // which Submodel
                var env = args[1] as AdminShell.AdministrationShellEnv;
                var sm = args[2] as AdminShell.Submodel;
                if (env == null || sm == null)
                    return null;

                // the Submodel elements need to have parents
                sm.SetAllParents();

                // prepare list of items to be exported
                var list = new ExportTableAasEntitiesList();
                ExportTable_EnumerateSubmodel(list, env, broadSearch: false, depth: 1, sm: sm, sme: null);

                // handle the export dialogue
                var uc = new ExportTableFlyout();
                uc.Presets = this.options.Presets;
                fop?.StartFlyoverModal(uc);
                if (uc.Result == null)
                    return null;
                var job = uc.Result;

                // get the output file
                var dlg = new Microsoft.Win32.SaveFileDialog();
                // ReSharper disable EmptyGeneralCatchClause
                try
                {
                    dlg.InitialDirectory = System.IO.Path.GetDirectoryName(
                        System.AppDomain.CurrentDomain.BaseDirectory);
                }
                catch { }
                // ReSharper enable EmptyGeneralCatchClause
                dlg.Title = "Select text file to be exported";

                if (job.Format == (int)ExportTableRecord.FormatEnum.TSF)
                {
                    dlg.FileName = "new.txt";
                    dlg.DefaultExt = "*.txt";
                    dlg.Filter =
                        "Tab separated file (*.txt)|*.txt|Tab separated file (*.tsf)|*.tsf|All files (*.*)|*.*";
                }
                if (job.Format == (int)ExportTableRecord.FormatEnum.LaTex)
                {
                    dlg.FileName = "new.tex";
                    dlg.DefaultExt = "*.tex";
                    dlg.Filter = "LaTex file (*.tex)|*.tex|All files (*.*)|*.*";
                }
                if (job.Format == (int)ExportTableRecord.FormatEnum.Excel)
                {
                    dlg.FileName = "new.xlsx";
                    dlg.DefaultExt = "*.xlsx";
                    dlg.Filter = "Microsoft Excel (*.xlsx)|*.xlsx|All files (*.*)|*.*";
                }
                if (job.Format == (int)ExportTableRecord.FormatEnum.Word)
                {
                    dlg.FileName = "new.docx";
                    dlg.DefaultExt = "*.docx";
                    dlg.Filter = "Microsoft Word (*.docx)|*.docx|All files (*.*)|*.*";
                }

                fop?.StartFlyover(new EmptyFlyout());
                var res = dlg.ShowDialog(fop?.GetWin32Window());

                try
                {
                    if (res == true)
                    {
                        Log.Info("Exporting table: {0}", dlg.FileName);
                        var success = false;
                        try
                        {
                            if (job.Format == (int)ExportTableRecord.FormatEnum.TSF)
                                success = job.ExportTabSeparated(dlg.FileName, list);
                            if (job.Format == (int)ExportTableRecord.FormatEnum.LaTex)
                                success = job.ExportLaTex(dlg.FileName, list);
                            if (job.Format == (int)ExportTableRecord.FormatEnum.Excel)
                                success = job.ExportExcel(dlg.FileName, list);
                            if (job.Format == (int)ExportTableRecord.FormatEnum.Word)
                                success = job.ExportWord(dlg.FileName, list);
                        }
                        catch
                        {
                            success = false;
                        }

                        if (!success)
                            fop?.MessageBoxFlyoutShow(
                                "Some error occured while exporting the table. Please refer to the log messages.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "When exporting table, an error occurred");
                }

                fop.CloseFlyover();
            }

            // default
            return null;
        }

        private void ExportTable_EnumerateSubmodel(
            ExportTableAasEntitiesList list, AdminShell.AdministrationShellEnv env,
            bool broadSearch, int depth,
            AdminShell.Submodel sm, AdminShell.SubmodelElement sme)
        {
            // check
            if (list == null || env == null || sm == null)
                return;

            //
            // Submodel or SME ??
            //

            AdminShell.IEnumerateChildren coll = null;
            if (sme == null)
            {

                // yield SM
                list.Add(new ExportTableAasEntitiesItem(depth, sm: sm));

                // use collection
                coll = sm;
            }
            else
            {
                // simple check for SME collection
                if (sme is AdminShell.IEnumerateChildren)
                    coll = (sme as AdminShell.IEnumerateChildren);
            }

            // pass 1: process value
            if (coll != null)
                foreach (var ci in coll.EnumerateChildren())
                {
                    // gather data for this entity
                    var sme2 = ci.submodelElement;
                    var cd = env.FindConceptDescription(sme2?.semanticId?.Keys);
                    list.Add(new ExportTableAasEntitiesItem(depth, sm, sme2, cd));

                    // go directly deeper?
                    if (!broadSearch && ci.submodelElement != null &&
                        ci.submodelElement is AdminShell.IEnumerateChildren)
                        ExportTable_EnumerateSubmodel(
                            list, env, broadSearch: false, depth: 1 + depth, sm: sm, sme: ci.submodelElement);
                }

            // pass 2: go for recursion AFTER?
            if (broadSearch)
            {
                if (coll != null)
                    foreach (var ci in coll.EnumerateChildren())
                        if (ci.submodelElement != null && ci.submodelElement is AdminShell.IEnumerateChildren)
                            ExportTable_EnumerateSubmodel(
                                list, env, broadSearch: true, depth: 1 + depth, sm: sm, sme: ci.submodelElement);
            }
        }

    }
}
