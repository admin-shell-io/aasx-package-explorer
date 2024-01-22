/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase.AdminShellEvents;
using AnyUi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aas = AasCore.Aas3_0;

// ReSharper disable ClassNeverInstantiated.Global

namespace AasxIntegrationBase
{
    public class AasxPluginActionDescriptionBase
    {
        public string name = "";
        public string info = "";

        /// <summary>
        /// If <c>true</c> will use <c>ActivateActionAsync</c> to call the plugin in async mode.
        /// </summary>
        public bool UseAsync = false;

        public AasxPluginActionDescriptionBase()
        {
        }

        public AasxPluginActionDescriptionBase(string name, string info, bool useAsync = false)
        {
            this.name = name;
            this.info = info;
            UseAsync = useAsync;
        }
    }

    public class AasxPluginListOfActionDescription : List<AasxPluginActionDescriptionBase>
    {
        public AasxPluginListOfActionDescription AddAction(
            AasxPluginActionDescriptionBase item)
        {
            this.Add(item);
            return this;
        }

        public AasxPluginListOfActionDescription AddAction(
            string name, string info, bool useAsync = false)
        {
            this.Add(new AasxPluginActionDescriptionBase(name, info, useAsync));
            return this;
        }
    }

    public class AasxPluginResultBase
    {
    }

    public class AasxPluginResultBaseObject : AasxPluginResultBase
    {
        public string strType = "";
        public object obj = null;

        public AasxPluginResultBaseObject() { }
        public AasxPluginResultBaseObject(string strType, object obj)
        {
            this.strType = strType;
            this.obj = obj;
        }
    }

    public class AasxPluginResultCallMenuItem : AasxPluginResultBase
    {
        public object RenderWpfContent = null;
    }

    public class AasxPluginResultGenerateSubmodel : AasxPluginResultBase
    {
        public Aas.Submodel sm;
        public List<Aas.ConceptDescription> cds;
    }

	public class AasxPluginResultSingleMenuItem
    {
        /// <summary>
        /// Identifies, where in the hierarchy of the application's main menu
        /// this new menu item shall be attached to. Designates a named 
        /// <c>AasxMenu</c>.
        /// </summary>
        public string AttachPoint;

        /// <summary>
        /// The new menu item including name, header, may be hotkey and 
        /// argument description.
        /// </summary>
        public AasxMenuItem MenuItem;
    }

    public class AasxPluginResultProvideMenuItems : AasxPluginResultBase
    {
        public List<AasxPluginResultSingleMenuItem> MenuItems;
    }

    public interface IPushApplicationEvent
    {
        void PushApplicationEvent(AasxIntegrationBase.AasxPluginResultEventBase evt);
    }

    public class AasxPluginResultEventBase : AasxPluginResultBase
    {
        public PluginSessionBase Session;
        public string info = null;
    }

    public class AasxPluginResultEventNavigateToReference : AasxPluginResultEventBase
    {
        public Aas.IReference targetReference = null;
    }

	public class AasxPluginResultEventVisualSelectEntities : AasxPluginResultEventBase
	{
		public List<Aas.IReferable> Referables = null;
	}

	public class AasxPluginResultEventDisplayContentFile : AasxPluginResultEventBase
    {
        public string fn = null;
        public string mimeType = null;
        public bool preferInternalDisplay = false;

        public bool SaveInsteadDisplay = false;
        public string ProposeFn = null;
    }

    public class AasxPluginResultEventRedrawAllElements : AasxPluginResultEventBase
    {
    }

    public class AasxPluginResultEventSelectAasEntity : AasxPluginResultEventBase
    {
        public string filterEntities = null;
        public bool showAuxPackage = false;
        public bool showRepoFiles = false;
    }

    public class AasxPluginResultEventSelectFile : AasxPluginResultEventBase
    {
        public bool SaveDialogue = false;
        public string Title = null;
        public string FileName = null;
        public string DefaultExt = null;
        public string Filter = null;
        public bool MultiSelect = false;
    }

    public class AasxPluginResultEventMessageBox : AasxPluginResultEventBase
    {
        public string Caption = "Question";
        public string Message = "";
        public AnyUiMessageBoxButton Buttons = AnyUiMessageBoxButton.YesNoCancel;
        public AnyUiMessageBoxImage Image = AnyUiMessageBoxImage.None;
    }

