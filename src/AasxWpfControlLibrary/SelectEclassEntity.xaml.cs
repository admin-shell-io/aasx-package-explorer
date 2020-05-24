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
using System.Windows.Shapes;
using System.Xml;

using AdminShellNS;

/* Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>, author: Michael Hoffmeister
This software is licensed under the Eclipse Public License - v 2.0 (EPL-2.0) (see https://www.eclipse.org/org/documents/epl-2.0/EPL-2.0.txt).
The browser functionality is under the cefSharp license (see https://raw.githubusercontent.com/cefsharp/CefSharp/master/LICENSE).
The JSON serialization is under the MIT license (see https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md).
The QR code generation is under the MIT license (see https://github.com/codebude/QRCoder/blob/master/LICENSE.txt).
The Dot Matrix Code (DMC) generation is under Apache license v.2 (see http://www.apache.org/licenses/LICENSE-2.0). */

namespace AasxPackageExplorer
{
    /// <summary>
    /// Interaktionslogik für SelectAasEntityDialogue.xaml
    /// </summary>
    public partial class SelectEclassEntity : Window
    {
        private string eclassFullPath = null;

        public string ResultIRDI = null;
        public AdminShell.ConceptDescription ResultCD = null;

        public SelectEclassEntity(string eclassFullPath = null)
        {
            InitializeComponent();
            this.eclassFullPath = eclassFullPath;

            // initial window state
            this.Width = 800;
            this.Height = 600;
            this.CheckBoxTwoPass.IsChecked = Options.Curr.EclassTwoPass;
        }

        private readonly BackgroundWorker worker = new BackgroundWorker();

        private class BackgroundWorkerArgs
        {
            public EclassUtils.SearchJobData jobData = null;
            public int jobType = 0; // 1 = search text, 2 = search irdi
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // setup workers
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
        }

        private EclassUtils.SearchJobData InitJobData()
        {
            var args = new EclassUtils.SearchJobData();

            args.searchInClasses = this.SearchInClasses.IsChecked == true;
            args.searchInDatatypes = this.SearchInDatatypes.IsChecked == true;
            args.searchInProperties = this.SearchInProperties.IsChecked == true;
            args.searchInUnits = this.SearchInUnits.IsChecked == true;

            args.eclassFiles.Clear();
            foreach (var fn in System.IO.Directory.GetFiles(eclassFullPath, "*.xml"))
            {
                var dft = EclassUtils.TryGetDataFileType(fn);
                args.eclassFiles.Add(new EclassUtils.FileItem(fn, dft));
            }

            return args;
        }

        private void ButtonSearchStart_Click(object sender, RoutedEventArgs e)
        {
            var st = SearchFor.Text.Trim().ToLower();
            if (st.Length < 2)
            {
                MessageBox.Show(this, "The search string needs to comprise at least 2 characters in order to limit the amount of search results!", "Info", MessageBoxButton.OK, MessageBoxImage.Exclamation);
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
                    SearchProgress.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => this.SearchProgress.Value = frac));
                });

            if (a?.jobData != null && a.jobType == 2)
                EclassUtils.SearchForIRDIinEclassFiles(a.jobData, (frac) =>
                {
                    SearchProgress.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => this.SearchProgress.Value = frac));
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
                    MessageBox.Show(this, "Too many search result. Search aborted!", "Search entities", MessageBoxButton.OK, MessageBoxImage.Exclamation);

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
                    this.DialogResult = true;
            }
        }

        private void ButtonSelect_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBoxTwoPass.IsChecked != true)
            {
                // trivial case, take directly
                if (FinalizeSelection())
                    this.DialogResult = true;
                return;
            }

            // OK, search for IRDI first
            var si = EntityList.SelectedItem as EclassUtils.SearchItem;
            if (si != null && si.Entity == "prop" && si.ContentNode != null)
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

                        worker.RunWorkerAsync(args);
                    }

                }
            }
        }

        private void Tree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ButtonSelect_Click(sender, e);
        }

        private void EntityList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // access
            if (EntityContent == null || EntityList == null || EntityList.SelectedItem == null || !(EntityList.SelectedItem is EclassUtils.SearchItem))
                return;

            // set
            var si = EntityList.SelectedItem as EclassUtils.SearchItem;
            if (si?.ContentNode != null)
                EntityContent.Text = si.ContentNode.OuterXml;
            else
                EntityContent.Text = "";
        }

        private bool FinalizeSelection()
        {
            // get the IRDI
            var si = EntityList.SelectedItem as EclassUtils.SearchItem;
            if (si == null)
                return false;

            // simply put the IRDI
            this.ResultIRDI = si.IRDI;

            // special case: property selected
            if (si.Entity == "prop")
            {
                var input = new List<EclassUtils.SearchItem>();
                input.Add(si);
                foreach (EclassUtils.SearchItem di in EntityList.Items)
                    if (di != null && di.Entity == si.Entity && di.IRDI == si.IRDI && si != di)
                        input.Add(di);

                // own function
                this.ResultCD = EclassUtils.GenerateConceptDescription(input, si.IRDI);
            }

            // ok
            return true;
        }

    }
}
