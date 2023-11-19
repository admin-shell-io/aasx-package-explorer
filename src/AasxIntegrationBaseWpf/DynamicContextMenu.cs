/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using static System.Net.Mime.MediaTypeNames;

namespace AasxIntegrationBaseWpf
{
    /// <summary>
    /// Display data for WPF context menu items.
    /// Holds the WPF control.
    /// </summary>
    public class DynamicContextMenuDisplayDataWpf : AasxMenuDisplayDataBase
    {
        /// <summary>
        /// A WPF control for the context menu item.
        /// </summary>
        public Control Control;

        public DynamicContextMenuDisplayDataWpf() { }

        public DynamicContextMenuDisplayDataWpf(Control control)
        {
            Control = control;
        }
    }

    /// <summary>
    /// In the past, this was an independent WPF class, basically a wrapper around
    /// WPF ContextMenu. For the time being, if is using AasxMeneu items and generates
    /// WPF ContextMenu from it.
    /// </summary>
    public class DynamicContextMenu
    {
        public AasxMenu Menu = new AasxMenu();

        public static DynamicContextMenu CreateNew(AasxMenu root)
        {
            var res = new DynamicContextMenu();
            res.Menu = root;
            return res;
        }

        /// <summary>
        /// Renders the abstract UI information into concrete WPF controls,
        /// which would be added to the WPF ContextMenu.
        /// </summary>
        protected Control RenderMenuItem(AasxMenu root, AasxMenuItemBase item)
        {
            if (item is AasxMenuSeparator sep)
            {
                var ctl = new Separator();

                // result
                sep.DisplayData = new DynamicContextMenuDisplayDataWpf(ctl);
                return ctl;
            }

            if (item is AasxMenuTextBox mitb)
            {
                var g = new System.Windows.Controls.Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition());
                g.ColumnDefinitions.Add(new ColumnDefinition());

                var bl = new System.Windows.Controls.TextBlock();
                bl.Text = mitb.Header;
                bl.Width = mitb.HeaderWidth;

                var bx = new System.Windows.Controls.TextBox();
                bx.Text = mitb.TextValue;
                bx.Width = mitb.TextWidth;
                bx.TextChanged += async (s, e) =>
                {
                    mitb.TextValue = bx.Text;
                    await root.ActivateAction(mitb, null);
                };

                Grid.SetColumn(bl, 0);
                Grid.SetColumn(bx, 1);

                g.Children.Add(bl);
                g.Children.Add(bx);

                var ctl = new System.Windows.Controls.MenuItem();
                ctl.Icon = mitb.Icon;
                ctl.Header = g;
                ctl.Tag = mitb;

                // result
                mitb.DisplayData = new DynamicContextMenuDisplayDataWpf(ctl);
                return ctl;
            }

            if (item is AasxMenuItem mi)
            {
                var ctl = new System.Windows.Controls.MenuItem();
                ctl.Icon = mi.Icon;
                ctl.Header = mi.Header;
                ctl.Tag = mi;
                ctl.IsCheckable = mi.IsCheckable;
                ctl.IsChecked = mi.IsChecked;

                // result
                mi.DisplayData = new DynamicContextMenuDisplayDataWpf(ctl);
                return ctl;
            }

            // no?!
            return null;
        }

        /// <summary>
        /// "Attaches" a WPF ContextMenu to a UI element.
        /// Menu data is based on AasxMenu items, which will be redered as WPF.
        /// Currently not hierarchially (recursive)!
        /// </summary>
        public void Start(
            UIElement placementTarget, AasxMenuActionDelegate lambdaAction = null,
            AasxMenuActionTicket ticket = null)
        {
            // access
            if (Menu == null)
                return;

            // attach to menu
            if (lambdaAction != null)
            {
                Menu.DefaultAction = lambdaAction;
            }

            // render
            var cm = new ContextMenu();
            foreach (var mi in Menu)
            {
                // can render?
                var ctl = RenderMenuItem(Menu, mi);
                if (ctl == null)
                    continue;

                // action
                if (ctl is MenuItem ctlmi)
                    ctlmi.Click += async (s, e) =>
                    {
                        await Menu.ActivateAction(mi, ticket);
                    };

                // add
                cm.Items.Add(ctl);
            }

            // show
            cm.PlacementTarget = placementTarget;
            cm.IsOpen = true;
        }
    }
}
