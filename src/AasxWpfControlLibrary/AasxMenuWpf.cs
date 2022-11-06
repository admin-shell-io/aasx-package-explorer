/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AdminShellNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        private void RenderItemCollection(AasxMenu topMenu, AasxMenu menuItems, System.Windows.Controls.ItemCollection wpfItems)
        {
            foreach (var mi in menuItems)
            {
                if (mi == null)
                    continue;

                if (mi is AasxMenuSeparator mis)
                {
                    // add separator
                    wpfItems.Add(new System.Windows.Controls.Separator());
                }
                else if (mi is AasxMenuItem mii)
                {
                    // remember for lambdas
                    var m_mii = mii;

                    // add menu item
                    var wpf = new System.Windows.Controls.MenuItem()
                    {
                        Header = mii.Header,
                        InputGestureText = mii.InputGesture,
                        IsCheckable = mii.IsCheckable,
                        IsChecked = mii.IsChecked
                    };
                    wpf.Click += (s, e) =>
                    {
                        e.Handled = true;
                        topMenu?.ActivateAction(m_mii);
                    };
                    wpfItems.Add(wpf);

                    // remember in dictionaries
                    if (m_mii.Name?.HasContent() == true)
                        _menuItems.AddPair(m_mii.Name?.Trim().ToLower(), m_mii);
                    _wpfItems.AddPair(m_mii, wpf);

                    // recurse
                    if (mii.Childs != null)
                        RenderItemCollection(topMenu, mii.Childs, wpf.Items);
                }
            }
        }

        public void LoadAndRender(AasxMenu menuInfo, System.Windows.Controls.Menu wpfMenu)
        {
            _menu = menuInfo;
            _menuItems.Clear();
            _wpfItems.Clear();
            wpfMenu.Items.Clear();
            RenderItemCollection(menuInfo, menuInfo, wpfMenu.Items);
        }

        public bool IsChecked(string name)
        {
            var wpf = _wpfItems.Get2OrDefault(_menuItems.Get2OrDefault(name?.Trim().ToLower()));
            if (wpf !=null) 
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
