/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AnyUi;
using AasxIntegrationBase;
using AdminShellNS;
using System.Globalization;
using System.Reflection;
using System.Linq;

namespace AnyUi
{
    /// <summary>
    /// This attribute controls the automatic generation of some edit fields
    /// in abstract AnyUI <c>DisplayContext</c>.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = true)]
    public class AnyUiEditField : System.Attribute
    {
        /// <summary>
        /// If set, header (key) in a UI dialogue.
        /// </summary>
        public string UiHeader = null;

        /// <summary>
        /// If true, add help texts in a additional column
        /// </summary>
        public bool UiShowHelp = false;

        /// <summary>
        /// If true, will group edit field and help directly after each other
        /// </summary>
        public bool UiGroupHelp = false;

        /// <summary>
        /// If not <c>null</c>, will restrict the minimum length of the edit field.
        /// Automatically disable "stretch" behavior of the field.
        /// </summary>
        public int? UiMinWidth = null;

        /// <summary>
        /// If not <c>null</c>, will restrict the maximum length of the edit field.
        /// Automatically disable "stretch" behavior of the field.
        /// </summary>
        public int? UiMaxWidth = null;

        public AnyUiEditField(
            string uiHeader = null, bool uiShowHelp = false,
            bool uiGroupHelp = false,
            int minWidth = -1, int maxWidth = -1)
        {
            UiHeader = uiHeader;
            UiShowHelp = uiShowHelp;
            UiGroupHelp = uiGroupHelp;
            if (minWidth >= 0.0)
                UiMinWidth = minWidth;
            if (maxWidth >= 0.0)
                UiMaxWidth = maxWidth;
        }
    }

    public enum AnyUiContextCapability
    {
        /// <summary>
        /// Display is a WPF application. 
        /// Allows hardcoded behaviour. It is recommended to use
        /// other capablities instead.
        /// </summary>
        WPF,

        /// <summary>
        /// Display is a Blazor application. 
        /// Allows hardcoded behaviour. It is recommended to use
        /// other capablities instead.
        /// </summary>
        Blazor,

        /// <summary>
        /// Display can perform open/save dialogs and further without
        /// utilizing the singleton modal flyover.
        /// Note: display context allows only for one level of
        /// modal / flyover dialogues. NO stacking of dialogues.
        /// </summary>
        DialogWithoutFlyover
    };

    /// <summary>
    /// This class extends the base context with more functions for handling 
    /// convenience dialogues.
    /// </summary>
    public class AnyUiContextPlusDialogs : AnyUiContextBase
    {
        /// <summary>
        /// Enumerates all given (positive) capabilities.
        /// </summary>
        public virtual IEnumerable<AnyUiContextCapability> EnumCapablities()
        {
            yield break;
        }

        /// <summary>
        /// Checks if a particular capability is given.
        /// </summary>
        public virtual bool HasCapability(AnyUiContextCapability capa)
        {
            return EnumCapablities().Contains(capa);
        }

        /// <summary>
        /// Used to hold the directory the user was using the last time
        /// </summary>
        /// <param name="fn"></param>
        public void RememberForInitialDirectory(string fn)
        {
            lastFnForInitialDirectory = fn;
        }
        protected static string lastFnForInitialDirectory = null;

        /// <summary>
        /// Selects a filename to read either from user or from ticket.
        /// </summary>
        /// <returns>The dialog data containing the filename or <c>null</c></returns>
        public virtual async Task<AnyUiDialogueDataOpenFile> MenuSelectOpenFilenameAsync(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeFn,
            string filter,
            string msg,
            bool requireNoFlyout = false)
        {
            await Task.Yield();
            return null;
        }

        /// <summary>
        /// If ticket does not contain the filename named by <c>argName</c>,
        /// read it by the user.
        /// </summary>
        public virtual async Task<bool> MenuSelectOpenFilenameToTicketAsync(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeFn,
            string filter,
            string msg)
        {
            await Task.Yield();
            return false;
        }

        /// <summary>
        /// Selects a filename to write either from user or from ticket.
        /// </summary>
        /// <returns>The dialog data containing the filename or <c>null</c></returns>
        public virtual async Task<AnyUiDialogueDataSaveFile> MenuSelectSaveFilenameAsync(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeFn,
            string filter,
            string msg,
            bool requireNoFlyout = false,
            bool reworkSpecialFn = false)
        {
            await Task.Yield();
            return null;
        }

