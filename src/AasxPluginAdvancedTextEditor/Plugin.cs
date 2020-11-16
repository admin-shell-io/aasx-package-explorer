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

/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

The Newtonsoft.JSON serialization is licensed under the MIT License (MIT).

The AvalonEdit component, is licensed under the MIT license (MIT).
*/

namespace AasxIntegrationBase // the namespace has to be: AasxIntegrationBase
{
    [UsedImplicitlyAttribute]
    // the class names has to be: AasxPlugin and subclassing IAasxPluginInterface
    public class AasxPlugin : IAasxPluginInterface
    {
        private LogInstance Log = new LogInstance();
        private PluginEventStack eventStack = new PluginEventStack();
        private AdvancedTextEditOptions options =
            new AdvancedTextEditOptions();

        private UserControlAdvancedTextEditor theEditControl = null;

        public string GetPluginName()
        {
            return "AasxPluginAdvancedTextEditor";
        }

        public void InitPlugin(string[] args)
        {
            // start ..
            Log.Info("InitPlugin() called with args = {0}", (args == null) ? "" : string.Join(", ", args));

            // .. with built-in options
            options = AdvancedTextEditOptions.CreateDefault();

            // try load defaults options from assy directory
            try
            {
                var newOpt =
                    AasxPluginOptionsBase.LoadDefaultOptionsFromAssemblyDir<AdvancedTextEditOptions>(
                        this.GetPluginName(), Assembly.GetExecutingAssembly());
                if (newOpt != null)
                    this.options = newOpt;
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
                    "get-textedit-control", "Returns a new instance of a Text editor control."));
            res.Add(new AasxPluginActionDescriptionBase("set-content", "Sets mime type and content string."));
            res.Add(new AasxPluginActionDescriptionBase("get-content", "Gets content string."));
            return res.ToArray();
        }

        public AasxPluginResultBase ActivateAction(string action, params object[] args)
        {
            if (action == "set-json-options" && args != null && args.Length >= 1 && args[0] is string)
            {
                var newOpt = Newtonsoft.Json.JsonConvert.DeserializeObject<AdvancedTextEditOptions>(
                    (args[0] as string));
                if (newOpt != null)
                    this.options = newOpt;
            }

            if (action == "get-json-options")
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(
                    this.options, Newtonsoft.Json.Formatting.Indented);
                return new AasxPluginResultBaseObject("OK", json);
            }

            if (action == "get-licenses")
            {
                var lic = new AasxPluginResultLicense();
                lic.shortLicense = "The AvalonEdit component, is licensed under the MIT license (MIT).";

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
