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
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AasxIntegrationBase;
using AasxPredefinedConcepts;
using AdminShellNS;
using Newtonsoft.Json;
using WpfMtpControl;
using WpfMtpControl.DataSources;

namespace AasxPluginMtpViewer
{
    public partial class WpfMtpControlWrapper : UserControl
    {
        // internal members

        private LogInstance theLog = null;
        private AdminShellPackageEnv thePackage = null;
        private AdminShell.Submodel theSubmodel = null;
        private AasxPluginMtpViewer.MtpViewerOptions theOptions = null;
        private PluginEventStack theEventStack = null;

        private AasxPredefinedConcepts.DefinitionsExperimental.InteropRelations defsInterop = null;
        private DefinitionsMTP.ModuleTypePackage defsMtp = null;

        public MtpDataSourceOpcUaPreLoadInfo thePreLoadInfo = new MtpDataSourceOpcUaPreLoadInfo();

        private WpfMtpControl.MtpSymbolLib theSymbolLib = null;
        private WpfMtpControl.MtpVisualObjectLib activeVisualObjectLib = null;
        private WpfMtpControl.MtpData activeMtpData = null;

        private AdminShell.File activeMtpFileElem = null;
        private string activeMtpFileFn = null;

        public WpfMtpControl.MtpVisuOpcUaClient client = new WpfMtpControl.MtpVisuOpcUaClient();

        private MtpDataSourceSubscriber activeSubscriber = null;

        private MtpSymbolMapRecordList hintsForConfigRecs = null;

        // window / plugin mechanics

        public WpfMtpControlWrapper()
        {
            InitializeComponent();

            // use pre-definitions
            this.defsInterop = new AasxPredefinedConcepts.DefinitionsExperimental.InteropRelations();
            this.defsMtp = new DefinitionsMTP.ModuleTypePackage();
        }

        public void Start(
            AdminShellPackageEnv thePackage,
            AdminShell.Submodel theSubmodel,
            AasxPluginMtpViewer.MtpViewerOptions theOptions,
            PluginEventStack eventStack,
            LogInstance log)
        {
            this.thePackage = thePackage;
            this.theSubmodel = theSubmodel;
            this.theOptions = theOptions;
            this.theEventStack = eventStack;
            this.theLog = log;
        }

        public static WpfMtpControlWrapper FillWithWpfControls(
            object opackage, object osm,
            AasxPluginMtpViewer.MtpViewerOptions options,
            PluginEventStack eventStack,
            LogInstance log,
            object masterDockPanel)
        {
            // access
            var package = opackage as AdminShellPackageEnv;
            var sm = osm as AdminShell.Submodel;
            var master = masterDockPanel as DockPanel;
            if (package == null || sm == null || master == null)
                return null;

            // the Submodel elements need to have parents
            sm.SetAllParents();

            // create TOP control
            var wrapperCntl = new WpfMtpControlWrapper();
            wrapperCntl.Start(package, sm, options, eventStack, log);
            master.Children.Add(wrapperCntl);

            // return shelf
            return wrapperCntl;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // initialize symbol library
            this.theSymbolLib = new MtpSymbolLib();

            var ISO10628 = new ResourceDictionary();
            ISO10628.Source = new Uri(
                "pack://application:,,,/WpfMtpControl;component/Resources/PNID_DIN_EN_ISO_10628.xaml");
            this.theSymbolLib.ImportResourceDicrectory("PNID_ISO10628", ISO10628);

            var FESTO = new ResourceDictionary();
            FESTO.Source = new Uri(
                "pack://application:,,,/WpfMtpControl;component/Resources/PNID_Festo.xaml");
            this.theSymbolLib.ImportResourceDicrectory("PNID_Festo", FESTO);

            // initialize visual object libraries
            activeVisualObjectLib = new WpfMtpControl.MtpVisualObjectLib();
            activeVisualObjectLib.LoadStatic(this.theSymbolLib);

            // gather infos
            var ok = GatherMtpInfos(this.thePreLoadInfo);
            if (ok && this.activeMtpFileFn != null)
            {
                // access file
                var inputFn = this.activeMtpFileFn;
                if (CheckIfPackageFile(inputFn))
                    inputFn = thePackage.MakePackageFileAvailableAsTempFile(inputFn);

                // load file
                LoadFile(inputFn);

                // fit it
                this.mtpVisu.ZoomToFitCanvas();

                // double click handler
                this.mtpVisu.MtpObjectDoubleClick += MtpVisu_MtpObjectDoubleClick;
            }

