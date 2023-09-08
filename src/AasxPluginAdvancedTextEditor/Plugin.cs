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
using System.Windows;
using System.Windows.Controls;
using AasxPluginAdvancedTextEditor;
using AdminShellNS;
using JetBrains.Annotations;

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    [UsedImplicitlyAttribute]
    // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    public class AasxPlugin : AasxPluginBase
    {
        protected AdvancedTextEditOptions _options = new AdvancedTextEditOptions();

        private UserControlAdvancedTextEditor theEditControl = null;

        public new void InitPlugin(string[] args)
        {
            // start ..
            PluginName = "AasxPluginAdvancedTextEditor";
            _log.Info("InitPlugin() called with args = {0}", (args == null) ? "" : string.Join(", ", args));

            // .. with built-in options
            _options = AdvancedTextEditOptions.CreateDefault();

            // try load defaults options from assy directory
            try
            {
                var newOpt =
                    AasxPluginOptionsBase.LoadDefaultOptionsFromAssemblyDir<AdvancedTextEditOptions>(
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
            return ListActionsBasicHelper(enableCheckVisualExt: false)
                .AddAction("get-textedit-control", "Returns a new instance of a Text editor control.")
                .AddAction("set-content", "Sets mime type and content string.")
                .AddAction("get-content", "Gets content string.")
                .ToArray();
        }

        public new AasxPluginResultBase ActivateAction(string action, params object[] args)
        {
            // can basic helper help to reduce lines of code?
            var help = ActivateActionBasicHelper(action, ref _options, args,
                disableDefaultLicense: true);
            if (help != null)
                return help;

            // rest follows

            if (action == "get-licenses")
            {
                var lic = new AasxPluginResultLicense();
                lic.shortLicense = "The AvalonEdit component, is licensed under the MIT license (MIT).";

                lic.isStandardLicense = true;
                lic.longLicense = AasxPluginHelper.LoadLicenseTxtFromAssemblyDir(
                    "LICENSE.txt", Assembly.GetExecutingAssembly());

                return lic;
            }

            if (action == "get-textedit-control" && args != null && args.Length >= 1 && args[0] is string)
            {
                // args
                var initialContent = args[0] as string;

                // build visual
                this.theEditControl = new UserControlAdvancedTextEditor();
                this.theEditControl.Text = initialContent;

                // give object back
                var res = new AasxPluginResultBaseObject();
                res.obj = this.theEditControl;
                return res;
            }

            if (action == "set-content" && args != null && args.Length >= 2
                && args[0] is string && args[1] is string
                && this.theEditControl != null)
            {
                // args
                var mimeType = args[0] as string;
                var content = args[1] as string;

                // apply
                this.theEditControl.MimeType = mimeType;
                this.theEditControl.Text = content;
            }

            if (action == "get-content" && this.theEditControl != null)
            {
                // give object back
                var res = new AasxPluginResultBaseObject();
                res.obj = this.theEditControl.Text;
                return res;
            }

            // default
            return null;
        }

    }
}
