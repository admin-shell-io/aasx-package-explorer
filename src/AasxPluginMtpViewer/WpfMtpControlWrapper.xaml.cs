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
using WpfMtpControl;

namespace AasxPluginMtpViewer
{
    /// <summary>
    /// Interaktionslogik für WpfMtpControlWrapper.xaml
    /// </summary>
    public partial class WpfMtpControlWrapper : UserControl
    {
        private AdminShellPackageEnv thePackage = null;
        private AdminShell.Submodel theSubmodel = null;
        private AasxPluginMtpViewer.MtpViewerOptions theOptions = null;
        private PluginEventStack theEventStack = null;

        private DefinitionsMTP.ModuleTypePackage theDefs = null;

        private WpfMtpControl.MtpSymbolLib theSymbolLib = null;
        private WpfMtpControl.MtpVisualObjectLib activeVisualObjectLib = null;
        private WpfMtpControl.MtpData activeMtpData = null;

        private AdminShell.File activeMtpFileElem = null;
        private string activeMtpFileFn = null;
        private Dictionary<string, string> activeEndpointMapping = new Dictionary<string, string>();

        public WpfMtpControlWrapper()
        {
            InitializeComponent();
        }

        public void Start(
            AdminShellPackageEnv thePackage,
            AdminShell.Submodel theSubmodel,
            AasxPluginMtpViewer.MtpViewerOptions theOptions,
            PluginEventStack eventStack)
        {
            this.thePackage = thePackage;
            this.theSubmodel = theSubmodel;
            this.theOptions = theOptions;
            this.theEventStack = eventStack;

            this.theDefs = new DefinitionsMTP.ModuleTypePackage(new DefinitionsMTP());
        }

        public static WpfMtpControlWrapper FillWithWpfControls(
            object opackage, object osm,
            AasxPluginMtpViewer.MtpViewerOptions options,
            PluginEventStack eventStack,
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
            wrapperCntl.Start(package, sm, options, eventStack);
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
            var ok = GatherMtpInfos();
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
        }

        private bool GatherMtpInfos()
        {
            // clear mappings
            this.activeEndpointMapping = new Dictionary<string, string>();

            // access
            var env = this.thePackage?.AasEnv;
            if (this.theSubmodel?.semanticId == null || this.theSubmodel.submodelElements == null
                || this.theDefs == null
                || env?.AdministrationShells == null
                || this.thePackage.AasEnv.Submodels == null)
                return false;

            // need to find the type Submodel
            AdminShell.Submodel mtpTypeSm = null;

            // check, if the user pointed to the instance submodel
            if (this.theSubmodel.semanticId.Matches(this.theDefs.SEM_MtpInstanceSubmodel))
            {
                // gather infos
                foreach (var srcLst in this.theSubmodel.submodelElements
                    .FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                        this.theDefs.CD_SourceList?.GetReference(), AdminShell.Key.MatchMode.Relaxed))
                {
                    // found a source list, might contain sources
                    if (srcLst?.value == null)
                        continue;

                    // UA Server?
                    foreach (var src in srcLst.value.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                        this.theDefs.CD_SourceOpcUaServer?.GetReference(), AdminShell.Key.MatchMode.Relaxed))
                        if (src?.value != null)
                        {
                            // UA server
                            var ep = src.value.FindFirstSemanticIdAs<AdminShell.Property>(
                                this.theDefs.CD_Endpoint.GetReference(), AdminShell.Key.MatchMode.Relaxed)?.value;

                            // add
                            this.activeEndpointMapping[("" + src.idShort).Trim()] = "" + ep;
                        }
                }

                // according spec from Sten Gruener, the AAS.derivedFrom relationship shall be exploited.
                // How to get from subModel to AAS?
                var instanceAas = env.FindAASwithSubmodel(this.theSubmodel.identification);
                var typeAas = env.FindReferableByReference(instanceAas?.derivedFrom) as AdminShell.AdministrationShell;
                if (instanceAas?.derivedFrom != null && typeAas != null)
                    foreach (var msm in env.FindAllSubmodelGroupedByAAS((aas, sm) =>
                    {
                        return aas == typeAas && true == sm?.semanticId?.Matches(this.theDefs.SEM_MtpSubmodel);
                    }))
                    {
                        mtpTypeSm = msm;
                        break;
                    }

