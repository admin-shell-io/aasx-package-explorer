/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AasxIntegrationBase
{
    /// <summary>
    /// AASX menu items might be available in multiple applications. 
    /// </summary>
    public enum AasxMenuFilter
    {
        None = 0x00,
        WPF = 0x01, Blazor = 0x02, Toolkit = 0x04,
        WpfBlazor = 0x03,
        All = 0x07
    }

    /// <summary>
    /// AASX menu items might call this action when activated.
    /// </summary>
    /// <param name="nameLower">Name of menu item in lower case</param>
    public delegate void AasxMenuActionDelegate(string nameLower);

    /// <summary>
    /// AASX menu items might call this action when activated.
    /// </summary>
    /// <param name="nameLower">Name of menu item in lower case</param>
    public delegate Task AasxMenuActionAsyncDelegate(string nameLower);

    /// <summary>
    /// Base class for menuitems.
    /// </summary>
    public abstract class AasxMenuItemBase
    {
        /// <summary>
        /// Name of the menu item. Relevant. Will be used to differentiate
        /// in actions.
        /// </summary>
        public string Name = "";

        /// <summary>
        /// The action to be activated.
        /// </summary>
        public AasxMenuActionDelegate Action = null;

        /// <summary>
        /// The action to be activated.
        /// </summary>
        public AasxMenuActionAsyncDelegate ActionAsync = null;
    }

    /// <summary>
    /// By this information, menu items for AASX Package Explorer, Blazor and Toolkit
    /// shall be described. Goal is to build up menu systems dynamically.
    /// Shall be also usable for plugins.
    /// </summary>
    public class AasxMenuItem : AasxMenuItemBase
    {
        /// <summary>
        /// For which application is this menu item applicable.
        /// </summary>
        public AasxMenuFilter Filter = AasxMenuFilter.All;

        /// <summary>
        /// Displayed header in GUI based applications.
        /// </summary>
        public string Header = "";

        /// <summary>
        /// Icon to be place aside the header in GUI based applications.
        /// Might contain unicode symbil, text or bitmap.
        /// </summary>
        public object Icon = null;

        /// <summary>
        /// Can be switched to checked or not
        /// </summary>
        public bool IsCheckable = false;

        /// <summary>
        /// Switch state to initailize with.
        /// </summary>
        public bool IsChecked = false;

        /// <summary>
        /// Keyboard shortcut in GUI based applications.
        /// </summary>
        public string InputGesture = null;

        /// <summary>
        /// Command name in command line based applications. Typically lower case with
        /// dashes in between. No dask at front.
        /// </summary>
        public string CmdLine = null;

        /// <summary>
        /// Help text or description in command line applications.
        /// </summary>
        public string HelpText = null;

        /// <summary>
        /// Sub menues
        /// </summary>
        public AasxMenu Childs = null;

        //
        // Constructors
        //

        public AasxMenuItem()
        {
        }
        
        //
        // more
        //

        public void Add(AasxMenuItemBase item)
        {
            Childs.Add(item);
        }
    }


    /// <summary>
    /// Represents a single hotkey which could activate an action
    /// </summary>
    public class AasxHotkey : AasxMenuItemBase
    {
        /// <summary>
        /// Contains the gesture in form of "Ctrl+Shift+F" stuff.
        /// see: https://learn.microsoft.com/en-us/dotnet/api/system.windows.input.keygesture
        /// </summary>
        public string Gesture;
    }

    /// <summary>
    /// Holds a list of menu items, e.g. representing the main menu.
    /// </summary>
    public class AasxMenu : List<AasxMenuItemBase>
    {
        //
        // Members
        //

        /// <summary>
        /// The action to be activated, if no action is set by single items.
        /// </summary>
        public AasxMenuActionDelegate DefaultAction = null;

        /// <summary>
        /// The action to be activated, if no action is set by single items.
        /// </summary>
        public AasxMenuActionAsyncDelegate DefaultActionAsync = null;


        //
        // Creators (here, because class name is shorter)
        //

        public AasxMenu AddWpf(
            string name, string header,
            AasxMenuActionDelegate action = null,
            AasxMenuActionAsyncDelegate actionAsync = null,
            AasxMenuFilter filter = AasxMenuFilter.WPF,
            string inputGesture = null,
            bool isCheckable = false, bool isChecked = false)
        {
            this.Add(new AasxMenuItem()
            {
                Name = name,
                Header = header,
                Action = action,
                ActionAsync = actionAsync,
                Filter = filter,
                InputGesture = inputGesture,
                IsCheckable = isCheckable,
                IsChecked = isChecked
            });
            return this;
        }

        public AasxMenu AddWpfBlazor(
            string name, string header,
            AasxMenuActionDelegate action = null,
            AasxMenuActionAsyncDelegate actionAsync = null,
            AasxMenuFilter filter = AasxMenuFilter.WpfBlazor,
            string inputGesture = null,
            bool isCheckable = false, bool isChecked = false)
        {
            this.Add(new AasxMenuItem()
            {
                Name = name,
                Header = header,
                Action = action,
                ActionAsync = actionAsync,
                Filter = filter,
                InputGesture = inputGesture,
                IsCheckable = isCheckable,
                IsChecked = isChecked
            });
            return this;
        }

        public AasxMenu AddAll(
            string name, string header,
            string cmd, string help,
            AasxMenuActionDelegate action = null,
            AasxMenuActionAsyncDelegate actionAsync = null,
            AasxMenuFilter filter = AasxMenuFilter.All,
            string inputGesture = null)
        {
            this.Add(new AasxMenuItem()
            {
                Name = name,
                Header = header,
                CmdLine = cmd,
                HelpText = header,
                Action = action,
                ActionAsync = actionAsync,
                Filter = filter,
                InputGesture = inputGesture
            });
            return this;
        }

        public AasxMenu AddSeparator()
        {
            this.Add(new AasxMenuSeparator());
            return this;
        }

        public AasxMenu AddMenu(
            string header,
            AasxMenuFilter filter = AasxMenuFilter.WpfBlazor,
            AasxMenu childs = null)
        {
            this.Add(new AasxMenuItem()
            {
                Header = header,
                Filter = filter,
                Childs = childs
            });
            return this;
        }

        public AasxMenu AddHotkey(
            string name,
            string gesture)
        {
            this.Add(new AasxHotkey()
            {
                Name = name,
                Gesture = gesture
            });
            return this;
        }

        //
        // Operate
        //

        public void ActivateAction(AasxMenuItem mii)
        {
            var name = mii?.Name?.Trim()?.ToLower();

            if (mii?.ActionAsync != null)
                mii.ActionAsync(name);
            else if (mii?.Action != null)
                mii.Action(name);
            else if (this.DefaultActionAsync != null)
                this.DefaultActionAsync(name);
            else if (this.DefaultAction != null)
                this.DefaultAction(name);
        }

    }

    /// <summary>
    /// To be used for separators
    /// </summary>
    public class AasxMenuSeparator : AasxMenuItemBase
    {
    }

}
