using System;
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
using AasxWpfControlLibrary;
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
    /// Interaktionslogik für SelectFromRepository.xaml
    /// </summary>
    public partial class SelectAasEntityFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlClosed ControlClosed;

        PackageCentral packages = null;
        PackageCentral.Selector selector;
        private string theFilter = null;

        public AdminShell.KeyList ResultKeys = null;
        public VisualElementGeneric ResultVisualElement = null;

        public SelectAasEntityFlyout(
            PackageCentral packages,
            PackageCentral.Selector selector,
            string filter = null)
        {
            InitializeComponent();
            this.packages = packages;
            this.selector = selector;
            theFilter = filter;
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
            DisplayElements.Background = new SolidColorBrush(Color.FromRgb(40, 40, 40));

            // fill combo box
            ComboBoxFilter.Items.Add("All");
            foreach (var x in AdminShell.Key.KeyElements)
                ComboBoxFilter.Items.Add(x);

            // select an item
            if (theFilter != null)
                foreach (var x in ComboBoxFilter.Items)
                    if (x.ToString().Trim().ToLower() == theFilter.Trim().ToLower())
                    {
                        ComboBoxFilter.SelectedItem = x;
                        break;
                    }

            // fill contents
            FilterFor(theFilter);
        }

        private void ComboBoxFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == ComboBoxFilter)
            {
                theFilter = ComboBoxFilter.SelectedItem.ToString();
                if (theFilter == "All")
                    theFilter = null;
                // fill contents
                FilterFor(theFilter);
            }
        }

        private AdminShell.KeyList BuildKeyListToTop(
            VisualElementGeneric visElem,
            bool includeAas = false)
        {
            // access
            if (visElem == null)
                return null;

            // prepare result
            var res = new AdminShell.KeyList();
            var ve = visElem;
            while (ve != null)
            {
                if (ve is VisualElementSubmodelRef)
                {
                    // import special case, as Submodel ref is important part of the chain!
                    var elem = ve as VisualElementSubmodelRef;
                    if (elem.theSubmodel != null)
                        res.Insert(
                            0,
                            AdminShell.Key.CreateNew(
                                elem.theSubmodel.GetElementName(), true,
                                elem.theSubmodel.identification.idType,
                                elem.theSubmodel.identification.id));

                    // include aas
                    if (includeAas && ve.Parent is VisualElementAdminShell veAas
                        && veAas.theAas?.identification != null)
                    {
                        res.Insert(
                            0,
                            AdminShell.Key.CreateNew(
                                AdminShell.Key.AAS, true,
                                veAas.theAas.identification.idType,
                                veAas.theAas.identification.id));
                    }

                    break;
                }
                else
                if (ve.GetMainDataObject() is AdminShell.Identifiable)
                {
                    // a Identifiable will terminate the list of keys
                    var data = ve.GetMainDataObject() as AdminShell.Identifiable;
                    if (data != null)
                        res.Insert(
                            0,
                            AdminShell.Key.CreateNew(
                                data.GetElementName(), true, data.identification.idType, data.identification.id));
                    break;
                }
                else
                if (ve.GetMainDataObject() is AdminShell.Referable)
                {
                    // add a key and go up ..
                    var data = ve.GetMainDataObject() as AdminShell.Referable;
                    if (data != null)
                        res.Insert(
                            0,
                            AdminShell.Key.CreateNew(data.GetElementName(), true, "IdShort", data.idShort));
                }
                else
                // uups!
                { }
                // need to go up
                ve = ve.Parent;
            }

            return res;
        }

        private bool PrepareResult()
        {
            // access
            if (DisplayElements == null || DisplayElements.SelectedItem == null)
                return false;
            var si = DisplayElements.SelectedItem;
            var siMdo = si.GetMainDataObject();

            // already one result
            this.ResultVisualElement = si;

            //
            // Referable
            //
            if (siMdo is AdminShell.Referable dataRef)
            {
                // check if a valuable item was selected
                var elemname = dataRef.GetElementName();
                var fullFilter = ApplyFullFilterString(theFilter);
                if (fullFilter != null && !(fullFilter.IndexOf(elemname + " ", StringComparison.Ordinal) >= 0))
                    return false;

                // ok, prepare list of keys
                this.ResultKeys = BuildKeyListToTop(si);
                return true;
            }

            //
            // other special cases
            //
            if (siMdo is AdminShell.SubmodelRef smref &&
                    (theFilter == null ||
                        ApplyFullFilterString(theFilter)
                            .ToLower().IndexOf("submodelref ", StringComparison.Ordinal) >= 0))
            {
                this.ResultKeys = new AdminShell.KeyList();
                this.ResultKeys.AddRange(smref.Keys);
                return true;
            }

            if (si is VisualElementPluginExtension vepe)
            {
                // get main data object of the parent of the plug in ..
                var parentMdo = vepe.Parent.GetMainDataObject();
                if (parentMdo != null)
                {
                    // safe to return a list for the parent ..
                    // (include AAS, as this is important to plug-ins)
                    this.ResultKeys = BuildKeyListToTop(si, includeAas: true);

                    // .. enriched by a last element
                    this.ResultKeys.Add(new AdminShell.Key(AdminShell.Key.FragmentReference, true,
                        AdminShell.Key.Custom, "Plugin:" + vepe.theExt.Tag));

                    // ok
                    return true;
                }
            }

            // uups
            return false;
        }

        private void ButtonSelect_Click(object sender, RoutedEventArgs e)
        {
            if (PrepareResult())
                ControlClosed?.Invoke();
        }

        private string ApplyFullFilterString(string filter)
        {
            if (filter == null)
                return null;
            var res = filter;
            if (res.Trim().ToLower() == "submodelelement")
                foreach (var s in AdminShell.Key.SubmodelElements)
                    res += " " + s + " ";
            return " " + res + " ";
        }

        private void FilterFor(string filter)
        {
            filter = ApplyFullFilterString(filter);
            DisplayElements.RebuildAasxElements(packages, selector, true, filter);
        }

        private void DisplayElements_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PrepareResult())
                ControlClosed?.Invoke();
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.ResultKeys = null;
            ControlClosed?.Invoke();
        }
    }
}
