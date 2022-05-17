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
using JetBrains.Annotations;

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    [UsedImplicitlyAttribute]
    // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    public class AasxPlugin : AasxPluginBase
    {
        private AasxPluginImageMap.ImageMapOptions _options = new AasxPluginImageMap.ImageMapOptions();

        private AasxPluginImageMap.ImageMapControl _viewerControl = null;

        public class Session : PluginSessionBase
        {
            public AasxPluginImageMap.ImageMapAnyUiControl AnyUiControl = null;
        }

        static AasxPlugin()
        {
            PluginName = "AasxPluginImageMap";
        }

        public new void InitPlugin(string[] args)
        {
            // start ..
            _log.Info("InitPlugin() called with args = {0}", (args == null) ? "" : string.Join(", ", args));

            // .. with built-in options
            _options = AasxPluginImageMap.ImageMapOptions.CreateDefault();

            // try load defaults options from assy directory
            try
            {
                var newOpt =
                    AasxPluginOptionsBase.LoadDefaultOptionsFromAssemblyDir<AasxPluginImageMap.ImageMapOptions>(
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
            res.Add(new AasxPluginActionDescriptionBase("get-list-new-submodel",
                "Returns a list of speaking names of Submodels, which could be generated by the plugin."));
            res.Add(new AasxPluginActionDescriptionBase("generate-submodel",
                "Returns a generated default Submodel based on the name provided as string argument."));
            res.Add(new AasxPluginActionDescriptionBase(
                "fill-anyui-visual-extension",
                "When called, fill given AnyUI panel with control for graph display."));
            res.Add(new AasxPluginActionDescriptionBase(
                "update-anyui-visual-extension",
                "When called, updated already presented AnyUI panel with some arguments."));
            res.Add(new AasxPluginActionDescriptionBase(
                "dispose-anyui-visual-extension",
                "When called, will dispose the plugin data associated with given session id."));

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
                var cve = new AasxPluginResultVisualExtension("IMG", "Image Map");

                // ok
                return cve;
            }

            // rest follows

            if (action == "set-json-options" && args != null && args.Length >= 1 && args[0] is string)
            {
                var newOpt = JsonConvert.DeserializeObject<AasxPluginImageMap.ImageMapOptions>(args[0] as string);
                if (newOpt != null)
                    this._options = newOpt;
            }

            if (action == "get-json-options")
            {
                var json = JsonConvert.SerializeObject(this._options, Newtonsoft.Json.Formatting.Indented);
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
                // arguments (package, submodel, panel, display-context, session-id)
                if (args == null || args.Length < 5)
                    return null;

                // create session and call
                var session = _sessions.CreateNewSession<Session>(args[4]);
                session.AnyUiControl = AasxPluginImageMap.ImageMapAnyUiControl.FillWithAnyUiControls(
                    _log, args[0], args[1], _options, _eventStack, args[2], args[3]);

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

                if (_sessions.AccessSession(args[0], out Session session))
                {
                    // dispose all ressources
                    ;

                    // remove
                    _sessions.Remove(args[0]);
                }
            }

            if (action == "fill-panel-visual-extension")
            {
                // arguments
                if (args?.Length < 3)
                    return null;

                // call
                _viewerControl = AasxPluginImageMap.ImageMapControl.FillWithWpfControls(_log, args?[0], args?[1],
                    this._options, this._eventStack, args?[2]);

                // give object back
                var res = new AasxPluginResultBaseObject();
                res.obj = _viewerControl;
                return res;
            }

            if (action == "get-list-new-submodel")
            {
                // prepare list
                var list = new List<string>();
                list.Add("ImageMap");

                // make result
                var res = new AasxPluginResultBaseObject();
                res.obj = list;
                return res;
            }

            if (action == "generate-submodel" && args != null && args.Length >= 1 && args[0] is string)
            {
                // get arguments
                var smName = args[0] as string;
                if (smName == null)
                    return null;

                // generate (by hand)
                var sm = new AdminShell.Submodel();
                sm.semanticId = new AdminShell.SemanticId(AasxPredefinedConcepts.ImageMap.Static.SEM_ImageMapSubmodel);
                sm.idShort = "ImageMap";

                sm.SmeForWrite.CreateSMEForCD<AdminShell.File>(AasxPredefinedConcepts.ImageMap.Static.CD_ImageFile,
                    idShort: "ImageFile", addSme: true);

                var ent = sm.SmeForWrite.CreateSMEForCD<AdminShell.Entity>(
                    AasxPredefinedConcepts.ImageMap.Static.CD_EntityOfImageMap,
                    idShort: "Entity00", addSme: true);

                ent.CreateSMEForCD<AdminShell.Property>(
                    AasxPredefinedConcepts.ImageMap.Static.CD_RegionRect,
                    idShort: "RegionRect", addSme: true)?.Set("string", "[ 10, 10, 30, 30 ]");

                ent.CreateSMEForCD<AdminShell.Property>(
                    AasxPredefinedConcepts.ImageMap.Static.CD_RegionCircle,
                    idShort: "RegionCircle", addSme: true)?.Set("string", "[ 40, 40, 10 ]");

                ent.CreateSMEForCD<AdminShell.Property>(
                    AasxPredefinedConcepts.ImageMap.Static.CD_RegionPolygon,
                    idShort: "RegionPolygon", addSme: true)?.Set("string", "[ 20, 20, 50, 20, 40, 30 ]");

                ent.CreateSMEForCD<AdminShell.ReferenceElement>(
                    AasxPredefinedConcepts.ImageMap.Static.CD_NavigateTo,
                    idShort: "NavigateTo", addSme: true);

                var smcVE = sm.SmeForWrite.CreateSMEForCD<AdminShell.SubmodelElementCollection>(
                    AasxPredefinedConcepts.ImageMap.Static.CD_VisualElement,
                    idShort: "VisuElem00", addSme: true);

                smcVE.CreateSMEForCD<AdminShell.Property>(
                    AasxPredefinedConcepts.ImageMap.Static.CD_RegionRect,
                    idShort: "RegionRect", addSme: true)?.Set("string", "[ 50, 10, 70, 30 ]");

                smcVE.CreateSMEForCD<AdminShell.Property>(
                    AasxPredefinedConcepts.ImageMap.Static.CD_TextDisplay,
                    idShort: "TextDisplay01", addSme: true)?.Set("string", "Hallo");

                smcVE.CreateSMEForCD<AdminShell.Property>(
                    AasxPredefinedConcepts.ImageMap.Static.CD_TextDisplay,
                    idShort: "TextDisplay02", addSme: true)?
                        .Set("double", "3.1415")
                        .Set(new AdminShell.Qualifier("ImageMap.Args", "{ fmt: \"F2\" }"));

                smcVE.CreateSMEForCD<AdminShell.ReferenceElement>(
                    AasxPredefinedConcepts.ImageMap.Static.CD_TextDisplay,
                    idShort: "TextDisplay03", addSme: true);

                smcVE.CreateSMEForCD<AdminShell.Property>(
                    AasxPredefinedConcepts.ImageMap.Static.CD_Foreground,
                    idShort: "Foreground", addSme: true)?.Set("string", "#ffffffff");

                smcVE.CreateSMEForCD<AdminShell.Property>(
                    AasxPredefinedConcepts.ImageMap.Static.CD_Background,
                    idShort: "Background", addSme: true)?.Set("string", "#ff000040");

                // make result
                var res = new AasxPluginResultBaseObject();
                res.strType = "OK";
                res.obj = sm;
                return res;
            }

            // default
            return null;
        }

    }
}
