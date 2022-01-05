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
using Newtonsoft.Json;

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    [UsedImplicitlyAttribute]
    // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    public class AasxPlugin : IAasxPluginInterface
    {
        public LogInstance Log = new LogInstance();
        private PluginEventStack eventStack = new PluginEventStack();
        private AasxPluginGenericForms.GenericFormOptions options = new AasxPluginGenericForms.GenericFormOptions();
        private AasxPluginGenericForms.GenericFormsControl formsControl = null;

        public string GetPluginName()
        {
            return "AasxPluginGenericForms";
        }

        public void InitPlugin(string[] args)
        {
            // start ..
            Log.Info("InitPlugin() called with args = {0}", (args == null) ? "" : string.Join(", ", args));

            // .. with built-in options
            options = AasxPluginGenericForms.GenericFormOptions.CreateDefault();

            // try load defaults options from assy directory
            try
            {
                // need special settings
                var settings = AasxPluginOptionSerialization.GetDefaultJsonSettings(
                    new[] { typeof(AasxPluginGenericForms.GenericFormOptions), typeof(AasForms.FormDescBase),
                    typeof(AasxCompatibilityModels.AdminShellV20) });

                // this plugin can read OLD options (using the meta-model V2.0.1)
                var upgrades = new List<AasxPluginOptionsBase.UpgradeMapping>();
                upgrades.Add(new AasxPluginOptionsBase.UpgradeMapping()
                {
                    Info = "AAS2.0.1",
                    Trigger = "AdminShellNS.AdminShellV20+",
                    OldRootType = typeof(AasxCompatibilityModels.AasxPluginGenericForms.GenericFormOptionsV20),
                    Replacements = new Dictionary<string, string>()
                    {
                        { "AdminShellNS.AdminShellV20+", "AasxCompatibilityModels.AdminShellV20+" }
                    },
                    UpgradeLambda = (old) => new AasxPluginGenericForms.GenericFormOptions(
                        old as AasxCompatibilityModels.AasxPluginGenericForms.GenericFormOptionsV20)
                });

                // base options
                var newOpt = AasxPluginOptionsBase.LoadDefaultOptionsFromAssemblyDir
                    <AasxPluginGenericForms.GenericFormOptions>(
                        this.GetPluginName(), Assembly.GetExecutingAssembly(), settings, 
                        this.Log, upgrades.ToArray());
                if (newOpt != null)
                    this.options = newOpt;

                // try find additional options
                this.options.TryLoadAdditionalOptionsFromAssemblyDir
                    <AasxPluginGenericForms.GenericFormOptions>(
                        this.GetPluginName(), Assembly.GetExecutingAssembly(), settings, 
                        this.Log, upgrades.ToArray());
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
            res.Add(new AasxPluginActionDescriptionBase(
                    "event-return", "Called to return a result evaluated by the host for a certain event."));
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "get-check-visual-extension", "Returns true, if plug-ins checks for visual extension."));
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "fill-panel-visual-extension",
                    "When called, fill given WPF panel with control for graph display."));
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "get-list-new-submodel",
                    "Returns a list of speaking names of Submodels, which could be generated by the plugin."));
            res.Add(
                new AasxPluginActionDescriptionBase(
                    "generate-submodel",
                    "Returns a generated default Submodel based on the name provided as string argument."));
            return res.ToArray();
        }

        public AasxPluginResultBase ActivateAction(string action, params object[] args)
        {
            try
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
                    var found = this.options?.MatchRecordsForSemanticId(sm.semanticId);
                    if (found == null)
                        return null;

                    // success prepare record
                    var cve = new AasxPluginResultVisualExtension(found.FormTag, found.FormTitle);

                    // ok
                    return cve;
                }

                // rest follows

                if (action == "set-json-options" && args != null && args.Length >= 1 && args[0] is string)
                {
                    var settings =
                        AasxPluginOptionSerialization.GetDefaultJsonSettings(
                            new[] {
                                typeof(AasxPluginGenericForms.GenericFormOptions),
                                typeof(AasForms.FormDescBase) });
                    var newOpt =
                        Newtonsoft.Json.JsonConvert.DeserializeObject<AasxPluginGenericForms.GenericFormOptions>(
                            (args[0] as string), settings);
                    if (newOpt != null)
                        this.options = newOpt;
                }

                if (action == "get-json-options")
                {
                    var settings = AasxPluginOptionSerialization.GetDefaultJsonSettings(
                        new[] {
                            typeof(AasxPluginGenericForms.GenericFormOptions),
                            typeof(AasForms.FormDescBase) });
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(
                        this.options, typeof(AasxPluginGenericForms.GenericFormOptions), settings);
                    return new AasxPluginResultBaseObject("OK", json);
                }

                if (action == "get-licenses")
                {
                    var lic = new AasxPluginResultLicense();
                    lic.shortLicense = "";

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

                if (action == "event-return" && args != null
                    && args.Length >= 1 && args[0] is AasxPluginEventReturnBase
                    && this.formsControl != null)
                {
                    this.formsControl.HandleEventReturn(args[0] as AasxPluginEventReturnBase);
                }

                if (action == "get-check-visual-extension")
                {
                    var cve = new AasxPluginResultBaseObject();
                    cve.strType = "True";
                    cve.obj = true;
                    return cve;
                }

                if (action == "fill-panel-visual-extension")
                {
                    // arguments
                    if (args == null || args.Length < 3)
                        return null;

                    // call
                    this.formsControl = AasxPluginGenericForms.GenericFormsControl.FillWithWpfControls(
                        Log, args[0], args[1], this.options, this.eventStack, args[2]);

                    // give object back
                    var res = new AasxPluginResultBaseObject();
                    res.obj = this.formsControl;
                    return res;
                }

                if (action == "get-list-new-submodel")
                {
                    // prepare list
                    var list = new List<string>();

                    // check
                    if (options != null && options.Records != null)
                        foreach (var rec in options.Records)
                            if (rec.FormTitle != null)
                                list.Add("" + rec.FormTitle);

                    // make result
                    var res = new AasxPluginResultBaseObject();
                    res.strType = "OK";
                    res.obj = list;
                    return res;
                }

                if (action == "generate-submodel" && args != null && args.Length >= 1 && args[0] is string)
                {
                    // get arguments
                    var smName = args[0] as string;
                    if (smName == null)
                        return null;

                    // identify record
                    AasxPluginGenericForms.GenericFormsOptionsRecord foundRec = null;
                    if (options != null && options.Records != null)
                        foreach (var rec in options.Records)
                            if (rec.FormTitle != null && rec.FormTitle == smName)
                                foundRec = rec;
                    if (foundRec == null || foundRec.FormSubmodel == null)
                        return null;

                    // generate
                    var sm = foundRec.FormSubmodel.GenerateDefault();

                    // make result
                    var res = new AasxPluginResultGenerateSubmodel();
                    res.sm = sm;
                    res.cds = foundRec.ConceptDescriptions;
                    return res;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "");
            }

            // default
            return null;
        }

    }
}