            // Timer for status
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            // ReSharper disable once RedundantDelegateCreation
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            dispatcherTimer.Start();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (this.client == null)
                textBoxDataSourceStatus.Text = "(no OPC UA client enabled)";
            else
            {
                this.client.Tick(100);
                textBoxDataSourceStatus.Text = this.client.GetStatus();
            }
        }

        // handle Submodel data

        private bool GatherMtpInfos(MtpDataSourceOpcUaPreLoadInfo preLoadInfo)
        {
            // access
            var env = this.thePackage?.AasEnv;
            if (this.theSubmodel?.semanticId == null || this.theSubmodel.submodelElements == null
                || this.defsMtp == null
                || env?.AdministrationShells == null
                || this.thePackage.AasEnv.Submodels == null)
                return false;

            // need to find the type Submodel
            AdminShell.Submodel mtpTypeSm = null;

            // check, if the user pointed to the instance submodel
            if (this.theSubmodel.semanticId.Matches(this.defsMtp.SEM_MtpInstanceSubmodel))
            {
                // Source list
                foreach (var srcLst in this.theSubmodel.submodelElements
                    .FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                        this.defsMtp.CD_SourceList?.GetReference(), AdminShell.Key.MatchMode.Relaxed))
                {
                    // found a source list, might contain sources
                    if (srcLst?.value == null)
                        continue;

                    // UA Server?
                    foreach (var src in srcLst.value.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                        this.defsMtp.CD_SourceOpcUaServer?.GetReference(), AdminShell.Key.MatchMode.Relaxed))
                        if (src?.value != null)
                        {
                            // UA server
                            var ep = src.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                this.defsMtp.CD_Endpoint.GetReference(), AdminShell.Key.MatchMode.Relaxed)?.value;

                            // add
                            if (preLoadInfo?.EndpointMapping != null)
                                preLoadInfo.EndpointMapping.Add(
                                    new MtpDataSourceOpcUaEndpointMapping(
                                        "" + ep, ForName: ("" + src.idShort).Trim()));
                        }
                }

