/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using AasxPackageLogic;
using AdminShellNS;
using AnyUi;
using Newtonsoft.Json;

namespace AasxPackageExplorer
{
    public partial class SelectEclassEntityFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlAction ControlClosed;

        public AnyUiDialogueDataSelectEclassEntity DiaData = new AnyUiDialogueDataSelectEclassEntity();

        private string eclassFullPath;

        public SelectEclassEntityFlyout()
        {
            InitializeComponent();

            // members
            this.eclassFullPath = System.IO.Path.GetFullPath(Options.Curr.EclassDir);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // initial window state
            this.CheckBoxTwoPass.IsChecked = Options.Curr.EclassTwoPass;

            // any complex
            if (DiaData.Mode == AnyUiDialogueDataSelectEclassEntity.SelectMode.IRDI)
            {
                this.CheckBoxTwoPass.IsChecked = false;
                this.CheckBoxTwoPass.IsEnabled = false;
            }

            // setup workers
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
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

        public void LambdaActionAvailable(AnyUiLambdaActionBase la)
        {
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            DiaData.ResultIRDI = null;
            DiaData.ResultCD = null;
            ControlClosed?.Invoke();
        }

        //
        // Mechanics
        //        

        private readonly BackgroundWorker worker = new BackgroundWorker();

        private class BackgroundWorkerArgs
        {
            public EclassUtils.SearchJobData jobData = null;
            public int jobType = 0; // 1 = search text, 2 = search irdi
        }

        private EclassUtils.SearchJobData InitJobData()
        {
            var args = new EclassUtils.SearchJobData(eclassFullPath);

            args.searchInClasses = this.SearchInClasses.IsChecked == true;
            args.searchInDatatypes = this.SearchInDatatypes.IsChecked == true;
            args.searchInProperties = this.SearchInProperties.IsChecked == true;
            args.searchInUnits = this.SearchInUnits.IsChecked == true;

            return args;
        }

        private void ButtonSearchStart_Click(object sender, RoutedEventArgs e)
        {
            var st = SearchFor.Text.Trim().ToLower();
            if (st.Length < 2)
            {
                MessageBox.Show(
                    "The search string needs to comprise at least 2 characters in order to " +
                        "limit the amount of search results!", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (!worker.IsBusy && eclassFullPath != null)
            {
                SearchProgress.Value = 0;
                EntityList.Items.Clear();
                EntityContent.Text = "";

                var args = new BackgroundWorkerArgs();
                args.jobData = InitJobData();
                args.jobData.searchText = st;
                args.jobType = 1;

                worker.RunWorkerAsync(args);
            }
        }

        private void SearchFor_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                // fake mouse click
                ButtonSearchStart_Click(null, null);
            }
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            // run all background tasks here
            var a = e.Argument as BackgroundWorkerArgs;

            if (a?.jobData != null && a.jobType == 1)
                EclassUtils.SearchForTextInEclassFiles(a.jobData, (frac) =>
                {
                    SearchProgress.Dispatcher.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Background,
                        new Action(() => this.SearchProgress.Value = frac));
                });

            if (a?.jobData != null && a.jobType == 2)
                EclassUtils.SearchForIRDIinEclassFiles(a.jobData, (frac) =>
                {
                    SearchProgress.Dispatcher.BeginInvoke(
                        System.Windows.Threading.DispatcherPriority.Background,
                        new Action(() => this.SearchProgress.Value = frac));
                });

            // set result
            e.Result = e.Argument;
        }

