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
using Newtonsoft.Json;
using AasxPluginAssetInterfaceDescription;

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    [UsedImplicitlyAttribute]
    // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    public class AasxPlugin : AasxPluginBase
    {
        private AasxPluginAssetInterfaceDescription.AssetInterfaceOptions _options
            = new AasxPluginAssetInterfaceDescription.AssetInterfaceOptions();

        // TODO: make this multi-session!!
        private AidAllInterfaceStatus _allInterfaceStatus = null;
        private AidInterfaceService _interfaceService = null;

        public class Session : PluginSessionBase
        {
            public AasxPluginAssetInterfaceDescription.AssetInterfaceAnyUiControl AnyUiControl = null;
        }

        public new void InitPlugin(string[] args)
        {
            // start ..
            PluginName = "AasxPluginAssetInterfaceDescription";

            // .. with built-in options
            _options = AasxPluginAssetInterfaceDescription.AssetInterfaceOptions.CreateDefault();

            // try load defaults options from assy directory
            try
            {
                var newOpt =
                    AasxPluginOptionsBase
                        .LoadDefaultOptionsFromAssemblyDir<AssetInterfaceOptions>(
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

            // start interface service
            _allInterfaceStatus = new AidAllInterfaceStatus(_log);
            _interfaceService = new AidInterfaceService();
            _interfaceService.StartOperation(_log, _allInterfaceStatus);
        }

        public new object CheckForLogMessage()
        {
            return _log.PopLastShortTermPrint();
        }

        public new AasxPluginActionDescriptionBase[] ListActions()
        {
            var res = ListActionsBasicHelper(
                enableCheckVisualExt: true,
                enableOptions: true,
                enableLicenses: true,
                enableEventsGet: true,
                enableEventReturn: true,
                enablePanelAnyUi: true);
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
                bool found = _options?.ContainsIndexKey(sm?.SemanticId?.GetAsExactlyOneKey()) ?? false;
                if (!found)
                    return null;

                // success prepare record
                var cve = new AasxPluginResultVisualExtension("AID", "Asset Interfaces Description");

                // ok
                return cve;
            }

            // can basic helper help to reduce lines of code?
            var help = ActivateActionBasicHelper(action, ref _options, args,
                enableGetCheckVisuExt: true);
            if (help != null)
                return help;

            // rest follows           

            if (action == "fill-anyui-visual-extension")
            {
                // arguments (package, submodel, panel, display-context, session-id)
                if (args == null || args.Length < 5)
                    return null;

                // create session and call
                var session = _sessions.CreateNewSession<Session>(args[4]);
                session.AnyUiControl = AasxPluginAssetInterfaceDescription.AssetInterfaceAnyUiControl.FillWithAnyUiControls(
                    _log, args[0], args[1], _options, _eventStack, session, args[2], this, _allInterfaceStatus);

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

            // default
            return null;
        }

    }
}
