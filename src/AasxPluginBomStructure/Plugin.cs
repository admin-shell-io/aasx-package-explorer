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
using JetBrains.Annotations;

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    [UsedImplicitlyAttribute]
    // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    public class AasxPlugin : IAasxPluginInterface
    {
        public LogInstance Log = new LogInstance();
        private PluginEventStack _eventStack = new PluginEventStack();
        private AasxPluginBomStructure.BomStructureOptions _options = new AasxPluginBomStructure.BomStructureOptions();

        private AasxPluginBomStructure.GenericBomControl _bomControl = new AasxPluginBomStructure.GenericBomControl();

        public string GetPluginName()
        {
            return "AasxPluginBomStructure";
        }

        public void InitPlugin(string[] args)
        {
            // start ..
            Log.Info("InitPlugin() called with args = {0}", (args == null) ? "" : string.Join(", ", args));

            // .. with built-in options
            _options = AasxPluginBomStructure.BomStructureOptions.CreateDefault();

            // try load defaults options from assy directory
            try
            {
                // this plugin can read OLD options (using the meta-model V2.0.1)
                var upgrades = new List<AasxPluginOptionsBase.UpgradeMapping>();
                upgrades.Add(new AasxPluginOptionsBase.UpgradeMapping()
                {
                    Info = "AAS2.0.1",
                    Trigger = @"""local""",
                    OldRootType = typeof(AasxCompatibilityModels.AasxPluginBomStructure.BomStructureOptionsV20),
                    Replacements = null,
                    UpgradeLambda = (old) => new AasxPluginBomStructure.BomStructureOptions(
                        old as AasxCompatibilityModels.AasxPluginBomStructure.BomStructureOptionsV20)
                });

                // read options?
                var newOpt =
                    AasxPluginOptionsBase
                        .LoadDefaultOptionsFromAssemblyDir<AasxPluginBomStructure.BomStructureOptions>(
                            this.GetPluginName(), Assembly.GetExecutingAssembly(), null, Log, upgrades.ToArray());
                if (newOpt != null)
                    this._options = newOpt;
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
                if (sm == null || _options == null)
                    return null;

                // check for a record in options, that matches Submodel
                var found = false;
                // ReSharper disable UnusedVariable
                foreach (var x in _options.MatchingRecords(sm.semanticId))
                {
                    found = true;
                    break;
                }
                if (!found)
                    return null;
                // ReSharper enable UnusedVariable

                // success prepare record
                var cve = new AasxPluginResultVisualExtension("BOM", "Bill of Material - Graph display");

                // ok
                return cve;
            }

            // rest follows

            if (action == "set-json-options" && args != null && args.Length >= 1 && args[0] is string)
            {
                var newOpt =
                    Newtonsoft.Json.JsonConvert.DeserializeObject<AasxPluginBomStructure.BomStructureOptions>(
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
                    "The Microsoft Microsoft Automatic Graph Layout, MSAGL, is licensed under the MIT license (MIT).";

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

            if (action == "fill-panel-visual-extension" && this._bomControl != null)
            {
                // arguments
                if (args == null || args.Length < 3)
                    return null;

                // call
                this._bomControl.SetEventStack(this._eventStack);
                var resobj = this._bomControl.FillWithWpfControls(_options, args[0], args[1], args[2]);

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
