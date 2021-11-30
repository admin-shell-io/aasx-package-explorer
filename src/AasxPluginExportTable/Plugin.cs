/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

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
using AnyUi;
using JetBrains.Annotations;

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
            res.Add(new AasxPluginActionDescriptionBase("import-submodel", "Imports a Submodel."));
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
                    "The ClosedXML library is under MIT license." + Environment.NewLine +
                    "The ExcelNumberFormat number parser is licensed under the MIT license." + Environment.NewLine +
                    "The FastMember reflection access is licensed under Apache License 2.0 (Apache - 2.0).";

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

            if (( action == "export-submodel" || action == "import-submodel" )
                && args != null && args.Length >= 3 &&
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

                // handle the export dialogue
                var uc = new ExportTableFlyout( (action == "export-submodel") 
                    ? "Export SubmodelElements as Table"
                    : "Import SubmodelElements from Table");
                uc.Presets = this.options.Presets;
                fop?.StartFlyoverModal(uc);
                fop?.CloseFlyover();
                if (uc.Result == null)
                    return null;

                if (action == "export-submodel")
                    Export(uc.Result, fop, sm, env);

                if (action == "import-submodel")
                    Import(uc.Result, fop, sm, env);
            }

            // default
            return null;
        }

        private void ExportTable_EnumerateSubmodel(
            List<ExportTableAasEntitiesList> list, AdminShell.AdministrationShellEnv env,
            bool broadSearch, bool actInHierarchy, int depth,
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
                // MIHO 21-11-24: IMHO this makes no sense
                // list.Add(new ExportTableAasEntitiesItem(depth, sm: sm, parentSm: sm));

                // use collection
                coll = sm;
            }
            else
            {
                // simple check for SME collection
                if (sme is AdminShell.IEnumerateChildren)
                    coll = (sme as AdminShell.IEnumerateChildren);
            }

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
                    var sme2 = ci.submodelElement;
                    var cd = env.FindConceptDescription(sme2?.semanticId?.Keys);

                    // add
                    listItem.Add(new ExportTableAasEntitiesItem(depth, sm, sme2, cd, parent: coll as AdminShell.Referable));

                    // go directly deeper?
                    if (!broadSearch && ci.submodelElement != null &&
                        ci.submodelElement is AdminShell.IEnumerateChildren)
                        ExportTable_EnumerateSubmodel(
                            list, env, broadSearch: false, actInHierarchy, 
                            depth: 1 + depth, sm: sm, sme: ci.submodelElement);
                }

            // pass 2: go for recursion AFTER?
            if (broadSearch)
            {
                if (coll != null)
                    foreach (var ci in coll.EnumerateChildren())
                        if (ci.submodelElement != null && ci.submodelElement is AdminShell.IEnumerateChildren)
                            ExportTable_EnumerateSubmodel(
                                list, env, broadSearch: true, actInHierarchy, 
                                depth: 1 + depth, sm: sm, sme: ci.submodelElement);
            }
        }

        private void Export(ExportTableRecord job,
            IFlyoutProvider fop,
            AdminShell.Submodel sm, AdminShell.AdministrationShellEnv env)
        {
            // prepare list of items to be exported
            var list = new List<ExportTableAasEntitiesList>();
            ExportTable_EnumerateSubmodel(list, env, broadSearch: false, 
                actInHierarchy: job.ActInHierarchy, depth: 1, sm: sm, sme: null);

            // get the output file
            var dlg = new Microsoft.Win32.SaveFileDialog();
            try
            {
                dlg.InitialDirectory = System.IO.Path.GetDirectoryName(
                    System.AppDomain.CurrentDomain.BaseDirectory);
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            }
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
            fop?.CloseFlyover();

            try
            {
                if (res == true)
                {
                    Log.Info("Exporting table: {0}", dlg.FileName);
                    var success = false;
                    try
                    {
                        // TODO: change list[0]!!

                        if (job.Format == (int)ExportTableRecord.FormatEnum.TSF)
                            success = job.ExportTabSeparated(dlg.FileName, list);
                        if (job.Format == (int)ExportTableRecord.FormatEnum.LaTex)
                            success = job.ExportLaTex(dlg.FileName, list[0]);
                        if (job.Format == (int)ExportTableRecord.FormatEnum.Excel)
                            success = job.ExportExcel(dlg.FileName, list);
                        if (job.Format == (int)ExportTableRecord.FormatEnum.Word)
                            success = job.ExportWord(dlg.FileName, list);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "performing data format export");
                        success = false;
                    }

                    if (!success)
                        fop?.MessageBoxFlyoutShow(
                            "Some error occured while exporting the table. Please refer to the log messages.",
                            "Error", AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Exclamation);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "When exporting table, an error occurred");
            }

        }

        private void Import(ExportTableRecord job,
            IFlyoutProvider fop,
            AdminShell.Submodel sm, AdminShell.AdministrationShellEnv env)
        {
            // get the import file
            var dlg = new Microsoft.Win32.OpenFileDialog();
            try
            {
                dlg.InitialDirectory = System.IO.Path.GetDirectoryName(
                    System.AppDomain.CurrentDomain.BaseDirectory);
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            }
            dlg.Title = "Select text file to be exported";

            if (job.Format == (int)ExportTableRecord.FormatEnum.TSF)
            {
                dlg.DefaultExt = "*.txt";
                dlg.Filter =
                    "Tab separated file (*.txt)|*.txt|Tab separated file (*.tsf)|*.tsf|All files (*.*)|*.*";
            }
            if (job.Format == (int)ExportTableRecord.FormatEnum.LaTex)
            {
                dlg.DefaultExt = "*.tex";
                dlg.Filter = "LaTex file (*.tex)|*.tex|All files (*.*)|*.*";
            }
            if (job.Format == (int)ExportTableRecord.FormatEnum.Excel)
            {
                dlg.DefaultExt = "*.xlsx";
                dlg.Filter = "Microsoft Excel (*.xlsx)|*.xlsx|All files (*.*)|*.*";
            }
            if (job.Format == (int)ExportTableRecord.FormatEnum.Word)
            {
                dlg.DefaultExt = "*.docx";
                dlg.Filter = "Microsoft Word (*.docx)|*.docx|All files (*.*)|*.*";
            }

            fop?.StartFlyover(new EmptyFlyout());
            var res = dlg.ShowDialog(fop?.GetWin32Window());
            fop?.CloseFlyover();

            if (true != res)
                return;

            // try import
            try
            {
                Log.Info("Importing table: {0}", dlg.FileName);
                var success = false;
                try
                {
                    if (job.Format == (int)ExportTableRecord.FormatEnum.Word)
                    {
                        success = true;
                        var pop = new ImportPopulateByTable(Log, job, sm, env, options);
                        foreach (var tp in ImportTableWordProvider.CreateProviders(dlg.FileName))
                            pop.PopulateBy(tp);
                    }

                    if (job.Format == (int)ExportTableRecord.FormatEnum.Excel)
                    {
                        success = true;
                        var pop = new ImportPopulateByTable(Log, job, sm, env, options);
                        foreach (var tp in ImportTableExcelProvider.CreateProviders(dlg.FileName))
                            pop.PopulateBy(tp);
                    }
                }
                catch (Exception ex)
                {
                    Log?.Error(ex, "importing table");
                    success = false;
                }

                if (!success)
                    fop?.MessageBoxFlyoutShow(
                        "Some error occured while importing the table. Please refer to the log messages.",
                        "Error", AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Exclamation);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "When exporting table, an error occurred");
            }

        }
    }
}