                // another possibility: direct reference
                var dirLink = this.theSubmodel.submodelElements
                    .FindFirstSemanticIdAs<AdminShell.ReferenceElement>(
                        this.theDefs.CD_MtpTypeSubmodel?.GetReference(), AdminShell.Key.MatchMode.Relaxed);
                var dirLinkSm = env.FindReferableByReference(dirLink?.value) as AdminShell.Submodel;
                if (mtpTypeSm == null)
                    mtpTypeSm = dirLinkSm;

            }

            // other (not intended) case: user points to type submodel directly
            if (mtpTypeSm == null
                && this.theSubmodel.semanticId.Matches(this.theDefs.SEM_MtpSubmodel))
                mtpTypeSm = this.theSubmodel;

            // ok, is there a type submodel?
            if (mtpTypeSm == null)
                return false;

            // find file, remember Submodel element for it, find filename
            // (ConceptDescription)(no-local)[IRI]http://www.admin-shell.io/mtp/v1/MTPSUCLib/ModuleTypePackage
            this.activeMtpFileElem = mtpTypeSm.submodelElements?
                .FindFirstSemanticIdAs<AdminShell.File>(this.theDefs.CD_MtpFile.GetReference(),
                    AdminShell.Key.MatchMode.Relaxed);
            var inputFn = this.activeMtpFileElem?.value;
            if (inputFn == null)
                return false;
            this.activeMtpFileFn = inputFn;

            return true;
        }

        private bool CheckIfPackageFile(string fn)
        {
            return fn.StartsWith(@"/");
        }

        private void LoadFile(string fn)
        {
            if (!".aml .zip .mtp".Contains(System.IO.Path.GetExtension(fn.Trim().ToLower())))
                return;

            this.activeMtpData = new WpfMtpControl.MtpData();
            this.activeMtpData.LoadAmlOrMtp(activeVisualObjectLib, null, null, fn);
            if (this.activeMtpData.PictureCollection.Count > 0)
                mtpVisu.SetPicture(this.activeMtpData.PictureCollection.Values.ElementAt(0));
            mtpVisu.RedrawMtp();
        }

        private void MtpVisu_MtpObjectDoubleClick(MtpData.MtpBaseObject source)
        {
            // access
            if (source == null || this.activeMtpFileElem == null || this.theSubmodel?.submodelElements == null)
                return;

            // for the active file, find a Reference for it
            var mtpFileElemReference = this.activeMtpFileElem.GetReference();

            // inside the Submodel .. look out for Relations
            // (ConceptDescription)(no-local)[IRI]http://www.admin-shell.io/mtp/1/0/documentationReference
            var relKey = new AdminShell.Key("ConceptDescription", false,
                "IRI", "http://www.admin-shell.io/mtp/1/0/documentationReference");
            var searchRelation = this.theSubmodel?.submodelElements.FindDeep<AdminShell.RelationshipElement>(
            (candidate) =>
            {
                return true == candidate?.semanticId?.MatchesExactlyOneKey(relKey, AdminShell.Key.MatchMode.Relaxed);
            });
            foreach (var rel in searchRelation)
            {
                // access
                if (rel.first == null || rel.second == null)
                    continue;

                // do some "math"
                var hit = false;
                if (source.Name != null)
                    hit = rel.first.Matches(mtpFileElemReference
                        + (new AdminShell.Key(
                            AdminShell.Key.GlobalReference, true, AdminShell.Key.Custom, source.Name)),
                        AdminShell.Key.MatchMode.Relaxed);
                if (source.RefID != null)
                    hit = hit || rel.first.Matches(mtpFileElemReference
                        + (new AdminShell.Key(
                            AdminShell.Key.GlobalReference, true, AdminShell.Key.Custom, source.RefID)),
                        AdminShell.Key.MatchMode.Relaxed);

                // yes?
                if (hit)
                {
                    var evt = new AasxPluginResultEventNavigateToReference();
                    evt.targetReference = new AdminShell.Reference(rel.second);
                    this.theEventStack.PushEvent(evt);
                }
            }
        }
    }
}
