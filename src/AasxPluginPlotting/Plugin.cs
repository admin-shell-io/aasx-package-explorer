/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using JetBrains.Annotations;

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    [UsedImplicitlyAttribute]
    // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    public class AasxPlugin : AasxPluginBase
    {
        private AasxPluginPlotting.PlottingOptions _options = new AasxPluginPlotting.PlottingOptions();

        private AasxPluginPlotting.PlottingViewControl _viewControl;

        public new void InitPlugin(string[] args)
        {
            // start ..
            PluginName = "AasxPluginPlotting";
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

            // index them!
            _options.IndexListOfRecords(_options.Records);
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
                // ReSharper disable once UnusedVariable
                foreach (var rec in _options.LookupAllIndexKey<AasxPluginPlotting.PlottingOptionsRecord>(
                    sm.SemanticId?.GetAsExactlyOneKey()))
                    found = true;
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
                lic.shortLicense =
                    "The application uses the ScottPlot NuGet package, which is under the MIT license.";

                lic.isStandardLicense = true;
                lic.longLicense = AasxPluginHelper.LoadLicenseTxtFromAssemblyDir(
                    "LICENSE.txt", Assembly.GetExecutingAssembly());

                return lic;
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
