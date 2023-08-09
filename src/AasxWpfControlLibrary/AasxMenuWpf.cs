/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AdminShellNS;
using AnyUi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AasxWpfControlLibrary
{
    /// <summary>
    /// This class "converts" the AasxMenu struncture into WPF menues
    /// </summary>
    public class AasxMenuWpf
    {
        //
        // Private
        //
        protected DoubleSidedDict<string, AasxMenuItem> _menuItems
            = new DoubleSidedDict<string, AasxMenuItem>();

        protected DoubleSidedDict<AasxMenuItem, System.Windows.Controls.MenuItem> _wpfItems
            = new DoubleSidedDict<AasxMenuItem, System.Windows.Controls.MenuItem>();

        public AasxMenu Menu { get => _menu; }
        private AasxMenu _menu = new AasxMenu();

        private void RenderItemCollection(
            AasxMenu topMenu, AasxMenu menuItems,
            System.Windows.Controls.ItemCollection wpfItems,
            CommandBindingCollection cmdBindings = null,
            InputBindingCollection inputBindings = null,
            KeyGestureConverter kgConv = null)
        {
            // loop
            foreach (var mi in menuItems)
            {
                // access and trivial stuff
                if (mi == null)
                    continue;

                if (mi is AasxMenuSeparator)
                {
                    // add separator
                    wpfItems.Add(new System.Windows.Controls.Separator());

                    // done
                    continue;
                }

                // create a command and possibly a hotkey
                ICommand cmd = null;
                if (cmdBindings != null && mi?.Name?.HasContent() == true)
                {
                    // remember for lambdas
                    var m_mii = mi;

                    // creat cmd and bind
                    cmd = new RoutedUICommand(mi.Name, mi.Name, typeof(string));
                    cmdBindings.Add(new CommandBinding(cmd, (s3, e3) =>
                    {
                        // activate
                        var ticket = new AasxMenuActionTicket()
                        {
                            MenuItem = m_mii
                        };
                        topMenu?.ActivateAction(m_mii, ticket);
                    }));

                    // possibly create hotkey
                    if (inputBindings != null
                        && kgConv != null
                        && mi is AasxMenuItemHotkeyed mihk
                        && mihk.InputGesture?.HasContent() == true
                        && !mihk.GestureOnlyDisplay)
                    {
                        var kb = new KeyBinding()
                        {
                            Gesture = kgConv.ConvertFromInvariantString(mihk.InputGesture) as KeyGesture,
                            Command = cmd
                        };
                        inputBindings.Add(kb);
                    }
                }

                // visible items?
                if (mi is AasxMenuItem mii && !mii.Hidden)
                {
                    // remember for lambdas
                    var m_mii = mii;

                    // add menu item
                    var wpf = new System.Windows.Controls.MenuItem()
                    {
                        Header = mii.Header,
                        InputGestureText = mii.InputGesture,
                        IsCheckable = mii.IsCheckable,
                        IsChecked = mii.IsChecked,
                        Command = cmd
                    };

                    if (topMenu?.DefaultForeground != null)
                        wpf.Foreground = AnyUiDisplayContextWpf.GetWpfBrush(topMenu.DefaultForeground);

                    if (mii.Foreground != null)
                        wpf.Foreground = AnyUiDisplayContextWpf.GetWpfBrush(mii.Foreground);

                    // if for any sake cmd isn't available, do directly
                    if (cmd == null && mii.Name?.HasContent() == true)
                    {
                        wpf.Click += (s, e) =>
                        {
                            e.Handled = true;
                            var ticket = new AasxMenuActionTicket()
                            {
                                MenuItem = m_mii
                            };
                            topMenu?.ActivateAction(m_mii, ticket);
                        };
                    }
                    wpfItems.Add(wpf);

                    // remember in dictionaries
                    if (m_mii.Name?.HasContent() == true)
                        _menuItems.AddPair(m_mii.Name?.Trim().ToLower(), m_mii);
                    _wpfItems.AddPair(m_mii, wpf);

                    // recurse (only on visible items possible)
                    if (mii.Childs != null)
                        RenderItemCollection(topMenu, mii.Childs, wpf.Items, cmdBindings, inputBindings, kgConv);
                }
            }
        }

        public AnyUiLambdaActionBase HandleGlobalKeyDown(KeyEventArgs e, bool preview)
        {
            // access
            if (e == null || Menu == null)
                return null;

            var kgConv = new KeyGestureConverter();

            foreach (var mi in Menu.FindAll<AasxMenuItemHotkeyed>())
            {
                if (mi.InputGesture == null)
                    continue;
                var g = kgConv.ConvertFromInvariantString(mi.InputGesture) as KeyGesture;
                if (g != null
                    && g.Key == e.Key
                    && g.Modifiers == Keyboard.Modifiers)
                {
                    var ticket = new AasxMenuActionTicket();
                    mi.Action?.Invoke(mi.Name, mi, ticket);
                    return ticket.UiLambdaAction;
                }
            }

            return null;
        }

        public void LoadAndRender(
            AasxMenu menuInfo,
            System.Windows.Controls.Menu wpfMenu,
            CommandBindingCollection cmdBindings = null,
            InputBindingCollection inputBindings = null)
        {
            _menu = menuInfo;
            _menuItems.Clear();
            _wpfItems.Clear();
            wpfMenu.Items.Clear();

            var kgConv = new KeyGestureConverter();

            RenderItemCollection(menuInfo, menuInfo, wpfMenu.Items, cmdBindings, inputBindings, kgConv);
        }

        public bool IsChecked(string name)
        {
            var wpf = _wpfItems.Get2OrDefault(_menuItems.Get2OrDefault(name?.Trim().ToLower()));
            if (wpf != null)
                return wpf.IsChecked;
            return false;
        }

        public void SetChecked(string name, bool state)
        {
            var wpf = _wpfItems.Get2OrDefault(_menuItems.Get2OrDefault(name?.Trim().ToLower()));
            if (wpf != null)
                wpf.IsChecked = state;
        }
    }
}
