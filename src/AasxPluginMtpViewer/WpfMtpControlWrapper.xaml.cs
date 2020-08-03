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

        private WpfMtpControl.MtpVisualObjectLib activeVisualObjectLib = null;
        private WpfMtpControl.MtpData activeMtpData = null;

        private AdminShell.File activeMtpFileElem = null;

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
            // init library
            activeVisualObjectLib = new WpfMtpControl.MtpVisualObjectLib();
            activeVisualObjectLib.LoadStatic(null);

            // find file, remember Submodel element for it, find filename
            // (ConceptDescription)(no-local)[IRI]http://www.admin-shell.io/mtp/v1/MTPSUCLib/ModuleTypePackage
            var simIdFn = new AdminShell.Key("ConceptDescription", false,
                "IRI", "http://www.admin-shell.io/mtp/v1/MTPSUCLib/ModuleTypePackage");
            this.activeMtpFileElem = theSubmodel?.submodelElements?.FindFirstSemanticIdAs<AdminShell.File>(simIdFn);
            var inputFn = this.activeMtpFileElem?.value;
            if (inputFn == null)
                return;

            // access file
            if (CheckIfPackageFile(inputFn))
                inputFn = thePackage.MakePackageFileAvailableAsTempFile(inputFn);

            // load file
            LoadFile(inputFn);

            // fit it
            this.mtpVisu.ZoomToFitCanvas();

            // double click handler
            this.mtpVisu.MtpObjectDoubleClick += MtpVisu_MtpObjectDoubleClick;
        }

        private bool CheckIfPackageFile(string fn)
        {
            return fn.StartsWith(@"/");
        }

        private void LoadFile(string fn)
        {
            if (!".aml .zip .mtp".Contains(System.IO.Path.GetExtension(fn.Trim().ToLower())))
                return;
            // this.client = new WpfMtpControl.MtpVisuOpcUaClient();
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
                    hit = hit || rel.first.Matches(mtpFileElemReference
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
