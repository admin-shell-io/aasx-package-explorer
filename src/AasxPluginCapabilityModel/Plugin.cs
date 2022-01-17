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
using AdminShellNS;
using Newtonsoft.Json;

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    // ReSharper disable UnusedType.Global
    public class AasxPlugin : IAasxPluginInterface
    // ReSharper enable UnusedType.Global
    {
        private LogInstance _log = new LogInstance();
        private PluginEventStack _eventStack = new PluginEventStack();
        private AasxPluginCapabilityModel.CapabilityModelOptions _options
            = new AasxPluginCapabilityModel.CapabilityModelOptions();

        private AasxPluginCapabilityModel.CapabilityModelControl _viewerControl
            = new AasxPluginCapabilityModel.CapabilityModelControl();

        public string GetPluginName()
        {
            _log.Info("GetPluginName() = {0}", "AasxPluginCapabilityModel");
            return "AasxPluginCapabilityModel";
        }

        public void InitPlugin(string[] args)
        {
            // start ..
            _log.Info("InitPlugin() called with args = {0}", (args == null) ? "" : string.Join(", ", args));

            // .. with built-in options
            _options = AasxPluginCapabilityModel.CapabilityModelOptions.CreateDefault();

            // try load defaults options from assy directory
            try
            {
                var newOpt =
                    AasxPluginOptionsBase
                        .LoadDefaultOptionsFromAssemblyDir<AasxPluginCapabilityModel.CapabilityModelOptions>(
                            this.GetPluginName(), Assembly.GetExecutingAssembly());
                if (newOpt != null)
                    _options = newOpt;
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
            res.Add(new AasxPluginActionDescriptionBase("eval-value",
                "Evaluates a string function to check a Property's value."));
            res.Add(new AasxPluginActionDescriptionBase("call-check-visual-extension",
                "When called with Referable, returns possibly visual extension for it."));
            // rest follows
            res.Add(new AasxPluginActionDescriptionBase("set-json-options",
                "Sets plugin-options according to provided JSON string."));
            res.Add(new AasxPluginActionDescriptionBase("get-json-options", "Gets plugin-options as a JSON string."));
            res.Add(new AasxPluginActionDescriptionBase("get-licenses", "Reports about used licenses."));
            res.Add(new AasxPluginActionDescriptionBase("get-events",
                "Pops and returns the earliest event from the event stack."));
            res.Add(new AasxPluginActionDescriptionBase("get-check-visual-extension",
                "Returns true, if plug-ins checks for visual extension."));
            res.Add(new AasxPluginActionDescriptionBase("fill-panel-visual-extension",
                "When called, fill given WPF panel with control for graph display."));

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
                bool found = _options?.ContainsIndexKey(sm?.semanticId?.GetAsExactlyOneKey()) ?? false;
                if (!found)
                    return null;

                // success prepare record
                var cve = new AasxPluginResultVisualExtension("CPM", "Capability Model");

                // ok
                return cve;
            }

            if (action == "eval-value")
            {
                var res = new AasxPluginResultBaseObject("MICHA43", "MICHA43");
                return res;
            }

            // rest follows

            if (action == "set-json-options" && args != null && args.Length >= 1 && args[0] is string)
            {
                var newOpt = JsonConvert.DeserializeObject<AasxPluginCapabilityModel.CapabilityModelOptions>(
                    args[0] as string);
                if (newOpt != null)
                    _options = newOpt;
            }

            if (action == "get-json-options")
            {
                var json = JsonConvert.SerializeObject(_options, Newtonsoft.Json.Formatting.Indented);
                return new AasxPluginResultBaseObject("OK", json);
            }

            if (action == "get-licenses")
            {
                var lic = new AasxPluginResultLicense();
                lic.shortLicense = "";
                lic.longLicense = "";
                lic.isStandardLicense = true;

                return lic;
            }

            if (action == "get-events" && _eventStack != null)
            {
                // try access
                return _eventStack.PopEvent();
            }

            if (action == "get-check-visual-extension")
            {
                var cve = new AasxPluginResultBaseObject();
                cve.strType = "True";
                cve.obj = true;
                return cve;
            }

            if (action == "fill-panel-visual-extension" && _viewerControl != null)
            {
                // arguments
                if (args?.Length < 3)
                    return null;

                // call
                var resobj = AasxPluginCapabilityModel.CapabilityModelControl.FillWithWpfControls(
                    _log, args?[0], args?[1],
                    _options, _eventStack, args?[2]);

                // give object back
                var res = new AasxPluginResultBaseObject();
                res.obj = resobj;
                return res;
            }

            // default
            return null;
        }

    }
}
