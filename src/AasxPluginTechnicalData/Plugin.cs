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
using System.Windows.Controls;
using AdminShellNS;
using JetBrains.Annotations;

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    [UsedImplicitlyAttribute]
    // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    public class AasxPlugin : IAasxPluginInterface
    {
        private LogInstance _log = new LogInstance();
        private PluginEventStack _eventStack = new PluginEventStack();
        private AasxPluginTechnicalData.TechnicalDataOptions _options =
            new AasxPluginTechnicalData.TechnicalDataOptions();

        private AasxPluginTechnicalData.TechnicalDataViewControl _viewControl = null;

        private AasxPluginTechnicalData.TechnicalDataAnyUiControl _anyUiControl = null;

        public static string PluginName = "AasxPluginTechnicalData";

        public string GetPluginName()
        {
            return PluginName;
        }

        public void InitPlugin(string[] args)
        {
            // start ..
            _log.Info("InitPlugin() called with args = {0}", (args == null) ? "" : string.Join(", ", args));

            // .. with built-in options
            _options = AasxPluginTechnicalData.TechnicalDataOptions.CreateDefault();

            // try load defaults options from assy directory
            try
            {
                var newOpt =
                    AasxPluginOptionsBase.LoadDefaultOptionsFromAssemblyDir<
                        AasxPluginTechnicalData.TechnicalDataOptions>(
                            this.GetPluginName(), Assembly.GetExecutingAssembly());
                if (newOpt != null)
                    this._options = newOpt;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception when reading default options {1}");
            }

            // index them!
            _options.IndexListOfRecords(_options.Records);
        }

        public object CheckForLogMessage()
        {
            return _log.PopLastShortTermPrint();
        }

        public AasxPluginActionDescriptionBase[] ListActions()
        {
            _log.Info("ListActions() called");
            var res = new List<AasxPluginActionDescriptionBase>();
            // for speed reasons, have the most often used at top!
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "call-check-visual-extension",
                    "When called with Referable, returns possibly visual extension for it."));
            // rest follows
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "set-json-options", "Sets plugin-options according to provided JSON string."));
            res.Add(new AasxPluginActionDescriptionBase("get-json-options", "Gets plugin-options as a JSON string."));
            res.Add(new AasxPluginActionDescriptionBase("get-licenses", "Reports about used licenses."));
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "get-events", "Pops and returns the earliest event from the event stack."));
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "get-check-visual-extension", "Returns true, if plug-ins checks for visual extension."));
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "fill-panel-visual-extension",
                    "When called, fill given WPF panel with control for graph display."));
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "fill-anyui-visual-extension",
                    "When called, fill given AnyUI panel with control for graph display."));
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "update-anyui-visual-extension",
                    "When called, updated already presented AnyUI panel with some arguments."));
            return res.ToArray();
        }

        public AasxPluginResultBase ActivateAction(string action, params object[] args)
        {
            // for speed reasons, have the most often used at top!
            if (action == "call-check-visual-extension")
            {
                // arguments
                if (args.Length < 1)
                    return null;

                // looking only for Submodels
                var sm = args[0] as AdminShell.Submodel;
                if (sm == null)
                    return null;

                // check for a record in options, that matches Submodel
                var found = false;
                if (this._options != null && this._options.Records != null)
                    foreach (var rec in this._options.Records)
                        if (rec.AllowSubmodelSemanticId != null)
                            foreach (var x in rec.AllowSubmodelSemanticId)
                                if (sm.semanticId != null && sm.semanticId.Matches(x))
                                {
                                    found = true;
                                    break;
                                }
                if (!found)
                    return null;

                // success prepare record
                var cve = new AasxPluginResultVisualExtension("TED", "Technical Data Viewer");

                // ok
                return cve;
            }

            // rest follows

            if (action == "set-json-options" && args != null && args.Length >= 1 && args[0] is string)
            {
                var newOpt =
                    Newtonsoft.Json.JsonConvert.DeserializeObject<AasxPluginTechnicalData.TechnicalDataOptions>(
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
                lic.shortLicense =
                    "The application uses one class provided under The Code Project Open License (CPOL).";

                lic.isStandardLicense = true;
                lic.longLicense = AasxPluginHelper.LoadLicenseTxtFromAssemblyDir(
                    "LICENSE.txt", Assembly.GetExecutingAssembly());

                return lic;
            }

            if (action == "get-events" && this._eventStack != null)
            {
                // try access
                return this._eventStack.PopEvent();
            }

            if (action == "get-check-visual-extension")
            {
                var cve = new AasxPluginResultBaseObject();
                cve.strType = "True";
                cve.obj = true;
                return cve;
            }

            if (action == "fill-anyui-visual-extension")
            {
                // arguments
                if (args == null || args.Length < 3)
                    return null;

                // call
                _anyUiControl = AasxPluginTechnicalData.TechnicalDataAnyUiControl.FillWithAnyUiControls(
                    _log, args[0], args[1], _options, _eventStack, args[2]);

                // give object back
                var res = new AasxPluginResultBaseObject();
                res.obj = _anyUiControl;
                return res;
            }

            if (action == "update-anyui-visual-extension"
                && _anyUiControl != null)
            {
                // arguments
                if (args == null || args.Length < 0)
                    return null;

                // call
                _anyUiControl.Update(args);

                // give object back
                var res = new AasxPluginResultBaseObject();
                res.obj = 42;
                return res;
            }

            if (action == "fill-panel-visual-extension" && args != null && args.Length >= 3)
            {
                // access
                var package = args[0] as AdminShellPackageEnv;
                var sm = args[1] as AdminShell.Submodel;
                var master = args[2] as DockPanel;
                if (package == null || sm == null || master == null)
                    return null;

                // the Submodel elements need to have parents
                sm.SetAllParents();

                // create TOP control
                this._viewControl = new AasxPluginTechnicalData.TechnicalDataViewControl();
                this._viewControl.Start(package, sm, _options, _eventStack);
                master.Children.Add(this._viewControl);

                // give object back
                var res = new AasxPluginResultBaseObject();
                res.obj = this._viewControl;
                return res;
            }

            // default
            return null;
        }

    }
}
