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
using AasxIntegrationBase.AdminShellEvents;
using Aas = AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;
using JetBrains.Annotations;

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    [UsedImplicitlyAttribute]
    // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    public class AasxPlugin : AasxPluginBase
    {
        private LogInstance _log = new LogInstance();
        private PluginEventStack _eventStack = new PluginEventStack();
        private AasxPluginPlotting.PlottingOptions _options = new AasxPluginPlotting.PlottingOptions();

        private AasxPluginPlotting.PlottingViewControl _viewControl;

        static AasxPlugin()
        {
            PluginName = "AasxPluginPlotting";
        }

        public new void InitPlugin(string[] args)
        {
            // start ..
            _log.Info("InitPlugin() called with args = {0}", (args == null) ? "" : string.Join(", ", args));

            // .. with built-in options
            _options = AasxPluginPlotting.PlottingOptions.CreateDefault();

            // try load defaults options from assy directory
            try
            {
                var newOpt =
                    AasxPluginOptionsBase.LoadDefaultOptionsFromAssemblyDir<
                         AasxPluginPlotting.PlottingOptions>(
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
            // for speed reasons, have the most often used at top!
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "call-check-visual-extension",
                    "When called with Referable, returns possibly visual extension for it."));
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "push-aas-event", "Pushes an AAS event to the plugin."));
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
                    "When called, fill given WPF panel with control for plugin display."));
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "clear-panel-visual-extension",
                    "Clear the panel information; might occur before fill-panel is called."));
            return res.ToArray();
        }

        public new AasxPluginResultBase ActivateAction(string action, params object[] args)
        {
            // for speed reasons, have the most often used at top!
            if (action == "call-check-visual-extension")
            {
                // arguments
                if (args.Length < 1)
                    return null;

                // looking only for Submodels
                var sm = args[0] as Aas.Submodel;
                if (sm == null)
                    return null;

                // check for a record in options, that matches Submodel
                var found = false;
                if (this._options != null && this._options.Records != null)
                    foreach (var rec in this._options.Records)
                        if (rec.AllowSubmodelSemanticId != null)
                            foreach (var x in rec.AllowSubmodelSemanticId)
                                if (sm.SemanticId != null && sm.SemanticId.MatchesExactlyOneKey(x))
                                {
                                    found = true;
                                    break;
                                }
                if (!found)
                    return null;

                // success prepare record
                var cve = new AasxPluginResultVisualExtension("PLOT", "Plotting of data");

                // ok
                return cve;
            }

            if (action == "push-aas-event")
            {
                // arguments
                if (args.Length < 1 || !(args[0] is AasEventMsgEnvelope ev))
                    return null;

                _viewControl?.PushEvent(ev);
            }

            // rest follows

            if (action == "set-json-options" && args != null && args.Length >= 1 && args[0] is string)
            {
                var newOpt =
                    Newtonsoft.Json.JsonConvert.DeserializeObject<AasxPluginPlotting.PlottingOptions>(
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
                    "The application uses the ScottPlot NuGet package, which is under the MIT license.";

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

            if (action == "clear-panel-visual-extension")
            {
                // simple delete reference to view control
                // this shall also stop event notifications!
                if (_viewControl != null)
                    _viewControl.Stop();
                _viewControl = null;
            }

            if (action == "fill-panel-visual-extension" && args != null && args.Length >= 3)
            {
                // access
                var package = args[0] as AdminShellPackageEnv;
                var sm = args[1] as Aas.Submodel;
                var master = args[2] as DockPanel;
                if (package == null || sm == null || master == null)
                    return null;

                // the Submodel elements need to have parents
                sm.SetAllParents();

                // create TOP control
                _viewControl = new AasxPluginPlotting.PlottingViewControl();
                _viewControl.Start(package, sm, _options, _eventStack, _log);
                master.Children.Add(_viewControl);

                // give object back
                var res = new AasxPluginResultBaseObject();
                res.obj = _viewControl;
                return res;
            }

            // default
            return null;
        }

    }
}