	public class AasxPluginResultEventInvokeOtherPlugin : AasxPluginResultEventBase
	{
		public string PluginName = "";
		public string Action = "";
        public bool UseAsync = false;
        public object[] Args = null;
	}

    /// <summary>
    /// This plugin result event shall be sent to the host in order to push
    /// AAS events, such as updates.
    /// </summary>
    public class AasxPluginResultEventPushSomeEvents : AasxPluginResultEventBase
    {
        public List<AasEventMsgEnvelope> AasEvents = null;
        public List<Aas.ISubmodelElement> AnimateSingleEvents = null;
    }

    public class AasxPluginEventReturnBase
    {
        public AasxPluginResultEventBase sourceEvent = null;
    }

    public class AasxPluginEventReturnSelectAasEntity : AasxPluginEventReturnBase
    {
        public List<Aas.IKey> resultKeys = null;
    }

    public class AasxPluginEventReturnSelectFile : AasxPluginEventReturnBase
    {
        public string[] FileNames;
    }

    public class AasxPluginEventReturnMessageBox : AasxPluginEventReturnBase
    {
        public AnyUiMessageBoxResult Result = AnyUiMessageBoxResult.None;
    }

	public class AasxPluginEventReturnInvokeOther : AasxPluginEventReturnBase
	{
        public object ResultData = null;
	}

	public class AasxPluginEventReturnUpdateAnyUi : AasxPluginResultEventBase
    {
        public string PluginName = "";
        public AnyUiRenderMode Mode = AnyUiRenderMode.All;
        public bool UseInnerGrid = false;
    }

    public class AasxPluginResultLicense : AasxPluginResultBase
    {
        /// <summary>
        /// License string (with <c>Environment.Newlines</c>) to be displayed in the splash screen
        /// </summary>
        public string shortLicense = null;

        /// <summary>
        /// License text (with <c>Environment.Newlines</c>) to be displayed in About box.
        /// </summary>
        public string longLicense = null;

        /// <summary>
        /// Set this to <c>true</c>, if the provided <c>longLicense</c> is identical to the LICENSE.txt
        /// of the main application (AasxPackageExplorer)
        /// </summary>
        public bool isStandardLicense = false;
    }

    public class AasxPluginResultVisualExtension : AasxPluginResultBase
    {
        public string Tag = "";
        public string Caption = "";
        // 1. object = any result data (null means error),
        // 2. object = Package, 3. object = Referable, 4. object = master dock panel to insert in
        public Func<object, object, object, object> FillWithWpfControls = null;
        public AasxPluginResultVisualExtension() { }

        public AasxPluginResultVisualExtension(string tag, string caption)
        {
            this.Tag = tag;
            this.Caption = caption;
        }
    }

    public class AasxPluginVisualElementExtension
    {
        public string Tag = "";
        public string Caption = "";
        // 1. object = any result data (null means error), 2. object = Package, 3. object = Referable,
        // 4. object = master dock panel to insert in
        public Func<object, object, object, object> FillWithWpfControls = null;
        public AasxPluginVisualElementExtension() { }

        public AasxPluginVisualElementExtension(string tag, string caption)
        {
            this.Tag = tag;
            this.Caption = caption;
        }
    }

    public interface IAasxPluginInterface
    {
        /// <summary>
        /// The plug-in reports its unique name
        /// </summary>
        string GetPluginName();

        /// <summary>
        /// The plug-in gets initialized (once) with an array of arguments
        /// </summary>
        void InitPlugin(string[] args);

        /// <summary>
        /// The plug-in gives back log message.
        /// </summary>
        /// <returns>One string per log message, null else. Either string or StoredPrint.</returns>
        object CheckForLogMessage();

        /// <summary>
        /// The plug-in describes possible actions
        /// </summary>
        AasxPluginActionDescriptionBase[] ListActions();

        /// <summary>
        /// Activate a specific action.
        /// </summary>
        /// <param name="action">Name of the action as describe in AasxPluginActionDescriptionBase record</param>
        /// <param name="args">Array of arguments. Will be checked and type-casted by the plugin</param>
        /// <returns>Any result to be derived from AasxPluginResultBase</returns>
        AasxPluginResultBase ActivateAction(string action, params object[] args);

