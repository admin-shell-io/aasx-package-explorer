/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace AasxIntegrationBaseWpf
{
    /// <summary>
    /// One item for DynamicContextMenu. Basically a wrapper around MenuItem
    /// </summary>
    public class DynamicContextItem
    {
        public string Tag = "";
        public Control MenuItem;

        public DynamicContextItem() { }

        public Action<string, object> Lambda = null;

        public DynamicContextItem(string tag, Control menuItem)
        {
            Tag = tag;
            MenuItem = menuItem;
        }

        public DynamicContextItem(string tag, object icon, object header)
        {
            Tag = tag;
            var mi = new MenuItem();
            mi.Icon = icon;
            mi.Header = header;
            mi.Tag = tag;
            MenuItem = mi;
        }

        public DynamicContextItem(string tag, object icon, object header, bool checkState)
        {
            Tag = tag;
            var mi = new MenuItem();
            mi.Icon = icon;
            mi.Header = header;
            mi.Tag = tag;
            mi.IsCheckable = true;
            mi.IsChecked = checkState;
            MenuItem = mi;
        }

        public static DynamicContextItem CreateSeparator()
        {
            var dci = new DynamicContextItem();
            dci.Tag = "-";
            dci.MenuItem = new Separator();
            return dci;
        }

        public static DynamicContextItem CreateTextBox(
            string tag, object icon, object header, 
            double headerWidth, string text, double textWidth)
        {
            var dci = new DynamicContextItem();

            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition());
            g.ColumnDefinitions.Add(new ColumnDefinition());

            var bl = new TextBlock();
            bl.Text = header as string;
            bl.Width = headerWidth;

            var bx = new TextBox();
            bx.Text = text;
            bx.Width = textWidth;
            bx.TextChanged += (s, e) =>
            {
                dci.Lambda?.Invoke(tag, bx.Text);
            };

            Grid.SetColumn(bl, 0);
            Grid.SetColumn(bx, 1);

            g.Children.Add(bl);
            g.Children.Add(bx);

            var mi = new MenuItem();
            mi.Icon = icon;
            mi.Header = g;
            mi.Tag = tag;

            dci.Tag = tag;
            dci.MenuItem = mi;
            return dci;
        }
    }

    public class DynamicContextMenu : List<DynamicContextItem>
    {
        public static DynamicContextMenu CreateNew(params DynamicContextItem[] items)
        {
            var res = new DynamicContextMenu();
            if (items != null)
                foreach (var it in items)
                    res.Add(it);
            return res;
        }

        public void Start(UIElement placementTarget, Action<string, object> lambda)
        {
            // render
            var cm = new ContextMenu();
            foreach (var dci in this)
            {
                var myDci = dci;
                dci.Lambda = lambda;
                var mi = dci.MenuItem;
                if (mi == null)
                    continue;
                if (mi is MenuItem mii)
                    mii.Click += (s, e) =>
                    {
                        lambda?.Invoke(myDci.Tag, 1);
                    };
                cm.Items.Add(mi);
            }
            cm.PlacementTarget = placementTarget;
            cm.IsOpen = true;
        }
    }
}