                // Identifier renaming?
                foreach (var ren in theSubmodel.submodelElements
                    .FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                    this.defsMtp.CD_IdentifierRenaming?.GetReference(), AdminShell.Key.MatchMode.Relaxed))
                    if (ren?.value != null)
                    {
                        var oldtxt = ren.value.FindFirstSemanticIdAs<AdminShell.Property>(
                            this.defsMtp.CD_RenamingOldText?.GetReference(), AdminShell.Key.MatchMode.Relaxed)?.value;
                        var newtxt = ren.value.FindFirstSemanticIdAs<AdminShell.Property>(
                            this.defsMtp.CD_RenamingNewText?.GetReference(), AdminShell.Key.MatchMode.Relaxed)?.value;
                        if (oldtxt.HasContent() && newtxt.HasContent() &&
                            preLoadInfo?.IdentifierRenaming != null)
                            preLoadInfo.IdentifierRenaming.Add(new MtpDataSourceStringReplacement(oldtxt, newtxt));
                    }

                // Namespace renaming?
                foreach (var ren in theSubmodel.submodelElements
                    .FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                    this.defsMtp.CD_NamespaceRenaming?.GetReference(), AdminShell.Key.MatchMode.Relaxed))
                    if (ren?.value != null)
                    {
                        var oldtxt = ren?.value.FindFirstSemanticIdAs<AdminShell.Property>(
                            this.defsMtp.CD_RenamingOldText?.GetReference(), AdminShell.Key.MatchMode.Relaxed)?.value;
                        var newtxt = ren?.value.FindFirstSemanticIdAs<AdminShell.Property>(
                            this.defsMtp.CD_RenamingNewText?.GetReference(), AdminShell.Key.MatchMode.Relaxed)?.value;
                        if (oldtxt.HasContent() && newtxt.HasContent() &&
                            preLoadInfo?.NamespaceRenaming != null)
                            preLoadInfo.NamespaceRenaming.Add(new MtpDataSourceStringReplacement(oldtxt, newtxt));
                    }

                // according spec from Sten Gruener, the AAS.derivedFrom relationship shall be exploited.
                // How to get from subModel to AAS?
                var instanceAas = env.FindAASwithSubmodel(this.theSubmodel.id);
                var typeAas = env.FindReferableByReference(instanceAas?.derivedFrom) as AdminShell.AdministrationShell;
                if (instanceAas?.derivedFrom != null && typeAas != null)
                    foreach (var msm in env.FindAllSubmodelGroupedByAAS((aas, sm) =>
                    {
                        return aas == typeAas && true == sm?.semanticId?.Matches(this.defsMtp.SEM_MtpSubmodel);
                    }))
                    {
                        mtpTypeSm = msm;
                        break;
                    }

                // another possibility: direct reference
                var dirLink = this.theSubmodel.submodelElements
                    .FindFirstSemanticIdAs<AdminShell.ReferenceElement>(
                        this.defsMtp.CD_MtpTypeSubmodel?.GetReference(), AdminShell.Key.MatchMode.Relaxed);
                var dirLinkSm = env.FindReferableByReference(dirLink?.value) as AdminShell.Submodel;
                if (mtpTypeSm == null)
                    mtpTypeSm = dirLinkSm;

            }

            // other (not intended) case: user points to type submodel directly
            if (mtpTypeSm == null
                && this.theSubmodel.semanticId.Matches(this.defsMtp.SEM_MtpSubmodel))
                mtpTypeSm = this.theSubmodel;

            // ok, is there a type submodel?
            if (mtpTypeSm == null)
                return false;

            // find file, remember Submodel element for it, find filename
            // (ConceptDescription)(no-local)[IRI]http://www.admin-shell.io/mtp/v1/MTPSUCLib/ModuleTypePackage
            this.activeMtpFileElem = mtpTypeSm.submodelElements?
                .FindFirstSemanticIdAs<AdminShell.File>(this.defsMtp.CD_MtpFile.GetReference(),
                    AdminShell.Key.MatchMode.Relaxed);
            var inputFn = this.activeMtpFileElem?.value;
            if (inputFn == null)
                return false;
            this.activeMtpFileFn = inputFn;

            return true;
        }

        // MTP handlings

        private bool CheckIfPackageFile(string fn)
        {
            return fn.StartsWith(@"/");
        }

        private void LoadFile(string fn)
        {
            if (!".aml .zip .mtp".Contains(System.IO.Path.GetExtension(fn.Trim().ToLower())))
                return;

            this.client = new WpfMtpControl.MtpVisuOpcUaClient();
            this.client.ItemChanged += Client_ItemChanged;
            this.activeSubscriber = new MtpDataSourceSubscriber();
            this.hintsForConfigRecs = new MtpSymbolMapRecordList();

            this.activeMtpData = new WpfMtpControl.MtpData();
            this.activeMtpData.LoadAmlOrMtp(activeVisualObjectLib,
                this.client, this.thePreLoadInfo, this.activeSubscriber, fn);

            if (this.activeMtpData.PictureCollection.Count > 0)
                mtpVisu.SetPicture(this.activeMtpData.PictureCollection.Values.ElementAt(0));
            mtpVisu.RedrawMtp();
        }

        private void Client_ItemChanged(WpfMtpControl.DataSources.IMtpDataSourceStatus dataSource,
            MtpVisuOpcUaClient.DetailItem itemRef, MtpVisuOpcUaClient.ItemChangeType changeType)
        {
            if (dataSource == null || itemRef == null || itemRef.MtpSourceItemId == null
                || this.activeSubscriber == null)
                return;

            if (changeType == MtpVisuOpcUaClient.ItemChangeType.Value)
                this.activeSubscriber.Invoke(itemRef.MtpSourceItemId, MtpDataSourceSubscriber.ChangeType.Value,
                    itemRef.Value);
        }

        private void MtpVisu_MtpObjectDoubleClick(MtpData.MtpBaseObject source)
        {
            // access
            var sme = this.theSubmodel?.submodelElements;
            var first = this.activeMtpFileElem.GetReference();
            if (source == null || this.activeMtpFileElem == null || sme == null || first == null)
                return;

            // for the active file, find a Reference for it

            foreach (var searchId in new[] { source.Name, source.RefID })
            {
                // access
                if (searchId == null)
                    continue;
                //
                // Search for FileToNavigateElement
                //

                var firstFtn = first + (new AdminShell.Key(AdminShell.Key.GlobalReference, searchId));
                this.theLog?.Info($"DblClick MTP .. search reference: {firstFtn.ToString(1)}");

                foreach (var fileToNav in sme.FindAllSemanticIdAs<AdminShell.RelationshipElement>(
                this.defsInterop?.CD_FileToNavigateElement?.GetReference(), AdminShell.Key.MatchMode.Relaxed))
                    if (fileToNav.first?.Matches(firstFtn, AdminShell.Key.MatchMode.Relaxed) == true)
                    {
                        // try activate
                        var ev = new AasxIntegrationBase.AasxPluginResultEventNavigateToReference();
                        ev.targetReference = new AdminShell.Reference(fileToNav.second);
                        this.theEventStack?.PushEvent(ev);
                        return;
                    }

                //
                // Search for FileToEntity
                //

                var firstFte = first + (new AdminShell.Key(AdminShell.Key.GlobalReference, searchId));
                this.theLog?.Info($"DblClick MTP .. search reference: {firstFte.ToString(1)}");

                foreach (var fileToEnt in sme.FindAllSemanticIdAs<AdminShell.RelationshipElement>(
                this.defsInterop?.CD_FileToEntity?.GetReference(), AdminShell.Key.MatchMode.Relaxed))
                    if (fileToEnt.first?.Matches(firstFte, AdminShell.Key.MatchMode.Relaxed) == true)
                    {
                        // debug
                        this.theLog?.Info($"try find Entity {"" + fileToEnt.second} ..");

                        // find Entity, check if self-contained
                        var foundRef = this.thePackage?.AasEnv?.FindReferableByReference(fileToEnt.second);
                        if (foundRef is AdminShell.Entity foundEnt
                            && foundEnt.GetEntityType() == AdminShell.Entity.EntityTypeEnum.SelfManagedEntity
                            && foundEnt.assetRef != null)
                        {
                            // try activate
                            var ev = new AasxIntegrationBase.AasxPluginResultEventNavigateToReference();
                            ev.targetReference = new AdminShell.Reference(foundEnt.assetRef);
                            this.theEventStack?.PushEvent(ev);
                            return;
                        }
                    }
            }
        }

        // visual window handling

        private int overlayPanelMode = 0;

        private void SetOverlayPanelMode(int newMode)
        {
            this.overlayPanelMode = newMode;

            switch (this.overlayPanelMode)
            {
                case 2:
                    this.ScrollViewerDataSources.Visibility = Visibility.Visible;
                    DataGridDataSources.ItemsSource = this.client.Items;
                    this.RichTextReport.Visibility = Visibility.Collapsed;
                    break;

                case 1:
                    this.ScrollViewerDataSources.Visibility = Visibility.Collapsed;
                    DataGridDataSources.ItemsSource = null;
                    this.RichTextReport.Visibility = Visibility.Visible;
                    ReportOnConfiguration(this.RichTextReport);
                    break;

                default:
                    this.ScrollViewerDataSources.Visibility = Visibility.Collapsed;
                    DataGridDataSources.ItemsSource = null;
                    this.RichTextReport.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender == buttonDataSourceDetails)
            {
                if (this.overlayPanelMode != 2)
                    SetOverlayPanelMode(2);
                else
                    SetOverlayPanelMode(0);
            }

            if (sender == buttonConfig)
            {
                if (this.overlayPanelMode != 1)
                    SetOverlayPanelMode(1);
                else
                    SetOverlayPanelMode(0);
            }
        }

        private void AddToRichTextBox(RichTextBox rtb, string text, bool bold = false, double? fontSize = null,
            bool monoSpaced = false)
        {
            var p = new Paragraph();
            if (bold)
                p.FontWeight = FontWeights.Bold;
            if (fontSize.HasValue)
                p.FontSize = fontSize.Value;
            if (monoSpaced)
                p.FontFamily = new FontFamily("Courier New");
            p.Inlines.Add(new Run(text));
            rtb.Document.Blocks.Add(p);
        }

        private void ReportOnConfiguration(RichTextBox rtb)
        {
            // access
            if (rtb == null)
                return;

            rtb.Document.Blocks.Clear();

            //
            // Report on available library symbols
            //

            if (this.theSymbolLib != null)
            {

                AddToRichTextBox(rtb, "Library symbols", bold: true, fontSize: 18);

                AddToRichTextBox(rtb, "The following lists shows available symbol full names.");

                foreach (var x in this.theSymbolLib.Values)
                {
                    AddToRichTextBox(rtb, "" + x.FullName, monoSpaced: true);
                }

                AddToRichTextBox(rtb, "");
            }

            //
            // Hints for configurations
            //

            if (this.hintsForConfigRecs != null)
            {
                AddToRichTextBox(rtb, "Preformatted configuration records", bold: true, fontSize: 18);
                AddToRichTextBox(rtb,
                    "The following JSON elements could be pasted into the options file named " + "" +
                    "'AasxPluginMtpViewer.options.json'. " +
                    "Prior to pasting, an appropriate symbol full name needs to be choosen from above list. " +
                    "For the eClass strings, multiples choices can be delimited by ';'. " +
                    "For EClassVersions, 'null' disables version checking. " +
                    "Either EClassClasses or EClassIRDIs shall be different to 'null'.");

                foreach (var x in this.hintsForConfigRecs)
                {
                    var txt = JsonConvert.SerializeObject(x, Formatting.None);
                    AddToRichTextBox(rtb, "" + txt, monoSpaced: true);
                }

                AddToRichTextBox(rtb, "");
            }
        }
    }
}
