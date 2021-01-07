/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using AasxPackageExplorer;
using AasxWpfControlLibrary.PackageCentral;

namespace AasxWpfControlLibrary.AasxFileRepo
{
    public partial class AasxRepoListControl : UserControl
    {
        //
        // External properties
        //

        private IFlyoutProvider _flyout;
        private AasxRepoList _repoList;

        /// <summary>
        /// Window (handler) which provides flyout control for this control. Is expected to sit in the MainWindow.
        /// Note: only setter, as direct access from outside shall be redirected to the original source.
        /// </summary>
        public IFlyoutProvider FlyoutProvider { set { _flyout = value; } }
        
        /// <summary>
        /// AasxRepoList which is being managed by this control. Is expected to sit in the PackageCentral.
        /// Note: only setter, as direct access from outside shall be redirected to the original source.
        /// </summary>
        public AasxRepoList RepoList { set { _repoList = value; } }

        private List<AasxFileRepoControl> _repoControls = new List<AasxFileRepoControl>();
        public List<AasxFileRepoControl> RepoControls { get { return _repoControls; } }

        //
        // Constructor
        //

        public AasxRepoListControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        // 
        // binding to ObservableCollection
        //

        public void BindToRepoList()
        {
            _repoList.CollectionChanged += RepoList_CollectionChanged;
        }

        private void RepoList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // see: https://blog.stephencleary.com/2009/07/interpreting-notifycollectionchangedeve.html
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    _repoControls.Clear();
                    break;

                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        if (e.NewStartingIndex >= 0)
                        {
                            // insert add a specific position
                        }
                        else
                        {
                            // simply add one by one
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                        foreach (var oi in e.OldItems)
                        {
                        }
                    break;

                case NotifyCollectionChangedAction.Replace:
                    break;

                case NotifyCollectionChangedAction.Move:
                    break;

            }
        }

        //
        // UI higher-level stuff (taken over and maintained in from MainWindow.CommandBindings.cs)
        //