        /// <summary>
        /// Activate a specific action. Async variant.
        /// Note: for some reason of type conversion, it has to return <c>Task<object></c>.
        /// </summary>
        /// <param name="action">Name of the action as describe in AasxPluginActionDescriptionBase record</param>
        /// <param name="args">Array of arguments. Will be checked and type-casted by the plugin</param>
        /// <returns>Any result to be derived from AasxPluginResultBase</returns>
        // dead-csharp off
        // Task<object> ActivateActionAsync(string action, params object[] args);
        // dead-csharp on
    }

    /// <summary>
    /// Base class for plugin session data (HTML/ Blazor might host multiple sessions at the same time)
    /// </summary>
    public class PluginSessionBase
    {
        public object SessionId;
    }

    /// <summary>
    /// Services to maintain session sefficiently
    /// </summary>
    public class PluginSessionCollection : Dictionary<object, PluginSessionBase>
    {
        public T CreateNewSession<T>(object sessionId)
            where T : PluginSessionBase, new()
        {
            if (this.ContainsKey(sessionId))
                this.Remove(sessionId);
            var res = new T() { SessionId = sessionId };
            this.Add(sessionId, res);
            return res;
        }

        public T FindSession<T>(object sessionId)
            where T : PluginSessionBase, new()
        {
            if (this.ContainsKey(sessionId))
                return this[sessionId] as T;
            return null;
        }

        public bool AccessSession<T>(object sessionId, out T session)
            where T : PluginSessionBase, new()
        {
            session = null;
            if (this.ContainsKey(sessionId))
                session = this[sessionId] as T;
            return session != null;
        }
    }

    public class AasxPluginBase : IAasxPluginInterface
    {
        protected LogInstance _log = new LogInstance();
        protected PluginEventStack _eventStack = new PluginEventStack();
        protected PluginSessionCollection _sessions = new PluginSessionCollection();

        protected string _pluginName = "(not initialized)";

        public string PluginName { get { return _pluginName; } set { _pluginName = value; } }

        public string GetPluginName()
        {
            return PluginName;
        }

        public void InitPlugin(string[] args)
        {
            throw new NotImplementedException();
        }

        public AasxPluginResultBase ActivateAction(string action, params object[] args)
        {
            throw new NotImplementedException();
        }

        public async Task<object> ActivateActionAsync(string action, params object[] args)
        {
            await Task.Yield();
            throw new NotImplementedException();
        }

        public object CheckForLogMessage()
        {
            return _log?.PopLastShortTermPrint();
        }

        public AasxPluginActionDescriptionBase[] ListActions()
        {
            throw new NotImplementedException();
        }

