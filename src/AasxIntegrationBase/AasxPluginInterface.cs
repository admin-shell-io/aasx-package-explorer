/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using Aas = AasCore.Aas3_0_RC02;
using AnyUi;
using System.Threading.Tasks;

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
        public Aas.Reference targetReference = null;
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

    public class AasxPluginEventReturnBase
    {
        public AasxPluginResultEventBase sourceEvent = null;
    }

    public class AasxPluginEventReturnSelectAasEntity : AasxPluginEventReturnBase
    {
        public List<Aas.Key> resultKeys = null;
    }

    public class AasxPluginEventReturnSelectFile : AasxPluginEventReturnBase
    {
        public string[] FileNames;
    }

    public class AasxPluginEventReturnMessageBox : AasxPluginEventReturnBase
    {
        public AnyUiMessageBoxResult Result = AnyUiMessageBoxResult.None;
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
        // Task<object> ActivateActionAsync(string action, params object[] args);
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
