/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AdminShellNS;
using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Special information requirement to an AASX menu item
    /// </summary>
    public enum AasxMenuArgReqInfo { 
        None = 0x00,
        AAS = 0x01, 
        Submodel = 0x02, 
        SubmodelRef = 0x04, 
        SME = 0x08,
        SmSmrSme = 0x02 | 0x04 | 0x08
    };

    /// <summary>
    /// AASX menu items will link to functionality, which requires
    /// a set of arguments to executed (automatically). This class
    /// describes the requirements for a particular argument.
    /// </summary>
    public class AasxMenuArgDef
    {
        /// <summary>
        /// Name of the argument. Might be used in script language and
        /// command line. PascalCasing is recommended.
        /// </summary>
        public string Name = "";

        /// <summary>
        /// Helping text for the argument. Might be displayed on the
        /// command line.
        /// </summary>
        public string Help = "";
    }

    /// <summary>
    /// List of such argument definitions.
    /// For future extension.
    /// </summary>
    public class AasxMenuListOfArgDefs : List<AasxMenuArgDef>
    {
        public AasxMenuListOfArgDefs Add(
            string name, string help)
        {
            this.Add(new AasxMenuArgDef() { Name = name, Help = help }); 
            return this;
        }

        public AasxMenuArgDef Find(string name)
        {
            return this
                .FindAll((arg) => arg?.Name?.Trim().ToLower() == name?.Trim().ToLower())
                .FirstOrDefault();
        }
    }

    /// <summary>
    /// AASX menu items might call this action when activated.
    /// </summary>
    /// <param name="nameLower">Name of menu item in lower case</param>
    public delegate void AasxMenuActionDelegate(
        string nameLower, 
        AasxMenuItemBase item,
        AasxMenuActionTicket ticket);

    /// <summary>
    /// AASX menu items might call this action when activated.
    /// </summary>
    /// <param name="nameLower">Name of menu item in lower case</param>
    public delegate Task AasxMenuActionAsyncDelegate(
        string nameLower, 
        AasxMenuItemBase item,
        AasxMenuActionTicket ticket);

    /// <summary>
    /// Base class for menu items with a possible action.
    /// </summary>
    public abstract class AasxMenuItemBase
    {
        /// <summary>
        /// Name of the menu item. Relevant. Will be used to differentiate
        /// in actions.
        /// </summary>
        public string Name = "";

        /// <summary>
        /// List of argument definitions
        /// </summary>
        public AasxMenuListOfArgDefs ArgDefs = null;

        /// <summary>
        /// List of required informations to be available, but
        /// not provided by used-provided arguments.
        /// </summary>
        public AasxMenuArgReqInfo RequiredInfos;

        /// <summary>
        /// The action to be activated.
        /// </summary>
        public AasxMenuActionDelegate Action = null;

        /// <summary>
        /// The action to be activated.
        /// </summary>
        public AasxMenuActionAsyncDelegate ActionAsync = null;

        //
        // Convenience
        //

        public AasxMenuItemBase Set(
            Nullable<AasxMenuArgReqInfo> reqs = null,
            AasxMenuListOfArgDefs args = null)
        {
            if (reqs != null)
                this.RequiredInfos = reqs.Value;
            if (args != null)
                this.ArgDefs = args;
            return this;
        }
    }

    /// <summary>
    /// Menu item equipped with a possible hotkey
    /// </summary>
    public abstract class AasxMenuItemHotkeyed : AasxMenuItemBase
    {
        /// <summary>
        /// Contains the gesture in form of "Ctrl+Shift+F" stuff.
        /// see: https://learn.microsoft.com/en-us/dotnet/api/system.windows.input.keygesture
        /// </summary>
        public string InputGesture;

        /// <summary>
        /// Displays gesture, but not auto-create a hotkey for it
        /// </summary>
        public bool GestureOnlyDisplay = false;
    }

    /// <summary>
    /// By this information, menu items for AASX Package Explorer, Blazor and Toolkit
    /// shall be described. Goal is to build up menu systems dynamically.
    /// Shall be also usable for plugins.
    /// </summary>
    public class AasxMenuItem : AasxMenuItemHotkeyed
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
        /// Switch   state to initailize with.
        /// </summary>
        public bool IsChecked = false;

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
    /// Represents a single hotkey which is not a visible menu item
    /// </summary>
    public class AasxHotkey : AasxMenuItemHotkeyed
    {
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
            string help = null,
            AasxMenuActionDelegate action = null,
            AasxMenuActionAsyncDelegate actionAsync = null,
            AasxMenuFilter filter = AasxMenuFilter.WPF,
            string inputGesture = null,
            bool onlyDisplay = false,
            bool isCheckable = false, bool isChecked = false,
            AasxMenuArgReqInfo reqs = AasxMenuArgReqInfo.None,
            AasxMenuListOfArgDefs args = null)
        {
            this.Add(new AasxMenuItem()
            {
                Name = name,
                Header = header,
                HelpText = help,
                Action = action,
                ActionAsync = actionAsync,
                Filter = filter,
                InputGesture = inputGesture,
                GestureOnlyDisplay = onlyDisplay,
                IsCheckable = isCheckable,
                IsChecked = isChecked,
                RequiredInfos = reqs,
                ArgDefs = args
            });
            return this;
        }

        public AasxMenu AddWpfBlazor(
            string name, string header,
            AasxMenuActionDelegate action = null,
            AasxMenuActionAsyncDelegate actionAsync = null,
            AasxMenuFilter filter = AasxMenuFilter.WpfBlazor,
            string inputGesture = null,
            bool onlyDisplay = false,
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
                GestureOnlyDisplay = onlyDisplay,
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
            string inputGesture = null,
            bool onlyDisplay = false)
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
                InputGesture = inputGesture,
                GestureOnlyDisplay = onlyDisplay,
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
                InputGesture = gesture
            });
            return this;
        }

        //
        // Operate
        //

        public async Task ActivateAction(AasxMenuItemBase mi, AasxMenuActionTicket ticket = null)
        {
            var name = mi?.Name?.Trim()?.ToLower();

            if (mi?.ActionAsync != null)
                await mi.ActionAsync(name, mi, ticket);
            else if (mi?.Action != null)
                mi.Action(name, mi, ticket);
            else if (this.DefaultActionAsync != null)
                await this.DefaultActionAsync(name, mi, ticket);
            else if (this.DefaultAction != null)
                this.DefaultAction(name, mi, ticket);
        }

        //
        // Child management
        //

        public IEnumerable<AasxMenuItemBase> FindAll(Func<AasxMenuItemBase, bool> pred)
        {
            foreach (var ch in this)
            {
                if (pred == null || pred(ch))
                    yield return ch;
                if (ch is AasxMenuItem chmi && chmi.Childs != null)
                    foreach (var x in chmi.Childs.FindAll(pred))
                        yield return x;
            }
        }

        public AasxMenuItemBase FindName(string name)
        {
            return FindAll((i) => i?.Name?.Trim().ToLower() == name?.Trim().ToLower())
                .FirstOrDefault(); ;
        }
    }

    /// <summary>
    /// To be used for separators
    /// </summary>
    public class AasxMenuSeparator : AasxMenuItemBase
    {
    }

    /// <summary>
    /// This class holds a runtime item which is used to invoke a certain action,
    /// transfer parameter assignments and give back action recults.
    /// </summary>
    public class AasxMenuActionTicket
    {
        /// <summary>
        /// The reuqested AASX menu item
        /// </summary>
        public AasxMenuItemBase MenuItem;

        /// <summary>
        /// If set, will not show interactive dialogues but will try to
        /// execute with as much information given as possible.
        /// </summary>
        public bool ScriptMode = false;

        /// <summary>
        /// This dictionary links to the single arg definitions of the menu item
        /// and assign values to it. The invoked action needs to check for value
        /// types.
        /// </summary>
        public Dictionary<AasxMenuArgDef, object> ArgValue = null;

        /// <summary>
        /// Indicates a success or the availablity of an result.
        /// </summary>
        public bool Result = false;

        /// <summary>
        /// If not <c>null</c> indicates the presence of an exception while executing
        /// the action. Should correspond to <c>Result == null</c>.
        /// </summary>
        public string Exception = null;

        //
        // Runtime data
        //

        /// <summary>
        /// Filled by the currently selected element.
        /// </summary>
        public AdminShell.IAasElement SelectedItem;

        /// <summary>
        /// Filled by the currently selected element.
        /// </summary>
        public AdminShell.AdministrationShellEnv Env;

        /// <summary>
        /// Filled by the currently selected element.
        /// </summary>
        public AdminShell.AdministrationShell AAS;

        /// <summary>
        /// Filled by the currently selected element.
        /// </summary>
        public AdminShell.Submodel Submodel;

        /// <summary>
        /// Filled by the currently selected element.
        /// </summary>
        public AdminShell.SubmodelRef SubmodelRef;

        /// <summary>
        /// Filled by the currently selected element.
        /// </summary>
        public AdminShell.SubmodelElement SubmodelElement;

        //
        // Convenience
        //

        public object this[string name]
        {
            get
            {
                if (ArgValue != null)
                    foreach (var av in ArgValue)
                        if (av.Key?.Name?.Trim().ToLower() == name?.Trim().ToLower())
                            return av.Value;
                return null;
            }        
        }

        /// <summary>
        /// Starts execution. For the time being, set <c>Result = true</c>.
        /// </summary>
        public void StartExec()
        {
            Result = true;
        }
    }

}
