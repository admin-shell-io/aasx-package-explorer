/*
Copyright (c) 2018-2023 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
using AasxPluginBomStructure;
using AnyUi;
using System.Windows.Controls;

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    [UsedImplicitlyAttribute]
    // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    public class AasxPlugin : AasxPluginBase
    {
        protected AasxPluginBomStructure.BomStructureOptions _options = new AasxPluginBomStructure.BomStructureOptions();

        private AasxPluginBomStructure.GenericBomControl _bomControl = new AasxPluginBomStructure.GenericBomControl();

        public new void InitPlugin(string[] args)
        {
            // start ..
            PluginName = "AasxPluginBomStructure";
            _log.Info("InitPlugin() called with args = {0}", (args == null) ? "" : string.Join(", ", args));

            // .. with built-in options
            _options = AasxPluginBomStructure.BomStructureOptions.CreateDefault();

            // try load defaults options from assy directory
            try
            {
                var newOpt =
                    AasxPluginOptionsBase
                        .LoadDefaultOptionsFromAssemblyDir<AasxPluginBomStructure.BomStructureOptions>(
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
            _options.Index();
        }

        public new AasxPluginActionDescriptionBase[] ListActions()
        {
            return ListActionsBasicHelper(
                enableCheckVisualExt: true,
                enablePanelWpf: true,
                enableMenuItems: true,
                enableEventsGet: true).ToArray();
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
                if (sm == null || _options == null)
                    return null;

                // check for a record in options, that matches Submodel
                var found = false;
                // ReSharper disable once UnusedVariable
                foreach (var rec in _options.LookupAllIndexKey<BomStructureOptionsRecord>(
                    sm.SemanticId?.GetAsExactlyOneKey()))
                    found = true;
                if (!found)
                    return null;

                // success prepare record
                var cve = new AasxPluginResultVisualExtension("BOM", "Bill of Material - Graph display");

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
                lic.shortLicense =
                    "The Microsoft Microsoft Automatic Graph Layout, MSAGL, is licensed under the MIT license (MIT).";

                lic.isStandardLicense = true;
                lic.longLicense = AasxPluginHelper.LoadLicenseTxtFromAssemblyDir(
                    "LICENSE.txt", Assembly.GetExecutingAssembly());

                return lic;
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

            if (action == "get-menu-items")
            {
                // result list 
                var res = new List<AasxPluginResultSingleMenuItem>();

                // view package relations
                res.Add(new AasxPluginResultSingleMenuItem()
                {
                    AttachPoint = "Visualize",
                    MenuItem = new AasxMenuItem()
                    {
                        Name = "ViewPackageRelations",
                        Header = "Visualize package relations …",
                        HelpText = "Visualize all relations of SME elements in a package."
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
                        if (cmd == "viewpackagerelations")
                        {
                            await Task.Yield();

                            // call
                            this._bomControl.SetEventStack(this._eventStack);
                            masterPanel?.Children?.Clear();
                            var resobj = this._bomControl.CreateViewPackageReleations(_options, ticket.Package, masterPanel);

                            // give object back
                            var res = new AasxPluginResultCallMenuItem();
                            res.RenderWpfContent = resobj;
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
