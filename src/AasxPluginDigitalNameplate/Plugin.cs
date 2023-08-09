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
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using JetBrains.Annotations;
using AnyUi;
using AasxPluginDigitalNameplate;

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    [UsedImplicitlyAttribute]
    // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    public class AasxPlugin : AasxPluginBase
    {
        private DigitalNameplateOptions _options = new DigitalNameplateOptions();

        public class Session : PluginSessionBase
        {
            public AasxPluginDigitalNameplate.NameplateAnyUiControl AnyUiControl = null;
        }

        public new void InitPlugin(string[] args)
        {
            // start ..
            PluginName = "AasxPluginDigitalNameplate";
            _log.Info("InitPlugin() called with args = {0}", (args == null) ? "" : string.Join(", ", args));

            // .. with built-in options
            _options = DigitalNameplateOptions.CreateDefault();

            // try load defaults options from assy directory
            try
            {
                var newOpt =
                    AasxPluginOptionsBase.LoadDefaultOptionsFromAssemblyDir<DigitalNameplateOptions>(
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

        public new AasxPluginActionDescriptionBase[] ListActions()
        {
            return ListActionsBasicHelper(
                enableCheckVisualExt: true,
                enableEventsGet: true,
                enableEventReturn: true,
                enablePanelAnyUi: true).ToArray();
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
                foreach (var rec in _options.LookupAllIndexKey<DigitalNameplateOptionsRecord>(
                    sm.SemanticId?.GetAsExactlyOneKey()))
                    found = true;
                if (!found)
                    return null;

                // success prepare record
                var cve = new AasxPluginResultVisualExtension("DN2", "Digital Nameplate");

                // ok
                return cve;
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
                lic.shortLicense = "";

                lic.isStandardLicense = true;
                lic.longLicense = AasxPluginHelper.LoadLicenseTxtFromAssemblyDir(
                    "LICENSE.txt", Assembly.GetExecutingAssembly());

                return lic;
            }

            if (action == "event-return" && args != null
                && args.Length >= 1 && args[0] is AasxPluginEventReturnBase erb)
            {
                // arguments (event return, session-id)
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
                session.AnyUiControl = AasxPluginDigitalNameplate.NameplateAnyUiControl.FillWithAnyUiControls(
                    _log, args[0], args[1], _options, _eventStack, session, args[2], opContext,
                    args[3] as AnyUiContextPlusDialogs, this);

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
                    session.AnyUiControl.Dispose();

                    // remove
                    _sessions.Remove(args[0]);
                }
                // ReSharper enable UnusedVariable
            }

            // default
            return null;
        }

    }
}
