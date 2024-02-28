/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AasxIntegrationBaseWpf;
using AasxPackageExplorer;
using AasxPackageLogic;
using AasxPackageLogic.PackageCentral;
using AasxPackageLogic.PackageCentral.AasxFileServerInterface;
using AdminShellNS;
using AnyUi;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AasxWpfControlLibrary.PackageCentral
{
    public partial class PackageContainerListOfListControl : UserControl
    {
        //
        // External properties
        //

        public event Action<Control, PackageContainerListBase, PackageContainerRepoItem> FileDoubleClick;
        public event Action<Control, PackageContainerListBase, string[]> FileDrop;

        private AasxPackageLogic.PackageCentral.PackageCentral _packageCentral;
        private IFlyoutProvider _flyout;
        private IManageVisualAasxElements _manageVisuElems;
        private PackageContainerListOfList _repoList;

        /// <summary>
        /// In order to directly create new valid items, PakcageCentral shall be given
        /// </summary>
        public AasxPackageLogic.PackageCentral.PackageCentral PackageCentral { set { _packageCentral = value; } }

        /// <summary>
        /// Window (handler) which provides flyout control for this control. Is expected to sit in the MainWindow.
        /// Note: only setter, as direct access from outside shall be redirected to the original source.
        /// </summary>
        public IFlyoutProvider FlyoutProvider { set { _flyout = value; } }

        /// <summary>
        /// Handler, which can provide the currently selected visual elements in the AASX tree.
        /// </summary>
        public IManageVisualAasxElements ManageVisuElems { set { _manageVisuElems = value; } }

        /// <summary>
        /// AasxRepoList which is being managed by this control. Is expected to sit in the PackageCentral.
        /// Note: only setter, as direct access from outside shall be redirected to the original source.
        /// </summary>
        public PackageContainerListOfList RepoList
        {
            get
            {
                return _repoList;
            }
            set
            {
                _repoList = value;
                StackPanelRepos.ItemsSource = RepoList;
                this.UpdateLayout();
            }
        }

        //
        // Constructor
        //

        public PackageContainerListOfListControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            StackPanelRepos.ItemsSource = RepoList;
        }

        protected void RedrawListFull()
        {
            var onoff = RepoList;
            RepoList = null;
            RepoList = onoff;
        }

        //
        // UI higher-level stuff (taken over and maintained in from MainWindow.CommandBindings.cs)
        //

        public async Task CommandBinding_FileRepoAll(Control senderList, PackageContainerListBase fr, string cmd)
        {
            // access
            if (cmd == null || _flyout == null)
                return;
            cmd = cmd.ToLower().Trim();

            // modify list
            if (fr != null && RepoList != null && RepoList.Contains(fr))
            {
                if (cmd == "filerepoclose")
                {
                    if (AnyUiMessageBoxResult.OK != await _flyout?.GetDisplayContext()?.MessageBoxFlyoutShowAsync(
                            "Close file repository? Pending changes might be unsaved!",
                            "AASX File Repository",
                            AnyUiMessageBoxButton.OKCancel, AnyUiMessageBoxImage.Hand))
                        return;

                    RepoList.Remove(fr);
                }

                if (cmd == "filerepoeditname")
                {
                    var uc = new AnyUiDialogueDataTextBox("Edit name of repository");
                    uc.Text = fr.Header;
                    if (await _flyout?.GetDisplayContext()?.StartFlyoverModalAsync(uc))
                    {
                        fr.Header = uc.Text;

                        // damage full repo list
                        RedrawListFull();
                    }
                }

                if (cmd == "item-up")
                {
                    // TODO (MIHO, 2021-01-09): check to use moveup/down of the PackageContainerListBase
                    int i = RepoList.IndexOf(fr);
                    if (i > 0)
                    {
                        RepoList.RemoveAt(i);
                        RepoList.Insert(i - 1, fr);
                    }
                }

                if (cmd == "item-down")
                {
                    // TODO (MIHO, 2021-01-09): check to use moveup/down of the PackageContainerListBase
                    int i = RepoList.IndexOf(fr);
                    if (i < RepoList.Count - 1)
                    {
                        RepoList.RemoveAt(i);
                        RepoList.Insert(i + 1, fr);
                    }
                }

                if (cmd == "filereposaveas")
                {
                    var uc = new AnyUiDialogueDataSaveFile(
                        caption: "Save AASX repository",
                        message: "Select AASX file repository to be saved.",
                        filter: "AASX repository files (*.json)|*.json|All files (*.*)|*.*",
                        proposeFn: "new-aasx-repo.json");

                    if (fr is PackageContainerListLocal frl && frl.Filename.HasContent())
                        uc.ProposeFileName = frl.Filename;

                    if (!(await _flyout?.GetDisplayContext()?.StartFlyoverModalAsync(uc))
                        || !uc.Result)
                        return;

                    // OK!
                    var fn = uc.TargetFileName;
                    try
                    {
                        Log.Singleton.Info($"Saving AASX file repository to {fn} ..");
                        fr.SaveAsLocalFile(fn);
                        if (fr is PackageContainerListLocal frl2)
                            frl2.Filename = fn;
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(ex, $"When saving AASX file repository to {fn}");
                    }
                }

                if (cmd == "filerepoquery")
                {
                    // dialogue
                    var uc = new AnyUiDialogueDataSelectFromRepository("Select from: " + fr.Header);
                    uc.Items = fr.EnumerateItems().ToList();
                    if (await _flyout?.GetDisplayContext()?.StartFlyoverModalAsync(uc)
                        && uc.ResultItem != null)
                    {
                        var fi = uc.ResultItem;
                        var fn = fi?.Location;
                        if (fn != null)
                        {
                            // start animation
                            fr.StartAnimation(fi,
                                PackageContainerRepoItem.VisualStateEnum.ReadFrom);

                            try
                            {
                                // load
                                Log.Singleton.Info("Switching to AASX repository file {0} ..", fn);
                                FileDoubleClick?.Invoke(senderList, fr, fi);
                            }
                            catch (Exception ex)
                            {
                                Log.Singleton.Error(
                                    ex, $"When switching to AASX repository file {fn}.");
                            }
                        }
                    }
                }

                if (cmd == "filerepoloadallresident")
                    if (fr is PackageContainerListLocalBase frlb
                        && !(fr is PackageContainerListLastRecentlyUsed))
                    {
                        foreach (var fi in frlb.EnumerateItems())
                        {
							await fi.LoadResidentIfPossible(frlb.GetFullItemLocation(fi.Location));
							Log.Singleton.Info($"Repository item {fi.Location} loaded.");
						}
                    }

                if (cmd == "filerepomakerelative")
                    if (fr is PackageContainerListLocal frl)
                    {
                        // make sure
                        if (AnyUiMessageBoxResult.OK != await _flyout.GetDisplayContext()?.MessageBoxFlyoutShowAsync(
                                "Make filename relative to the locaton of the file repository? " +
                                "This enables re-locating the repository.",
                                "AASX File Repository",
                                AnyUiMessageBoxButton.OKCancel, AnyUiMessageBoxImage.Hand))
                            return;

                        if (!frl.Filename.HasContent())
                        {
                            Log.Singleton.Error("AASX file repository has no valid filename!");
                            return;
                        }

                        // execute (is data binded)
                        try
                        {
                            Log.Singleton.Info("Make AASX file names relative to {0}",
                                Path.GetFullPath(Path.GetDirectoryName("" + frl.Filename)));
                            frl.MakeFilenamesRelative();
                        }
                        catch (Exception ex)
                        {
                            Log.Singleton.Error(
                                ex, $"When making AASX file names in repository relative.");
                        }
                    }

                if (cmd == "filerepoprint")
                {
                    // try print
                    try
                    {
                        AasxPrintFunctions.PrintRepositoryCodeSheet(
                            repoDirect: fr, title: "AASX file repository");
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(ex, "When printing, an error occurred");
                    }
                }

                if (cmd == "filerepoaddcurrent")
                {
                    // check
                    var veAas = _manageVisuElems?.GetSelectedItem() as VisualElementAdminShell;

                    var veEnv = veAas?.FindFirstParent((ve) =>
                    (ve is VisualElementEnvironmentItem vev
                    && vev.theItemType == VisualElementEnvironmentItem.ItemType.Package), includeThis: false)
                        as VisualElementEnvironmentItem;

                    if (veAas == null || veAas.theAas == null || veAas.theEnv == null || veAas.thePackage == null
                        || veEnv == null || !veEnv.thePackageSourceFn.HasContent())
                    {
                        await _flyout?.GetDisplayContext()?.MessageBoxFlyoutShowAsync(
                            "No valid AAS selected. The application needs to be in edit mode. " +
                            "Aborting.", "AASX File repository",
                            AnyUiMessageBoxButton.OK, AnyUiMessageBoxImage.Error);
                        return;
                    }

                    // generate appropriate container
                    var cnt = PackageContainerFactory.GuessAndCreateFor(
                        null, veEnv.thePackageSourceFn, veEnv.thePackageSourceFn,
                        overrideLoadResident: false,
                        containerOptions: PackageContainerOptionsBase.CreateDefault(Options.Curr));
                    if (cnt is PackageContainerRepoItem ri)
                    {
                        ri.Env = veAas.thePackage;
                        ri.CalculateIdsTagAndDesc();
                        fr.Add(ri);
                    }
                }

                if (cmd == "filerepoaddtoserver")
                {
                    var uc = new AnyUiDialogueDataOpenFile(
                        caption: "AASX Package File upload",
                        message: "Select the AASX Package File to be uploaded in the file repository.",
                        filter: "AASX package files (*.aasx)|*.aasx|AAS XML file (*.xml)|*.xml|All files (*.*)|*.*");

                    if (!(await _flyout?.GetDisplayContext()?.StartFlyoverModalAsync(uc))
                        || !uc.Result)
                        return;

                    var fileName = uc.OriginalFileName;
                    if (fr is PackageContainerAasxFileRepository fileRepo)
                    {
                        //Add the file to File Server
                        int packageId = fileRepo.AddPackageToServer(fileName);
                        if (packageId != -1)
                        {
                            fileRepo.LoadAasxFile(_packageCentral, fileName, packageId);
                        }
                    }
                }

                if (cmd == "filerepomultiadd")
                {
                    // get the input files
                    var uc = new AnyUiDialogueDataOpenFile(
                        caption: "AASX Package File upload",
                        message: "Multi-select AASX package files to be in repository.",
                        filter: "AASX package files (*.aasx)|*.aasx" +
                                "|AAS XML file (*.xml)|*.xml|All files (*.*)|*.*");
                    uc.Multiselect = true;

                    if (!(await _flyout?.GetDisplayContext()?.StartFlyoverModalAsync(uc))
                        || !uc.Result)
                        return;
                    // dead-csharp off
                    //var inputDlg = new Microsoft.Win32.OpenFileDialog();
                    //inputDlg.Title = "Multi-select AASX package files to be in repository";
                    //inputDlg.Filter = "AASX package files (*.aasx)|*.aasx" +
                    //    "|AAS XML file (*.xml)|*.xml|All files (*.*)|*.*";
                    //inputDlg.Multiselect = true;

                    //_flyout.StartFlyover(new EmptyFlyout());
                    //var res = inputDlg.ShowDialog();
                    //_flyout.CloseFlyover();

                    //if (res != true || inputDlg.FileNames.Length < 1)
                    //    return;

                    //// loop
                    //foreach (var fn in inputDlg.FileNames)
                    //    fr.AddByAasxFn(_packageCentral, fn);
                    // dead-csharp on
                    // loop
                    foreach (var fn in uc.Filenames)
                        fr.AddByAasxFn(_packageCentral, fn);
                }

                if (cmd == "filerepoaddfromserver")
                {
                    // read server address
                    var uc = new AnyUiDialogueDataTextBox("REST endpoint (without \"/server/listaas\"):",
                        symbol: AnyUiMessageBoxImage.Question);
                    uc.Text = Options.Curr.DefaultConnectRepositoryLocation;
                    await _flyout?.GetDisplayContext()?.StartFlyoverModalAsync(uc);
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
                            return;
                        }

                        // loop
                        foreach (var fi in items)
                            fr.Add(fi);
                    }
                    catch (Exception ex)
                    {
                        Log.Singleton.Error(ex, $"When adding file repo items from REST server {uc.Text}, " +
                            $"an error occurred");
                    }
                }

            }
        }

        //
        // Mechanics (of the control)
        //

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // DIY -> this catches early the scroll events, before they are consumed by the individual
            // ListView of the child repo controls
            ScrollViewerRepoList.ScrollToVerticalOffset(ScrollViewerRepoList.VerticalOffset - e.Delta);
        }

        private void PackageContainerListControl_FileDoubleClick(
            Control senderList,
            PackageContainerListBase fr, PackageContainerRepoItem fi)
        {
            FileDoubleClick?.Invoke(senderList, fr, fi);
        }

        private async Task PackageContainerListControl_ButtonClick(
            Control senderList,
            PackageContainerListBase fr, PackageContainerListControl.CustomButton btn,
            Button sender)
        {
            if (btn == PackageContainerListControl.CustomButton.Context)
            {
                var menu = new AasxMenu()
                    .AddAction("FileRepoClose", "Close", icon: "\u274c")
                    .AddAction("FileRepoEditName", "Edit name", icon: "\u270e");

                if (!(fr is PackageContainerListLastRecentlyUsed))
                {
                    menu.AddAction("item-up", "Move Up", icon: "\u25b2")
                        .AddAction("item-down", "Move Down", icon: "\u25bc");
                }

                menu.AddSeparator()
                    .AddAction("FileRepoSaveAs", "Save as ..", icon: "\U0001f4be")
                    .AddSeparator();

                if (!(fr is PackageContainerListLastRecentlyUsed))
                {
					menu.AddAction(
                    	"FileRepoLoadAllResident", "Load all resident files ..", icon: "\U0001f503");

					if (fr is PackageContainerListLocal)
                    {
                        menu.AddAction(
                            "FileRepoMakeRelative", "Make AASX filenames relative ..", icon: "\u2699");
					}

					menu.AddAction("FileRepoAddCurrent", "Add current AAS", icon: "\u2699")
                        .AddAction("FileRepoAddToServer", "Add AASX File to File Repository", icon: "\u2699")
                        .AddAction("FileRepoMultiAdd", "Add multiple AASX files ..", icon: "\u2699")
                        .AddAction("FileRepoAddFromServer", "Add from REST server ..", icon: "\u2699")
                        .AddAction("FileRepoPrint", "Print 2D code sheet ..", icon: "\u2699");
                }

                var cm2 = DynamicContextMenu.CreateNew(
                    menu.AddLambda(async (name, mi, ticket) =>
                    {
                        await CommandBinding_FileRepoAll(senderList, fr, name);
                    }));

                cm2.Start(sender as Button);
            }

            if (btn == PackageContainerListControl.CustomButton.Query)
            {
                await CommandBinding_FileRepoAll(senderList, fr, "FileRepoQuery");
            }
        }

        private void PackageContainerListControl_FileDrop(
            Control senderList,
            PackageContainerListBase fr, string[] files)
        {
            FileDrop?.Invoke(senderList, fr, files);
        }

        private void UserControl_Drop(object sender, DragEventArgs e)
        {
            if (!e.Handled && e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // simply pass over to upper layer to decide, how to finally handle
                e.Handled = true;
                FileDrop?.Invoke(null, null, files);
            }

        }

    }
}