        /// <summary>
        /// If ticket does not contain the filename named by <c>argName</c>,
        /// read it by the user.
        /// </summary>
        public virtual async Task<bool> MenuSelectSaveFilenameToTicketAsync(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeFn,
            string filter,
            string msg,
            string argFilterIndex = null,
            string argLocation = null,
            bool reworkSpecialFn = false)
        {
            await Task.Yield();
            return false;
        }

        /// <summary>
        /// Selects a text either from user or from ticket.
        /// </summary>
        /// <returns>Success</returns>
        public virtual async Task<AnyUiDialogueDataTextBox> MenuSelectTextAsync(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeText,
            string msg)
        {
            await Task.Yield();
            return null;
        }

        /// <summary>
        /// Selects a text either from user or from ticket.
        /// </summary>
        /// <returns>Success</returns>
        public virtual async Task<bool> MenuSelectTextToTicketAsync(
            AasxMenuActionTicket ticket,
            string argName,
            string caption,
            string proposeText,
            string msg)
        {
            await Task.Yield();
            return false;
        }

        /// <summary>
        /// The display context tells, if user files are allowable for the application
        /// </summary>
        public virtual bool UserFilesAllowed()
        {
            return false;
        }

        /// <summary>
        /// The display context tells, if web browser services are allowable for the application.
        /// These are: download files, open new browser window ..
        /// </summary>
        public virtual bool WebBrowserServicesAllowed()
        {
            return false;
        }

        /// <summary>
        /// Initiate a download within the web browser
        /// </summary>
        public virtual async Task WebBrowserDisplayOrDownloadFile(string fn, string mimeType = null)
        {
            await Task.Yield();
            return;
        }

        public async Task CheckIfDownloadAndStart(
            LogInstance log,
            object location,
            string fn,
            string contentType = "application/octet-stream")
        {
            if (location is string ls
                && ls.Equals(AnyUiDialogueDataSaveFile.LocationKind.Download.ToString(),
                    StringComparison.InvariantCultureIgnoreCase)
                && WebBrowserServicesAllowed())
            {
                try
                {
                    await WebBrowserDisplayOrDownloadFile(fn, contentType);
                    log?.Info("Download initiated.");
                }
                catch (Exception ex)
                {
                    log?.Error(
                        ex, $"When downloading saved file");
                    return;
                }
            }
        }

        public async Task CheckIfDownloadAndStart(
            AasxMenuActionTicket ticket,
            LogInstance log,
            string argFileName,
            string argLocation,
            string contentType = "application/octet-stream")
        {
            // access
            if (ticket == null
                || !(ticket[argFileName] is string fn)
                || !(ticket[argLocation] is string location))
                return;

            await CheckIfDownloadAndStart(log, location, fn, contentType);
        }

