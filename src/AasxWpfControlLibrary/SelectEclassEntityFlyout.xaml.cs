using AasxIntegrationBase;
using AdminShellNS;
using Newtonsoft.Json;
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
    public partial class SelectEclassEntityFlyout : UserControl, IFlyoutControl
    {
        public event IFlyoutControlClosed ControlClosed;

        public enum SelectMode { General, IRDI, ConceptDescription }

        private string eclassFullPath = null;
        private SelectMode selectMode = SelectMode.General;

        public string ResultIRDI = null;
        public AdminShell.ConceptDescription ResultCD = null;

        public SelectEclassEntityFlyout(string eclassFullPath = null, SelectMode selectMode = SelectMode.General)
        {
            InitializeComponent();
            this.eclassFullPath = eclassFullPath;

            // initial window state
            this.CheckBoxTwoPass.IsChecked = Options.Curr.EclassTwoPass;

            // any complex
            if (selectMode == SelectMode.IRDI)
            {
                this.CheckBoxTwoPass.IsChecked = false;
                this.CheckBoxTwoPass.IsEnabled = false;
            }
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

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.ResultIRDI = null;
            this.ResultCD = null;
            ControlClosed?.Invoke();
        }

        //
        // Mechanics
        //

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // setup workers
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
        }

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
                this.ResultCD = EclassUtils.GenerateConceptDescription(a.jobData.items, resIrdi);
                this.ResultIRDI = resIrdi;

                // success -> auto close
                if (this.ResultCD != null)
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
            this.ResultIRDI = si.IRDI;

            // special case: unit .. try correct from unit id to unitCodeValue for IRDI
            if (si.Entity == "unit")
            {
                var x = EclassUtils.GetIrdiForUnitSearchItem(si);
                if (x != null)
                    this.ResultIRDI = x;
            }

            // one or two passes?
            if (!twoPass)
            {
                // special case: property selected
                if (si.Entity == "prop" && selectMode != SelectMode.IRDI)
                {
                    var input = new List<EclassUtils.SearchItem>();
                    input.Add(si);
                    foreach (EclassUtils.SearchItem di in EntityList.Items)
                        if (di != null && di.Entity == si.Entity && di.IRDI == si.IRDI && si != di)
                            input.Add(di);

                    // own function
                    this.ResultCD = EclassUtils.GenerateConceptDescription(input, si.IRDI);
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