        public void CommandBinding_FileRepoAll(string cmd)
        {
            // access
            if (cmd == null)
                return;
            cmd = cmd.ToLower().Trim();
#if cdscdscdsd
            if (cmd == "filereponew")
            {
                if (MessageBoxResult.OK != MessageBoxFlyoutShow(
                        "Create new (empty) file repository? Pending changes might be unsaved!",
                        "AASX File Repository",
                        MessageBoxButton.OKCancel, MessageBoxImage.Hand))
                    return;

                this.UiSetFileRepository(new AasxFileRepository());
            }

            if (cmd == "filerepoopen")
            {
                // ask for the file
                var dlg = new Microsoft.Win32.OpenFileDialog();
                dlg.InitialDirectory = DetermineInitialDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
                dlg.Filter = "JSON files (*.JSON)|*.json|All files (*.*)|*.*";
                if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                var res = dlg.ShowDialog();
                if (Options.Curr.UseFlyovers) this.CloseFlyover();

                if (res == true)
                {
                    var fr = this.UiLoadFileRepository(dlg.FileName);
                    if (fr != null)
                        this.UiSetFileRepository(fr);
                }
            }

            if (cmd == "filereposaveas")
            {
                // any repository
                if (packages.FileRepository == null)
                {
                    MessageBoxFlyoutShow(
                        "No repository currently opened!",
                        "AASX File Repository",
                        MessageBoxButton.OK, MessageBoxImage.Hand);

                    return;
                }

                // prepare dialogue
                var outputDlg = new Microsoft.Win32.SaveFileDialog();
                outputDlg.InitialDirectory = DetermineInitialDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
                outputDlg.Title = "Select AASX file repository to be saved";
                outputDlg.FileName = "new-aasx-repo.json";

                if (packages.FileRepository?.Filename?.HasContent() == true)
                {
                    outputDlg.InitialDirectory = Path.GetDirectoryName(packages.FileRepository.Filename);
                    outputDlg.FileName = Path.GetFileName(packages.FileRepository.Filename);
                }

                outputDlg.DefaultExt = "*.json";
                outputDlg.Filter = "AASX repository files (*.json)|*.json|All files (*.*)|*.*";

                if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                var res = outputDlg.ShowDialog();
                if (Options.Curr.UseFlyovers) this.CloseFlyover();

                if (res != true)
                    return;

                // OK!
                var fn = outputDlg.FileName;

                if (packages.FileRepository == null)
                {
                    AasxPackageExplorer.Log.Singleton.Error("No file repository open to be saved. Aborting.");
                    return;
                }

                try
                {
                    AasxPackageExplorer.Log.Singleton.Info($"Saving AASX file repository to {fn} ..");
                    packages.FileRepository.SaveAs(fn);
                }
                catch (Exception ex)
                {
                    AasxPackageExplorer.Log.Singleton.Error(ex, $"When saving AASX file repository to {fn}");
                }
            }

            if (cmd == "filerepoclose")
            {
                if (MessageBoxResult.OK != MessageBoxFlyoutShow(
                        "Close file repository? Pending changes might be unsaved!",
                        "AASX File Repository",
                        MessageBoxButton.OKCancel, MessageBoxImage.Hand))
                    return;

                this.UiSetFileRepository(null);
            }

            if (cmd == "filerepomakerelative")
            {
                // access
                if (packages.FileRepository == null || packages.FileRepository.Filename == null)
                {
                    MessageBoxFlyoutShow(
                        "No repository currently opened!",
                        "AASX File Repository",
                        MessageBoxButton.OK, MessageBoxImage.Hand);

                    return;
                }

                // execute (is data binded)
                try
                {
                    AasxPackageExplorer.Log.Singleton.Info("Make AASX file names relative to {0}", Path.GetFullPath(
                        Path.GetDirectoryName("" + packages.FileRepository.Filename)));
                    packages.FileRepository.MakeFilenamesRelative();
                }
                catch (Exception ex)
                {
                    AasxPackageExplorer.Log.Singleton.Error(
                        ex, $"When making AASX file names in repository relative.");
                }
            }

            if (cmd == "filerepoquery")
            {
                // access
                if (packages.FileRepository == null)
                {
                    MessageBoxFlyoutShow(
                        "No repository currently available! Please open.",
                        "AASX File Repository",
                        MessageBoxButton.OK, MessageBoxImage.Hand);

                    return;
                }

                // dialogue
                var uc = new SelectFromRepositoryFlyout();
                uc.Margin = new Thickness(10);
                if (uc.LoadAasxRepoFile(repo: packages.FileRepository))
                {
                    uc.ControlClosed += () =>
                    {
                        var fi = uc.ResultItem;
                        if (fi?.Filename != null)
                        {
                            // which file?
                            var fn = packages.FileRepository?.GetFullFilename(fi);
                            if (fn == null)
                                return;

                            // start animation
                            packages.FileRepository?.StartAnimation(fi,
                                AasxFileRepository.FileItem.VisualStateEnum.ReadFrom);

                            try
                            {
                                // load
                                AasxPackageExplorer.Log.Singleton.Info("Switching to AASX repository file {0} ..", fn);
                                UiLoadPackageWithNew(
                                    packages.MainItem, null, fn, onlyAuxiliary: false);
                            }
                            catch (Exception ex)
                            {
                                AasxPackageExplorer.Log.Singleton.Error(
                                    ex, $"When switching to AASX repository file {fn}.");
                            }
                        }

                    };
                    this.StartFlyover(uc);
                }
            }

            if (cmd == "filerepoprint")
            {
                // access
                if (packages.FileRepository == null)
                {
                    MessageBoxFlyoutShow(
                        "No repository currently available! Please open.",
                        "AASX File Repository",
                        MessageBoxButton.OK, MessageBoxImage.Hand);

                    return;
                }

                // try print
                try
                {
                    AasxPrintFunctions.PrintRepositoryCodeSheet(
                        repoDirect: packages.FileRepository, title: "AASX file repository");
                }
                catch (Exception ex)
                {
                    AasxPackageExplorer.Log.Singleton.Error(ex, "When printing, an error occurred");
                }
            }

            if (cmd == "filerepoaddcurrent")
            {
                // check
                VisualElementAdminShell ve = null;
                if (DisplayElements.SelectedItem != null && DisplayElements.SelectedItem is VisualElementAdminShell)
                    ve = DisplayElements.SelectedItem as VisualElementAdminShell;

                if (ve == null || ve.theAas == null || ve.theEnv == null || ve.thePackage == null)
                {
                    MessageBoxFlyoutShow(
                        "No valid AAS selected. Aborting.", "AASX File repository",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (packages.FileRepository == null)
                {
                    MessageBoxFlyoutShow(
                        "No repository currently available! Please create new or open.",
                        "AASX File Repository",
                        MessageBoxButton.OK, MessageBoxImage.Hand);
                    return;
                }

                // add
                packages.FileRepository.AddByAas(ve.theEnv, ve.theAas, "" + ve.thePackage?.Filename);
            }

            if (cmd == "filerepomultiadd")
            {
                // access
                if (packages.FileRepository == null)
                {
                    MessageBoxFlyoutShow(
                        "No repository currently available! Please create new or open.",
                        "AASX File Repository",
                        MessageBoxButton.OK, MessageBoxImage.Hand);

                    return;
                }

                // get the input files
                var inputDlg = new Microsoft.Win32.OpenFileDialog();
                inputDlg.InitialDirectory = DetermineInitialDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
                inputDlg.Title = "Multi-select AASX package files to be in repository";
                inputDlg.Filter = "AASX package files (*.aasx)|*.aasx|AAS XML file (*.xml)|*.xml|All files (*.*)|*.*";
                inputDlg.Multiselect = true;

                if (Options.Curr.UseFlyovers) this.StartFlyover(new EmptyFlyout());
                var res = inputDlg.ShowDialog();
                if (Options.Curr.UseFlyovers) this.CloseFlyover();

                if (res != true || inputDlg.FileNames.Length < 1)
                    return;

                RememberForInitialDirectory(inputDlg.FileName);

                // loop
                foreach (var fn in inputDlg.FileNames)
                    packages.FileRepository.AddByAasxFn(fn);
            }

            if (cmd == "filerepoaddfromserver")
            {
                // access
                if (packages.FileRepository == null)
                {
                    MessageBoxFlyoutShow(
                        "No repository currently available! Please create new or open.",
                        "AASX File Repository",
                        MessageBoxButton.OK, MessageBoxImage.Hand);

                    return;
                }

                // read server address
                var uc = new TextBoxFlyout("REST endpoint (without \"/server/listaas\"):", MessageBoxImage.Question);
                uc.Text = "http://localhost:51310";
                this.StartFlyoverModal(uc);
                if (!uc.Result)
                    return;

                // execute
                try
                {
                    var conn = new PackageConnectorHttpRest(null, new Uri(uc.Text));

                    var task = Task.Run(() => conn.GenerateRepositoryFromEndpointAsync());
                    var items = task.Result;
                    if (items == null || items.Count < 1)
                    {
                        Log.Singleton.Error($"When adding file repo items from REST server {uc.Text}," +
                            $"the function returned NO items!");
                    }

                    // loop
                    foreach (var fi in items)
                        packages.FileRepository.Add(fi);
                }
                catch (Exception ex)
                {
                    Log.Singleton.Error(ex, $"When adding file repo items from REST server {uc.Text}, " +
                        $"an error occurred");
                }
            }
#endif
        }


        //
        // Mechanics (of the control)
        //
    }
}
