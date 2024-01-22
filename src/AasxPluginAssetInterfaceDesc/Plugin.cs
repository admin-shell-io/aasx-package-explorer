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
using AnyUi;
using System.Windows.Controls;
using System.IO.Packaging;

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
            PluginName = "AasxPluginAssetInterfaceDesc";

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
            _interfaceService.StartOperation(_log, _eventStack, _allInterfaceStatus);
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
                enableMenuItems: true,
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

                // specifically find the record
                var foundOptRec = _options.LookupAllIndexKey<AssetInterfaceOptionsRecord>(
                    sm.SemanticId.GetAsExactlyOneKey()).FirstOrDefault();
                if (foundOptRec == null)
                    return null;

                // remember for later / background
                if (foundOptRec.IsDescription)
                    _allInterfaceStatus.RememberAidSubmodel(sm, foundOptRec,
                        adoptUseFlags: true);
                if (foundOptRec.IsMapping)
                    _allInterfaceStatus.RememberMappingSubmodel(sm);

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
                    session.AnyUiControl.Dispose();

                    // remove
                    _sessions.Remove(args[0]);
                }
                // ReSharper enable UnusedVariable
            }

            if (action == "get-menu-items")
            {
                // result list 
                var res = new List<AasxPluginResultSingleMenuItem>();

                // attach
                // note: need to be single items, no childs allowed!
                res.Add(new AasxPluginResultSingleMenuItem()
                {
                    AttachPoint = "Plugins",
                    MenuItem = new AasxMenuItem()
                    {
                        Name = "AssetInterfaceOperationStart",
                        Header = "Asset Interfaces (AID): Start operations …",
                        HelpText = "Looks for a suitable Submodel with SMT Asset Interfaces Descriptions " +
                                    "(AID) and mapping and starts operations in the background."
                    }
                });
                res.Add(new AasxPluginResultSingleMenuItem()
                {
                    AttachPoint = "Plugins",
                    MenuItem = new AasxMenuItem()
                    {
                        Name = "AssetInterfaceOperationStop",
                        Header = "Asset Interfaces (AID): Stop operations …",
                        HelpText = "Stops all AID operations."
                    }
                });

                // return
                return new AasxPluginResultProvideMenuItems()
                {
                    MenuItems = res
                };
            }

            // default
            return null;
        }

        /// <summary>
        /// Async variant of <c>ActivateAction</c>.
        /// Note: for some reason of type conversion, it has to return <c>Task<object></c>.
        /// </summary>
        public new async Task<object> ActivateActionAsync(string action, params object[] args)
        {
            if (action == "call-menu-item")
            {
                if (args != null && args.Length >= 3
                    && args[0] is string cmd
                    && args[1] is AasxMenuActionTicket ticket
                    && args[2] is AnyUiContextPlusDialogs displayContext
                    && args[3] is DockPanel masterPanel)
                {
                    try
                    {
                        if (cmd == "assetinterfaceoperationstart")
                        {
                            await Task.Yield();

                            // be safe
                            if (_allInterfaceStatus == null)
                            {
                                _log?.Error("Error accessing background all interface status! Aborting.");
                                return null;
                            }

                            try
                            {
                                // in (used!)Submodels of the respecitve AAS, find memories
                                _allInterfaceStatus.RememberNothing();
                                if (ticket.Env != null)
                                    foreach (var sm in ticket.Env.FindAllSubmodelGroupedByAAS())
                                    {
                                        var foundOptRec = _options.LookupAllIndexKey<AssetInterfaceOptionsRecord>(
                                            sm.SemanticId.GetAsExactlyOneKey()).FirstOrDefault();
                                        if (foundOptRec == null)
                                            continue;

                                        if (foundOptRec.IsDescription)
                                            _allInterfaceStatus.RememberAidSubmodel(sm, foundOptRec,
                                                adoptUseFlags: true);
                                        if (foundOptRec.IsMapping)
                                            _allInterfaceStatus.RememberMappingSubmodel(sm);
                                    }

                                // start?
                                if (_allInterfaceStatus.EnoughMemories())
                                {
                                    // switch off current?
                                    if (_allInterfaceStatus.ContinousRun)
                                    {
                                        _log?.Info("Asset Interfaces: stopping current operation ..");
                                        _allInterfaceStatus.StopContinousRun();
                                    }

                                    // (re-) init
                                    _log?.Info("Asset Interfaces: starting new operation ..");
                                    _allInterfaceStatus.PrepareAidInformation(
                                        _allInterfaceStatus.SmAidDescription,
                                        _allInterfaceStatus.SmAidMapping,
                                        lambdaLookupReference: (rf) => ticket?.Env.FindReferableByReference(rf));
                                    _allInterfaceStatus.SetAidInformationForUpdateAndTimeout();
                                    _allInterfaceStatus.StartContinousRun();
                                }

                            } catch (Exception ex)
                            {
                                _log?.Error(ex, "when starting Asset Interface operations");
                            }

                            // give object back
                            var res = new AasxPluginResultCallMenuItem();
                            return res;
                        }

                        if (cmd == "assetinterfaceoperationstop")
                        {
                            await Task.Yield();

                            // be safe
                            if (_allInterfaceStatus == null)
                            {
                                _log?.Error("Error accessing background all interface status! Aborting.");
                                return null;
                            }

                            try
                            {
                                // switch off current?
                                if (_allInterfaceStatus.ContinousRun)
                                {
                                    _log?.Info("Asset Interfaces: stopping current operation ..");
                                    _allInterfaceStatus.StopContinousRun();
                                }
                            }
                            catch (Exception ex)
                            {
                                _log?.Error(ex, "when starting Asset Interface operations");
                            }

                            // give object back
                            var res = new AasxPluginResultCallMenuItem();
                            return res;
                        }
                    }
                    catch (Exception ex)
                    {
                        _log?.Error(ex, "when executing plugin menu item " + cmd);
                    }
                }
            }

            // default
            return null;
        }

    }
}
