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
using AasxPluginExportTable.TimeSeries;
using AasxPluginExportTable.Uml;
using AnyUi;
using JetBrains.Annotations;
using Aas = AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    [UsedImplicitlyAttribute]
    // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    public class AasxPlugin : AasxPluginBase
    {
        private AasxPluginExportTable.ExportTableOptions _options = new AasxPluginExportTable.ExportTableOptions();

        static AasxPlugin()
        {
            PluginName = "AasxPluginExportTable";
        }

        public new void InitPlugin(string[] args)
        {
            // start ..
            _log.Info("InitPlugin() called with args = {0}", (args == null) ? "" : string.Join(", ", args));

            // .. with built-in options
            _options = AasxPluginExportTable.ExportTableOptions.CreateDefault();

            // try load defaults options from assy directory
            try
            {
                var newOpt =
                    AasxPluginOptionsBase.LoadDefaultOptionsFromAssemblyDir<AasxPluginExportTable.ExportTableOptions>(
                        this.GetPluginName(), Assembly.GetExecutingAssembly());
                if (newOpt != null)
                    this._options = newOpt;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception when reading default options {1}");
            }
        }

        public new AasxPluginActionDescriptionBase[] ListActions()
        {
            _log.Info("ListActions() called");
            var res = new List<AasxPluginActionDescriptionBase>();
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "set-json-options", "Sets plugin-options according to provided JSON string."));
            res.Add(new AasxPluginActionDescriptionBase(
                "get-json-options", "Gets plugin-options as a JSON string."));
            res.Add(new AasxPluginActionDescriptionBase("get-licenses", "Reports about used licenses."));
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "get-events", "Pops and returns the earliest event from the event stack."));
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "get-presets", "Provides options/ preset data of plugin to caller."));
            res.Add(new AasxPluginActionDescriptionBase("export-submodel", "Exports a Submodel."));
            res.Add(new AasxPluginActionDescriptionBase("import-submodel", "Imports a Submodel."));
            res.Add(new AasxPluginActionDescriptionBase("export-uml", "Exports a Submodel to an UML file."));
            res.Add(new AasxPluginActionDescriptionBase(
                "import-time-series", "Import time series data from a table file."));
            return res.ToArray();
        }

        public new AasxPluginResultBase ActivateAction(string action, params object[] args)
        {
            if (action == "set-json-options" && args != null && args.Length >= 1 && args[0] is string)
            {
                var newOpt = Newtonsoft.Json.JsonConvert.DeserializeObject<AasxPluginExportTable.ExportTableOptions>(
                    (args[0] as string));
                if (newOpt != null)
                    this._options = newOpt;
            }

            if (action == "get-json-options")
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(
                    this._options, Newtonsoft.Json.Formatting.Indented);
                return new AasxPluginResultBaseObject("OK", json);
            }

            if (action == "get-licenses")
            {
                var lic = new AasxPluginResultLicense();
                lic.shortLicense = "The OpenXML SDK is under MIT license." + System.Environment.NewLine +
                    "The ClosedXML library is under MIT license." + System.Environment.NewLine +
                    "The ExcelNumberFormat number parser is licensed under the MIT license." 
                    + System.Environment.NewLine +
                    "The FastMember reflection access is licensed under Apache License 2.0 (Apache - 2.0).";

                lic.isStandardLicense = true;
                lic.longLicense = AasxPluginHelper.LoadLicenseTxtFromAssemblyDir(
                    "LICENSE.txt", Assembly.GetExecutingAssembly());

                return lic;
            }

            if (action == "get-events" && _eventStack != null)
            {
                // try access
                return _eventStack.PopEvent();
            }

            if (action == "get-presets")
            {
                var presets = new object[]
                {
                    _options.Presets,
                    _options.UmlExport,
                    _options.TimeSeriesImport
                };
                return new AasxPluginResultBaseObject("presets", presets);
            }

            if ((action == "export-submodel" || action == "import-submodel")
                && args != null && args.Length >= 5)
            {
                if (args[0] is ImportExportTableRecord record
                    && args[1] is string fn
                    && args[2] is Aas.Environment env
                    && args[3] is Aas.Submodel sm
                    && args[4] is AasxMenuActionTicket ticket)
                {
                    // the Submodel elements need to have parents
                    sm.SetAllParents();

                    if (action == "export-submodel")
                        Export(record, fn, sm, env, ticket);

                    if (action == "import-submodel")
                        Import(record, fn, sm, env);
                }
            }

            if (action == "export-uml")
            {
                if (args != null && args.Length >= 4
                    && args[0] is ExportUmlRecord record
                    && args[1] is string fn
                    && args[2] is Aas.Environment env
                    && args[3] is Aas.Submodel sm)
                {
                    // the Submodel elements need to have parents
                    sm.SetAllParents();

                    // use functionality
                    ExportUml.ExportUmlToFile(env, sm, record, fn);
                    _log.Info($"Export UML data to file: {fn}");


                }
            }

            if (action == "import-time-series")
            {
                if (args != null && args.Length >= 4
                    && args[0] is ImportTimeSeriesRecord record
                    && args[1] is string fn
                    && args[1] is Aas.Environment env
                    && args[2] is Aas.Submodel sm)
                {
                    // the Submodel elements need to have parents
                    sm.SetAllParents();

                    // use functionality
                    _log.Info($"Importing time series from file: {fn} ..");
                    ImportTimeSeries.ImportTimeSeriesFromFile(env, sm, record, fn, _log);
                }
            }

            // default
            return null;
        }

        private void ExportTable_EnumerateSubmodel(
            List<ExportTableAasEntitiesList> list, Aas.Environment env,
            bool broadSearch, bool actInHierarchy, int depth,
            Aas.Submodel sm, Aas.ISubmodelElement sme)
        {
            // check
            if (list == null || env == null || sm == null)
                return;

            //
            // Submodel or SME ??
            //

            Aas.IReferable coll = null;
            if (sme == null)
            {
                // yield SM
                // MIHO 21-11-24: IMHO this makes no sense
                //// list.Add(new ExportTableAasEntitiesItem(depth, sm: sm, parentSm: sm));

                // use collection
                coll = sm;
            }
            else
            {
                // simple check for SME collection
                if (sme is Aas.IReferable)
                    coll = (sme as Aas.IReferable);
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
                    var sme2 = ci;
                    var cd = env.FindConceptDescriptionByReference(sme2?.SemanticId);

                    // add
                    listItem.Add(new ExportTableAasEntitiesItem(depth, sm, sme2, cd,
                        parent: coll as Aas.IReferable));

                    // go directly deeper?
                    if (!broadSearch && ci != null &&
                        ci is Aas.IReferable)
                        ExportTable_EnumerateSubmodel(
                            list, env, broadSearch: false, actInHierarchy,
                            depth: 1 + depth, sm: sm, sme: ci);
                }

            // pass 2: go for recursion AFTER?
            if (broadSearch)
            {
                if (coll != null)
                    foreach (var ci in coll.EnumerateChildren())
                        if (ci != null && ci is Aas.IReferable)
                            ExportTable_EnumerateSubmodel(
                                list, env, broadSearch: true, actInHierarchy,
                                depth: 1 + depth, sm: sm, sme: ci);
            }
        }

        private void Export(ImportExportTableRecord record,
            string fn,
            Aas.Submodel sm, Aas.Environment env,
            AasxMenuActionTicket ticket = null)
        {
            // prepare list of items to be exported
            var list = new List<ExportTableAasEntitiesList>();
            ExportTable_EnumerateSubmodel(list, env, broadSearch: false,
                actInHierarchy: record.ActInHierarchy, depth: 1, sm: sm, sme: null);

            if (fn == null)
                return;

            try
            {
                _log.Info("Exporting table: {0}", fn);
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
                    if (record.Format == (int)ImportExportTableRecord.FormatEnum.NarkdownGH)
                        success = proc.ExportMarkdownGithub(fn, list);
                }
                catch (Exception ex)
                {
                    _log?.Error(ex, "performing data format export");
                    success = false;
                }

                if (!success && ticket?.ScriptMode != true)
                    _log?.Error(
                        "Export table: Some error occured while exporting the table. " +
                        "Please refer to the log messages.");
            }
            catch (Exception ex)
            {
                _log?.Error(ex, "When exporting table, an error occurred");
            }

        }

        private void Import(ImportExportTableRecord record,
            string fn,
            Aas.Submodel sm, Aas.Environment env,
            AasxMenuActionTicket ticket = null)
        {
            // get the import file
            if (fn == null)
                return;

            // try import
            try
            {
                _log.Info("Importing table: {0}", fn);
                var success = false;
                try
                {
                    if (record.Format == (int)ImportExportTableRecord.FormatEnum.Word)
                    {
                        success = true;
                        var pop = new ImportPopulateByTable(_log, record, sm, env, _options);
                        using (var stream = System.IO.File.Open(fn, FileMode.Open,
                                    FileAccess.Read, FileShare.ReadWrite))
                            foreach (var tp in ImportTableWordProvider.CreateProviders(stream))
                                pop.PopulateBy(tp);
                    }

                    if (record.Format == (int)ImportExportTableRecord.FormatEnum.Excel)
                    {
                        success = true;
                        var pop = new ImportPopulateByTable(_log, record, sm, env, _options);
                        foreach (var tp in ImportTableExcelProvider.CreateProviders(fn))
                            pop.PopulateBy(tp);
                    }
                }
                catch (Exception ex)
                {
                    _log?.Error(ex, "importing table");
                    success = false;
                }

                if (!success && ticket?.ScriptMode != true)
                    _log?.Error(
                        "Table import: Some error occured while importing the table. " +
                        "Please refer to the log messages.");
            }
            catch (Exception ex)
            {
                _log?.Error(ex, "When exporting table, an error occurred");
            }

        }
    }
}
