using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AasxIntegrationBase;
using AdminShellNS;
using Newtonsoft.Json;

/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

The browser functionality is under the cefSharp license
(see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).

The JSON serialization is under the MIT license
(see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).

The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).

The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0).
*/

namespace AasxPackageExplorer
{
    /// <summary>
    /// Creates a flyout in order to select items from a list
    /// </summary>
    public partial class SelectFromReferablesPoolFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlClosed ControlClosed;

        public string Caption = "Select item ..";

        public AasxPredefinedConcepts.DefinitionsPool DataSourcePools = null;

        public int ResultIndex = -1;
        public object ResultItem = null;

        public SelectFromReferablesPoolFlyout()
        {
            InitializeComponent();
        }

        //
        // Outer
        //

        public void ControlStart()
        {
        }

        public void ControlPreviewKeyDown(KeyEventArgs e)
        {
        }

        //
        // Mechanics
        //

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // fill caption
            if (this.Caption != null)
                TextBlockCaption.Text = "" + this.Caption;

            // entities
            DataGridEntities.Items.Clear();

            // fill listbox
            this.ListBoxDomains.Items.Clear();
            if (DataSourcePools != null)
            {
                foreach (var d in DataSourcePools.GetDomains())
                    this.ListBoxDomains.Items.Add(d);
                if (this.ListBoxDomains.Items.Count > 0)
                    this.ListBoxDomains.SelectedIndex = 0;
            }

        }

        private void ListBoxDomains_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var dom = this.ListBoxDomains.SelectedItem as string;
            if (dom == null)
                return;
            var ld = this.DataSourcePools?.GetEntitiesForDomain(dom);
            if (ld != null)
            {
                DataGridEntities.Items.Clear();
                foreach (var ent in ld)
                    DataGridEntities.Items.Add(ent);
            }
        }

        private bool PrepareResult()
        {
            if (DataGridEntities.SelectedItem != null && DataGridEntities.SelectedIndex >= 0)
            {
                this.ResultIndex = DataGridEntities.SelectedIndex;
                this.ResultItem = DataGridEntities.SelectedItem;
                return true;
            }

            // uups
            return false;
        }

        private void ButtonSelect_Click(object sender, RoutedEventArgs e)
        {
            if (PrepareResult())
                ControlClosed?.Invoke();
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.ResultIndex = -1;
            this.ResultItem = null;
            ControlClosed?.Invoke();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PrepareResult())
                ControlClosed?.Invoke();
        }

    }
}