        private void worker_RunWorkerCompleted(object sender,
                                               RunWorkerCompletedEventArgs e)
        {
            //update ui once worker complete his work
            SearchProgress.Value = 100;

            // access
            var a = (e.Result as BackgroundWorkerArgs);
            if (a == null)
                return;

            // search text!
            if (a.jobType == 1)
            {
                // may be inform upon too many elements
                if (a.jobData.tooMany)
                    MessageBox.Show(
                        "Too many search results. Search aborted!", "Search entities",
                        MessageBoxButton.OK, MessageBoxImage.Exclamation);

                // sort
                a.jobData.items.Sort(delegate (EclassUtils.SearchItem si1, EclassUtils.SearchItem si2)
                {
                    if (si1.Entity != si2.Entity)
                        return String.Compare(si1.Entity, si2.Entity, StringComparison.Ordinal);
                    else
                        return String.Compare(si1.IRDI, si2.IRDI, StringComparison.Ordinal);
                });

                // re-fill into the UI list
                EntityList.Items.Clear();
                foreach (var it in a.jobData.items)
                    EntityList.Items.Add(it);
            }

            if (a.jobType == 2 && a.jobData != null && a.jobData.searchIRDIs.Count == 1)
            {
                // 1st pass already done, the jobData items already containing all required information to
                // generate a proper content description

                // own function
                var resIrdi = a.jobData.searchIRDIs[0].ToUpper();
                DiaData.ResultCD = EclassUtils.GenerateConceptDescription(a.jobData.items, resIrdi);
                DiaData.ResultIRDI = resIrdi;

                // success -> auto close
                if (DiaData.ResultCD != null)
                    ControlClosed?.Invoke();
            }
        }

        private void ButtonSelect_Click(object sender, RoutedEventArgs e)
        {
            if (FinalizeSelection(EntityList.SelectedItem as EclassUtils.SearchItem, CheckBoxTwoPass.IsChecked == true))
                ControlClosed?.Invoke();
        }

        private void Tree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ButtonSelect_Click(sender, e);
        }

        private void EntityList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // access
            if (EntityContent == null || EntityList == null || EntityList.SelectedItem == null ||
                    !(EntityList.SelectedItem is EclassUtils.SearchItem))
                return;

            // set
            var si = EntityList.SelectedItem as EclassUtils.SearchItem;
            if (si?.ContentNode != null)
                EntityContent.Text = si.ContentNode.OuterXml;
            else
                EntityContent.Text = "";
        }

        private bool FinalizeSelection(EclassUtils.SearchItem si, bool twoPass)
        {
            // access
            if (si == null)
                return false;

            // simply put the IRDI
            DiaData.ResultIRDI = si.IRDI;

            // special case: unit .. try correct from unit id to unitCodeValue for IRDI
            if (si.Entity == "unit")
            {
                var x = EclassUtils.GetIrdiForUnitSearchItem(si);
                if (x != null)
                    DiaData.ResultIRDI = x;
            }

            // one or two passes?
            if (!twoPass)
            {
                // special case: property selected
                if (si.Entity == "prop" && DiaData.Mode != AnyUiDialogueDataSelectEclassEntity.SelectMode.IRDI)
                {
                    var input = new List<EclassUtils.SearchItem>();
                    input.Add(si);
                    foreach (EclassUtils.SearchItem di in EntityList.Items)
                        if (di != null && di.Entity == si.Entity && di.IRDI == si.IRDI && si != di)
                            input.Add(di);

                    // own function
                    DiaData.ResultCD = EclassUtils.GenerateConceptDescription(input, si.IRDI);
                }
            }
            else
            {
                if (si.Entity == "prop" && si.ContentNode != null)
                {
                    var irdi = EclassUtils.GetAttributeByName(si.ContentNode, "id");
                    if (irdi != null)
                    {
                        if (!worker.IsBusy && eclassFullPath != null)
                        {
                            SearchProgress.Value = 0;
                            EntityList.Items.Clear();
                            EntityContent.Text = "";

                            var args = new BackgroundWorkerArgs();
                            args.jobData = InitJobData();
                            args.jobData.searchIRDIs.Add(irdi.Trim().ToLower());
                            args.jobType = 2;

                            // start second pass!
                            worker.RunWorkerAsync(args);
                            return false;
                        }
                    }
                }
            }

            // generally ok
            return true;
        }
    }
}
