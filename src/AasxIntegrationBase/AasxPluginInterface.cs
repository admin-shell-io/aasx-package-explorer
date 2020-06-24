﻿using AdminShellNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable ClassNeverInstantiated.Global


namespace AasxIntegrationBase
{
    public class AasxPluginActionDescriptionBase
    {
        public string name = "";
        public string info = "";

        public AasxPluginActionDescriptionBase()
        {
        }

        public AasxPluginActionDescriptionBase(string name, string info)
        {
            this.name = name;
            this.info = info;
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

    public class AasxPluginResultEventBase : AasxPluginResultBase
    {
        public string info = null;
    }

    public class AasxPluginResultEventNavigateToReference : AasxPluginResultEventBase
    {
        public AdminShell.Reference targetReference = null;
    }

    public class AasxPluginResultEventDisplayContentFile : AasxPluginResultEventBase
    {
        public string fn = null;
        public bool preferInternalDisplay = false;
    }

    public class AasxPluginResultEventRedrawAllElements : AasxPluginResultEventBase
    {
    }

    public class AasxPluginResultEventSelectAasEntity : AasxPluginResultEventBase
    {
        public string filterEntities = null;
    }

    public class AasxPluginEventReturnBase
    {
        public AasxPluginResultEventBase sourceEvent = null;
    }

    public class AasxPluginEventReturnSelectAasEntity : AasxPluginEventReturnBase
    {
        public AdminShell.KeyList resultKeys = null;
    }

    public class AasxPluginResultLicense : AasxPluginResultBase
    {
        public string shortLicense = null;
        public string longLicense = null;
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
    }
}
