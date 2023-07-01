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
using AasxPluginExportTable.TimeSeries;
using AasxPluginExportTable.Uml;
using AnyUi;
using JetBrains.Annotations;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using static AnyUi.AnyUiDialogueDataSaveFile;
using DocumentFormat.OpenXml.Drawing.Charts;
using AasxPluginExportTable.Table;
using AasxPluginExportTable;

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    [UsedImplicitlyAttribute]
    // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    public class AasxPlugin : AasxPluginBase
    {
        private ExportTableOptions _options = new AasxPluginExportTable.ExportTableOptions();

        public new void InitPlugin(string[] args)
        {
            // start ..
            PluginName = "AasxPluginExportTable";
            _log.Info("InitPlugin() called with args = {0}", (args == null) ? "" : string.Join(", ", args));

            // .. with built-in options
            _options = ExportTableOptions.CreateDefault();

            // try load defaults options from assy directory
            try
            {
                var newOpt =
                    AasxPluginOptionsBase.LoadDefaultOptionsFromAssemblyDir<ExportTableOptions>(
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
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "get-menu-items", "Provides a list of menu items of the plugin to the caller."));
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "call-menu-item", "Caller activates a named menu item.", useAsync: true));
            res.Add(new AasxPluginActionDescriptionBase("export-submodel", "Exports a Submodel."));
            res.Add(new AasxPluginActionDescriptionBase("import-submodel", "Imports a Submodel."));
            res.Add(new AasxPluginActionDescriptionBase("export-uml", "Exports a Submodel to an UML file."));
            res.Add(new AasxPluginActionDescriptionBase(
                "export-uml-dialogs", "Exports a Submodel to an UML file using AnyUI modal dialogs.",
                useAsync: true));
            res.Add(new AasxPluginActionDescriptionBase(
                "import-time-series", "Import time series data from a table file."));
            res.Add(new AasxPluginActionDescriptionBase(
                "interop-export", "Provides service to export a interop table into specific formats."));
            return res.ToArray();
        }

        public new AasxPluginResultBase ActivateAction(string action, params object[] args)
        {
            // can basic helper help to reduce lines of code?
            var help = ActivateActionBasicHelper(action, ref _options, args,
                disableDefaultLicense: true,
                enableGetCheckVisuExt: true);
            if (help != null)
                return help;

            // rest follows

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

            if (action == "get-menu-items")
            {
                // result list 
                var res = new List<AasxPluginResultSingleMenuItem>();

                // import table
                res.Add(new AasxPluginResultSingleMenuItem()
                {
                    AttachPoint = "Import",
                    MenuItem = new AasxMenuItem()
                    {
                        Name = "ImportTable",
                        Header = "Import SubmodelElements from Table …",
                        HelpText = "Import sets of SubmodelElements from table datat in multiple common formats.",
                        ArgDefs = new AasxMenuListOfArgDefs()
                                .Add("File", "Filename and path of file to imported.")
                                .Add("Preset", "Name of preset to load.")
                                .Add("Format", "Format to be either " +
                                        "'Tab separated', 'LaTex', 'Word', 'Excel', 'Markdown'.")
                                .Add("Record", "Record data", hidden: true)
                    }
                });

                // export table
                res.Add(new AasxPluginResultSingleMenuItem()
                {
                    AttachPoint = "Export",
                    MenuItem = new AasxMenuItem()
                    {
                        Name = "ExportTable",
                        Header = "Export SubmodelElements as Table …",
                        HelpText = "Export table(s) for sets of SubmodelElements in multiple common formats.",
                        ArgDefs = new AasxMenuListOfArgDefs()
                            .Add("File", "Filename and path of file to exported.")
                            .Add("Location", "Location of the file (local, user, download).")
                            .Add("Preset", "Name of preset to load.")
                            .Add("Format", "Format to be either " +
                                    "'Tab separated', 'LaTex', 'Word', 'Excel', 'Markdown'.")
                            .Add("Record", "Record data", hidden: true)
                    }
                });

                // export uml
                res.Add(new AasxPluginResultSingleMenuItem()
                {
                    AttachPoint = "Export",
                    MenuItem = new AasxMenuItem()
                    {
                        Name = "ExportUml",
                        Header = "Export Submodel as UML …",
                        HelpText = "Export UML of Submodel in multiple common formats.",
                        ArgDefs = new AasxMenuListOfArgDefs()
                            .Add("File", "Filename and path of file to exported.")
                            .Add("Location", "Location of the file (local, user, download).")
                            .Add("Format", "Format to be either 'XMI v1.1', 'XML v2.1', 'PlantUML'.")
                            .Add("Record", "Record data", hidden: true)
                            .AddFromReflection(new ExportUmlRecord())
                    }
                });

                // export SMT AsciiDoc
                res.Add(new AasxPluginResultSingleMenuItem()
                {
                    AttachPoint = "Export",
                    MenuItem = new AasxMenuItem()
                    {
                        Name = "ExportSmtAsciiDoc",
                        Header = "Export Package as AsciiDoc SMT spec …",
                        HelpText = "Export SMT in package and further AsciiDoc contents into a integrated " +
                            "AsciiDoc document.",
                        ArgDefs = new AasxMenuListOfArgDefs()
                            .Add("File", "Filename and path of file to exported.")
                            .Add("Location", "Location of the file (local, user, download).")
                            .Add("Format", "Format to be either 'adoc' or 'zip'.")
                            .Add("Record", "Record data", hidden: true)
                            .AddFromReflection(new ExportUmlRecord())
                    }
                });

                // import time series
                res.Add(new AasxPluginResultSingleMenuItem()
                {
                    AttachPoint = "Import",
                    MenuItem = new AasxMenuItem()
                    {
                        Name = "ImportTimeSeries",
                        Header = "Read time series values into SubModel …",
                        HelpText = "Import sets of time series values from an table in common format.",
                        ArgDefs = new AasxMenuListOfArgDefs()
                                .Add("File", "Filename and path of file to imported.")
                                .Add("Format", "Format to be 'Excel'.")
                                .Add("Record", "Record data", hidden: true)
                                .AddFromReflection(new ImportTimeSeriesRecord())
                    }
                });

                // return
                return new AasxPluginResultProvideMenuItems()
                {
                    MenuItems = res
                };
            }

            if (action == "interop-export")
            {
                if (args != null && args.Length >= 3
                    && args[0] is string fmt
                    && args[1] is string fn
                    && args[2] is AasxPluginExportTableInterop.InteropTable table)
                {
                    if (fmt == "excel")
                    {
                        var res = InteropUtils.ExportExcel(fn, table);
                        return new AasxPluginResultBaseObject(res.ToString(), res);
                    }
                }
            }

            // default
            return null;
        }

        /// <summary>
        /// Async variant of <c>ActivateAction</c>.
        /// Note: for some reason of type conversion, it has to return <c>Task<object></c>.
        /// </summary>
        public new async Task<object> ActivateActionAsync(string action, params object[] args)
        {
            if (action == "call-menu-item")
            {
                if (args != null && args.Length >= 3
                    && args[0] is string cmd
                    && args[1] is AasxMenuActionTicket ticket
                    && args[2] is AnyUiContextPlusDialogs displayContext)
                {
                    if (cmd == "exporttable")
                    {
                        await AnyUiDialogueTable.ImportExportTableDialogBased(
                            _options, _log, ticket, displayContext, _options, doImport: false);
                        return new AasxPluginResultBase();
                    }

                    if (cmd == "importtable")
                    {
                        await AnyUiDialogueTable.ImportExportTableDialogBased(
                            _options, _log, ticket, displayContext, _options, doImport: true);
                        return new AasxPluginResultBase();
                    }

                    if (cmd == "exportuml")
                    {
                        await AnyUiDialogueUmlExport.ExportUmlDialogBased(
                            _log, ticket, displayContext);
                        return new AasxPluginResultBase();
                    }

                    if (cmd == "exportuml")
                    {
                        await AnyUiDialogueUmlExport.ExportUmlDialogBased(
                            _log, ticket, displayContext);
                        return new AasxPluginResultBase();
                    }

                    if (cmd == "exportsmtasciidoc")
                    {
                        await AnyUiDialogueUmlExport.ExportUmlDialogBased(
                            _log, ticket, displayContext);
                        return new AasxPluginResultBase();
                    }

                    if (cmd == "importtimeseries")
                    {
                        await AnyUiDialogueTimeSeries.ImportTimeSeriesDialogBased(_log, ticket, displayContext);
                        return new AasxPluginResultBase();
                    }
                }
            }

            // default
            return null;
        }

    }
}
