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
using System.Threading.Tasks;using Aas = AasCore.Aas3_0_RC02;
using AdminShellNS;
using Extensions;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    [UsedImplicitlyAttribute]
    // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    public class AasxPlugin : AasxPluginBase
    {
        private AasxPluginGenericForms.GenericFormOptions _options = new AasxPluginGenericForms.GenericFormOptions();

#if USE_WPF
        private AasxPluginGenericForms.GenericFormsControl _formsControl = null;
#endif

        public class Session : PluginSessionBase
        {
            public AasxPluginGenericForms.GenericFormsAnyUiControl AnyUiControl = null;
        }

        static AasxPlugin()
        {
            PluginName = "AasxPluginGenericForms";
        }

        public new void InitPlugin(string[] args)
        {
            // start ..
            _log.Info("InitPlugin() called with args = {0}", (args == null) ? "" : string.Join(", ", args));

            // .. with built-in options
            _options = AasxPluginGenericForms.GenericFormOptions.CreateDefault();

            // try load defaults options from assy directory
            try
            {
                // need special settings
                var settings = AasxPluginOptionSerialization.GetDefaultJsonSettings(
                    new[] { typeof(AasxPluginGenericForms.GenericFormOptions), typeof(AasForms.FormDescBase),
                    typeof(AasxCompatibilityModels.AasxPluginGenericForms.GenericFormOptionsV20),
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
                        _log, upgrades.ToArray());
                if (newOpt != null)
                    _options = newOpt;

                // try find additional options
                _options.TryLoadAdditionalOptionsFromAssemblyDir
                    <AasxPluginGenericForms.GenericFormOptions>(
                        this.GetPluginName(), Assembly.GetExecutingAssembly(), settings,
                        _log, upgrades.ToArray());

                //// need special settings
                //var settings = AasxPluginOptionSerialization.GetDefaultJsonSettings(
                //    new[] { typeof(AasxPluginGenericForms.GenericFormOptions), typeof(AasForms.FormDescBase) });

                //// base options
                //var newOpt =
                //    AasxPluginOptionsBase.LoadDefaultOptionsFromAssemblyDir<AasxPluginGenericForms.GenericFormOptions>(
                //        this.GetPluginName(), Assembly.GetExecutingAssembly(), settings);
                //if (newOpt != null)
                //    this._options = newOpt;

                //// try find additional options
                //this._options.TryLoadAdditionalOptionsFromAssemblyDir<AasxPluginGenericForms.GenericFormOptions>(
                //    this.GetPluginName(), Assembly.GetExecutingAssembly(), settings, _log);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception when reading default options {1}");
            }
        }

        public new object CheckForLogMessage()
        {
            return _log.PopLastShortTermPrint();
        }

        public new AasxPluginActionDescriptionBase[] ListActions()
        {
            _log.Info("ListActions() called");
            var res = new List<AasxPluginActionDescriptionBase>();
            // for speed reasons, have the most often used at top!
            res.Add(new AasxPluginActionDescriptionBase(
                "call-check-visual-extension",
                "When called with Referable, returns possibly visual extension for it."));
            // rest follows
            res.Add(new AasxPluginActionDescriptionBase(
                "set-json-options", "Sets plugin-options according to provided JSON string."));
            res.Add(new AasxPluginActionDescriptionBase("get-json-options", "Gets plugin-options as a JSON string."));
            res.Add(new AasxPluginActionDescriptionBase("get-licenses", "Reports about used licenses."));
            res.Add(new AasxPluginActionDescriptionBase(
                "get-events", "Pops and returns the earliest event from the event stack."));
            res.Add(new AasxPluginActionDescriptionBase(
                "event-return", "Called to return a result evaluated by the host for a certain event."));
            res.Add(new AasxPluginActionDescriptionBase(
                "get-check-visual-extension", "Returns true, if plug-ins checks for visual extension."));
#if USE_WPF
            res.Add(new AasxPluginActionDescriptionBase(
                "fill-panel-visual-extension",
                "When called, fill given WPF panel with control for graph display."));
#endif
            res.Add(new AasxPluginActionDescriptionBase(
                "fill-anyui-visual-extension",
                "When called, fill given AnyUI panel with control for graph display."));
            res.Add(new AasxPluginActionDescriptionBase(
                "update-anyui-visual-extension",
                "When called, updated already presented AnyUI panel with some arguments."));
            res.Add(new AasxPluginActionDescriptionBase(
                "dispose-anyui-visual-extension",
                "When called, will dispose the plugin data associated with given session id."));
            res.Add(new AasxPluginActionDescriptionBase(
                "get-list-new-submodel",
                "Returns a list of speaking names of Submodels, which could be generated by the plugin."));
            res.Add(new AasxPluginActionDescriptionBase(
                "generate-submodel",
                "Returns a generated default Submodel based on the name provided as string argument."));
            return res.ToArray();
        }

        public new AasxPluginResultBase ActivateAction(string action, params object[] args)
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
                    var sm = args[0] as Aas.Submodel;
                    if (sm == null)
                        return null;

                    // check for a record in options, that matches Submodel
                    var found = this._options?.MatchRecordsForSemanticId(sm.SemanticId);
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
                        this._options = newOpt;
                }

                if (action == "get-json-options")
                {
                    var settings = AasxPluginOptionSerialization.GetDefaultJsonSettings(
                        new[] {
                            typeof(AasxPluginGenericForms.GenericFormOptions),
                            typeof(AasForms.FormDescBase) });
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(
                        this._options, typeof(AasxPluginGenericForms.GenericFormOptions), settings);
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

                if (action == "get-events" && _eventStack != null)
                {
                    // try access
                    return _eventStack.PopEvent();
                }

                if (action == "event-return" && args != null
                    && args.Length >= 1 && args[0] is AasxPluginEventReturnBase erb)
                {
                    // arguments (event return, session-id)

#if USE_WPF
                    if (_formsControl != null)
                        _formsControl.HandleEventReturn(erb);
#endif

                    if (args.Length >= 2
                        && _sessions.AccessSession(args[1], out Session session)
                        && session.AnyUiControl != null)
                    {
                        session.AnyUiControl.HandleEventReturn(erb);
                    }
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
                    // arguments (package, submodel, panel, display-context, session-id, operation-context)
                    if (args == null || args.Length < 6)
                        return null;

                    // create session and call
                    var session = _sessions.CreateNewSession<Session>(args[4]);
                    var opContext = args[5] as PluginOperationContextBase;
                    session.AnyUiControl = AasxPluginGenericForms.GenericFormsAnyUiControl.FillWithAnyUiControls(
                        _log, args[0], args[1], _options, _eventStack, args[2], opContext);

                    // give object back
                    var res = new AasxPluginResultBaseObject();
                    res.obj = session.AnyUiControl;
                    return res;
                }

                if (action == "update-anyui-visual-extension"
                    && _sessions != null)
                {
                    // arguments (panel, display-context, session-id)
                    if (args == null || args.Length < 3)
                        return null;

                    if (_sessions.AccessSession(args[2], out Session session))
                    {
                        // call
                        session.AnyUiControl.Update(args);

                        // give object back
                        var res = new AasxPluginResultBaseObject();
                        res.obj = 42;
                        return res;
                    }
                }

                if (action == "dispose-anyui-visual-extension"
                    && _sessions != null)
                {
                    // arguments (session-id)
                    if (args == null || args.Length < 1)
                        return null;

                    // ReSharper disable UnusedVariable
                    if (_sessions.AccessSession(args[0], out Session session))
                    {
                        // dispose all ressources
                        ;

                        // remove
                        _sessions.Remove(args[0]);
                    }
                    // ReSharper enable UnusedVariable
                }

#if USE_WPF
                if (action == "fill-panel-visual-extension")
                {
                    // arguments
                    if (args == null || args.Length < 3)
                        return null;

                    // call
                    this._formsControl = AasxPluginGenericForms.GenericFormsControl.FillWithWpfControls(
                        _log, args[0], args[1], this._options, _eventStack, args[2]);

                    // give object back
                    var res = new AasxPluginResultBaseObject();
                    res.obj = this._formsControl;
                    return res;
                }
#endif

                if (action == "get-list-new-submodel")
                {
                    // prepare list
                    var list = new List<string>();

                    // check
                    if (_options != null && _options.Records != null)
                        foreach (var rec in _options.Records)
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
                    if (_options != null && _options.Records != null)
                        foreach (var rec in _options.Records)
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
                _log.Error(ex, "");
            }

            // default
            return null;
        }

    }
}