        public AasxPluginListOfActionDescription ListActionsBasicHelper(
            bool enableCheckVisualExt,
            bool enableOptions = true,
            bool enableLicenses = true,
            bool enableEventsGet = false,
            bool enableEventReturn = false,
            bool enablePresetsGet = false,
            bool enableMenuItems = false,
            bool enablePanelWpf = false,
            bool enablePanelAnyUi = false,
            bool enableNewSubmodel = false)
        {
            var res = new AasxPluginListOfActionDescription();

            // for speed reasons, have the most often used at top!
            if (enableCheckVisualExt)
                res.Add(new AasxPluginActionDescriptionBase(
                    "call-check-visual-extension",
                    "When called with Referable, returns possibly visual extension for it."));
            // rest follows
            if (enableOptions)
            {
                res.Add(new AasxPluginActionDescriptionBase(
                    "set-json-options", "Sets plugin-options according to provided JSON string."));
                res.Add(new AasxPluginActionDescriptionBase(
                    "get-json-options", "Gets plugin-options as a JSON string."));
            }
            if (enableLicenses)
                res.Add(new AasxPluginActionDescriptionBase(
                    "get-licenses", "Reports about used licenses."));
            if (enableEventsGet)
                res.Add(new AasxPluginActionDescriptionBase(
                    "get-events", "Pops and returns the earliest event from the event stack."));
            if (enableEventReturn)
                res.Add(new AasxPluginActionDescriptionBase(
                    "event-return", "Called to return a result evaluated by the host for a certain event."));
            if (enablePresetsGet)
                res.Add(new AasxPluginActionDescriptionBase(
                    "get-presets", "Provides options/ preset data of plugin to caller."));
            if (enableMenuItems)
            {
                res.Add(new AasxPluginActionDescriptionBase(
                    "get-menu-items", "Provides a list of menu items of the plugin to the caller."));
                res.Add(new AasxPluginActionDescriptionBase(
                    "call-menu-item", "Caller activates a named menu item.", useAsync: true));
            }
            if (enableCheckVisualExt)
                res.Add(new AasxPluginActionDescriptionBase(
                "get-check-visual-extension", "Returns true, if plug-ins checks for visual extension."));
            if (enablePanelWpf)
                res.Add(new AasxPluginActionDescriptionBase(
                    "fill-panel-visual-extension",
                    "When called, fill given WPF panel with control for graph display."));
            if (enablePanelAnyUi)
            {
                res.Add(new AasxPluginActionDescriptionBase(
                    "fill-anyui-visual-extension",
                    "When called, fill given AnyUI panel with control for graph display."));
                res.Add(new AasxPluginActionDescriptionBase(
                    "update-anyui-visual-extension",
                    "When called, updated already presented AnyUI panel with some arguments."));
                res.Add(new AasxPluginActionDescriptionBase(
                    "dispose-anyui-visual-extension",
                    "When called, will dispose the plugin data associated with given session id."));
            }
            if (enableNewSubmodel)
            {
                res.Add(new AasxPluginActionDescriptionBase(
                    "get-list-new-submodel",
                    "Returns a list of speaking names of Submodels, which could be generated by the plugin."));
                res.Add(new AasxPluginActionDescriptionBase(
                    "generate-submodel",
                    "Returns a generated default Submodel based on the name provided as string argument."));
            }

            return res;
        }

        /// <summary>
        /// Tries to provide default activate actions functionality.
        /// </summary>
        /// <returns>Result not null means, helper was sucessfull</returns>
        public AasxPluginResultBase ActivateActionBasicHelper<T>(
            string action, ref T options, object[] args,
            bool disableDefaultLicense = false,
            bool enableGetCheckVisuExt = false)
            where T : AasxPluginOptionsBase
        {
            if (action == "set-json-options" && args != null && args.Length >= 1 && args[0] is string)
            {
                var newOpt = JsonConvert.DeserializeObject<T>(
                    args[0] as string);
                if (newOpt != null)
                    options = newOpt;
            }

            if (action == "get-json-options")
            {
                // need to care about AAS serialization of enums
                // see: https://stackoverflow.com/questions/2441290/
                // javascriptserializer-json-serialization-of-enum-as-string
                var json = JsonConvert.SerializeObject(options, Newtonsoft.Json.Formatting.Indented,
                    new Newtonsoft.Json.Converters.StringEnumConverter());
                return new AasxPluginResultBaseObject("OK", json);
            }

            if (!disableDefaultLicense && action == "get-licenses")
            {
                var lic = new AasxPluginResultLicense();
                lic.shortLicense = "";
                lic.longLicense = "";
                lic.isStandardLicense = true;

                return lic;
            }

            if (action == "get-events" && _eventStack != null)
            {
                // try access
                return _eventStack.PopEvent();
            }

            if (enableGetCheckVisuExt && action == "get-check-visual-extension")
            {
                var cve = new AasxPluginResultBaseObject();
                cve.strType = "True";
                cve.obj = true;
                return cve;
            }

            return null;
        }
    }

    public enum PluginOperationDisplayMode { NoDisplay, JustDisplay, MayEdit, MayAddEdit }

    /// <summary>
    /// Provides some context for the operatioln of the plugin. What kind of overall
    /// behaviour is expected? Editing allowed? Display options?
    /// </summary>
    public class PluginOperationContextBase
    {
        public PluginOperationDisplayMode DisplayMode;

        public bool IsDisplayModeEditOrAdd =>
            DisplayMode == PluginOperationDisplayMode.MayEdit
            || DisplayMode == PluginOperationDisplayMode.MayAddEdit;
    }
}