        /// <summary>
        /// This function add single rows to a given grid (2 or 3 columns) having
        /// header, edit field and help text for a series of data fields given in
        /// <c>data</c> via the attribute <c>AasxMenuArgument</c>
        /// </summary>
        public bool AutoGenerateUiFieldsFor(
            object data, AnyUiSmallWidgetToolkit helper,
            AnyUiGrid grid, int startRow = 0)
        {
            // access
            if (data == null || helper == null || grid == null)
                return false;

            int row = startRow;

            // find fields for this object
            var t = data.GetType();
            var l = t.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var f in l)
            {
                var attrMenuArg = f.GetCustomAttribute<AasxMenuArgument>();
                var attrEditField = f.GetCustomAttribute<AnyUiEditField>();
                if (attrEditField != null)
                {
                    // access here
                    if (attrEditField.UiHeader?.HasContent() != true)
                        continue;

                    // some more layout options
                    var gridToAdd = grid;
                    var grpHelp = attrEditField.UiGroupHelp;
                    var hAlign = AnyUiHorizontalAlignment.Stretch;
                    int? minWidth = null;
                    int? maxWidth = null;
                    int helpGap = 0;
                    if (attrEditField.UiMinWidth.HasValue)
                    {
                        grpHelp = true;
                        minWidth = attrEditField.UiMinWidth.Value;
                    }
                    if (attrEditField.UiMaxWidth.HasValue)
                    {
                        grpHelp = true;
                        maxWidth = attrEditField.UiMinWidth.Value;
                    }
                    if (grpHelp)
                    {
                        hAlign = AnyUiHorizontalAlignment.Left;
                        helpGap = 10;
                        gridToAdd = helper.AddSmallGridTo(grid, row, 1, 1, 3, new[] { "0:", "#", "*" });
                    }

                    // string
                    if (f.FieldType == typeof(string)
                        && f.GetValue(data) is string strVal)
                    {
                        AnyUiUIElement.SetStringFromControl(
                            helper.Set(
                                helper.AddSmallTextBoxTo(gridToAdd, row, 1,
                                    margin: new AnyUiThickness(0, 2, 2, 2),
                                    text: "" + strVal,
                                    verticalAlignment: AnyUiVerticalAlignment.Center,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                    minWidth: minWidth, maxWidth: maxWidth,
                                    horizontalAlignment: hAlign),
                            (i) =>
                            {
                                AdminShellUtil.SetFieldLazyValue(f, data, i);
                            });
                    }
                    else
                    if (f.FieldType == typeof(bool)
                        && f.GetValue(data) is bool boolVal)
                    {
                        AnyUiUIElement.SetBoolFromControl(
                            helper.Set(
                                helper.AddSmallCheckBoxTo(gridToAdd, row, 1,
                                    content: "",
                                    isChecked: boolVal,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center)),
                            (b) =>
                            {
                                AdminShellUtil.SetFieldLazyValue(f, data, b);
                            });
                    }
                    else
                    if ((f.FieldType == typeof(byte) || f.FieldType == typeof(sbyte)
                        || f.FieldType == typeof(Int16) || f.FieldType == typeof(Int32)
                        || f.FieldType == typeof(Int64) || f.FieldType == typeof(UInt16)
                        || f.FieldType == typeof(UInt32) || f.FieldType == typeof(UInt64)
                        || f.FieldType == typeof(Single) || f.FieldType == typeof(Double))
                        && f.GetValue(data) is object objVal)
                    {
                        var valStr = objVal.ToString();
                        if (objVal is Single fVal)
                            valStr = fVal.ToString(CultureInfo.InvariantCulture);
                        if (objVal is Double dVal)
                            valStr = dVal.ToString(CultureInfo.InvariantCulture);

                        AnyUiUIElement.RegisterControl(
                            helper.Set(
                                helper.AddSmallTextBoxTo(gridToAdd, row, 1,
                                    margin: new AnyUiThickness(0, 2, 2, 2),
                                    text: "" + valStr,
                                    verticalAlignment: AnyUiVerticalAlignment.Center,
                                    verticalContentAlignment: AnyUiVerticalAlignment.Center),
                                    minWidth: minWidth, maxWidth: maxWidth,
                                    horizontalAlignment: hAlign),
                            setValue: (o) =>
                            {
                                AdminShellUtil.SetFieldLazyValue(f, data, o);
                                return new AnyUiLambdaActionNone();
                            });
                    }
                    else
                    {
                        // if not found, no row
                        continue;
                    }

                    // start the row with the header
                    helper.AddSmallLabelTo(grid, row, 0, content: attrEditField.UiHeader + ":",
                        verticalAlignment: AnyUiVerticalAlignment.Center,
                        verticalContentAlignment: AnyUiVerticalAlignment.Center);

                    // help text
                    if (attrEditField.UiShowHelp && attrMenuArg?.Help?.HasContent() == true)
                    {
                        helper.AddSmallLabelTo(gridToAdd, row, 2, content: "(" + attrMenuArg.Help + ")",
                            margin: new AnyUiThickness(helpGap, 0, 0, 0),
                            setNoWrap: true,
                            verticalAlignment: AnyUiVerticalAlignment.Center,
                            verticalContentAlignment: AnyUiVerticalAlignment.Center);
                    }

                    // advance row
                    row++;
                }
            }

            // OK
            return true;
        }

        /// <summary>
        /// Selects a text either from user or from ticket.
        /// </summary>
        /// <returns>Success</returns>
        public async virtual Task<AnyUiDialogueDataLogMessage> MenuExecuteSystemCommand(
            string caption,
            string workDir,
            string cmd,
            string args)
        {
            await Task.Yield();
            throw new NotImplementedException("MenuExecuteSystemCommand");
        }
    }
}
