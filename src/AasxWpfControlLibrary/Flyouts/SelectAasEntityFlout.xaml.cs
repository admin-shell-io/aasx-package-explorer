/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

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
using AasCore.Aas3_0_RC02;
using AasxIntegrationBase;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AasxWpfControlLibrary;
using AdminShellNS;
using Extensions;
using Newtonsoft.Json;

namespace AasxPackageExplorer
{
    public partial class SelectAasEntityFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlAction ControlClosed;

        // TODO (MIHO, 2020-12-21): make DiaData non-Nullable
        public AnyUiDialogueDataSelectAasEntity DiaData = new AnyUiDialogueDataSelectAasEntity();

        PackageCentral packages = null;

        public SelectAasEntityFlyout(
            PackageCentral packages,
            PackageCentral.Selector? selector = null,
            string filter = null)
        {
            InitializeComponent();
            this.packages = packages;
            if (selector.HasValue)
                DiaData.Selector = selector.Value;
            if (filter != null)
                DiaData.Filter = filter;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            DisplayElements.Background = new SolidColorBrush(Color.FromRgb(40, 40, 40));

            // fill combo box
            ComboBoxFilter.Items.Add("All");
            foreach (var x in Enum.GetNames(typeof(KeyTypes)))
                ComboBoxFilter.Items.Add(x);

            // select an item
            if (DiaData.Filter != null)
                foreach (var x in ComboBoxFilter.Items)
                    if (x.ToString().Trim().ToLower() == DiaData.Filter.Trim().ToLower())
                    {
                        ComboBoxFilter.SelectedItem = x;
                        break;
                    }

            // fill contents
            FilterFor(DiaData.Filter);
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

        private void ComboBoxFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == ComboBoxFilter)
            {
                DiaData.Filter = ComboBoxFilter.SelectedItem.ToString();
                if (DiaData.Filter == "All")
                    DiaData.Filter = null;
                // fill contents
                FilterFor(DiaData.Filter);
            }
        }

        private bool PrepareResult()
        {
            // access
            if (DisplayElements == null || DisplayElements.SelectedItem == null)
                return false;
            var si = DisplayElements.SelectedItem;
            var siMdo = si.GetMainDataObject();

            // already one result
            DiaData.ResultVisualElement = si;

            //
            // IReferable
            //
            if (siMdo is IReferable dataRef)
            {
                // check if a valuable item was selected
                // new special case: "GlobalReference" allows to select all (2021-09-11)
                var skip = DiaData.Filter != null &&
                    DiaData.Filter.Trim().ToLower() == Stringification.ToString(KeyTypes.GlobalReference).Trim().ToLower();
                if (!skip)
                {
                    var elemname = dataRef.GetSelfDescription().AasElementName;
                    var fullFilter = ApplyFullFilterString(DiaData.Filter);
                    if (fullFilter != null && !(fullFilter.IndexOf(elemname + " ", StringComparison.Ordinal) >= 0))
                        return false;
                }

                // ok, prepare list of keys
                DiaData.ResultKeys = si.BuildKeyListToTop();

                return true;
            }

            //
            // other special cases
            //
            if (siMdo is Reference smref && CheckFilter("submodelref"))
            {
                DiaData.ResultKeys = new List<AasCore.Aas3_0_RC02.Key>();
                DiaData.ResultKeys.AddRange(smref.Keys);
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
                    DiaData.ResultKeys = si.BuildKeyListToTop(includeAas: true);

                    // .. enriched by a last element
                    DiaData.ResultKeys.Add(new AasCore.Aas3_0_RC02.Key(KeyTypes.FragmentReference, "Plugin:" + vepe.theExt.Tag));

                    // ok
                    return true;
                }
            }

            if (si is VisualElementOperationVariable veov && CheckFilter("OperationVariable")
                && veov.theOpVar?.Value != null)
            {
                // prepare data
                DiaData.ResultKeys = si.BuildKeyListToTop(includeAas: true);
                return true;
            }

            if (si is VisualElementSupplementalFile vesf && vesf.theFile != null)
            {
                // prepare data
                DiaData.ResultKeys = si.BuildKeyListToTop(includeAas: true);
                return true;
            }

            // uups
            return false;
        }

        private void ButtonSelect_Click(object sender, RoutedEventArgs e)
        {
            if (PrepareResult())
            {
                DiaData.Result = true;
                ControlClosed?.Invoke();
            }
        }

        private string ApplyFullFilterString(string filter)
        {
            if (filter == null)
                return null;
            var res = filter;
            if (res.Trim().ToLower() == "submodelelement")
                foreach (var s in Enum.GetNames(typeof(AasSubmodelElements)))
                    res += " " + s + " ";
            return " " + res + " ";
        }

        private void FilterFor(string filter)
        {
            filter = ApplyFullFilterString(filter);
            DisplayElements.RebuildAasxElements(packages, DiaData.Selector, true, filter,
                lazyLoadingFirst: true);
        }

        private bool CheckFilter(string name)
        {
            return (
                DiaData.Filter == null || name == null
                || ApplyFullFilterString(DiaData.Filter).ToLower()
                    .IndexOf($"{name.ToLower().Trim()} ", StringComparison.Ordinal) >= 0);
        }

        private void DisplayElements_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PrepareResult())
            {
                DiaData.Result = true;
                ControlClosed?.Invoke();
            }
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            DiaData.Result = false;
            ControlClosed?.Invoke();
        }
    }
}
