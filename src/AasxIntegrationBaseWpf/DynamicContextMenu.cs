/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
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
    /// One item for DynamicContextMenu. Basicall a wrapper around MenuItem
    /// </summary>
    public class DynamicContextItem
    {
        public string Tag = "";
        public Control MenuItem;

        public DynamicContextItem() { }

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

        public void Start(UIElement placementTarget, Action<string> lambda)
        {
            // render
            var cm = new ContextMenu();
            foreach (var dci in this)
            {
                var myDci = dci;
                var mi = dci.MenuItem;
                if (mi == null)
                    continue;
                if (mi is MenuItem mii)
                    mii.Click += (s, e) =>
                    {
                        lambda?.Invoke(myDci.Tag);
                    };
                cm.Items.Add(mi);
            }
            cm.PlacementTarget = placementTarget;
            cm.IsOpen = true;
        }
    }
}
