using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AasxPredefinedConcepts;
using AdminShellNS;
using Newtonsoft.Json;

// ReSharper disable InconsistentlySynchronizedField
// checks and everything looks fine .. maybe .Count() is already treated as synchronized action?

namespace AasxPluginDocumentShelf
{
    /// <summary>
    /// Interaktionslogik für ShelfControl.xaml
    /// </summary>
    public partial class ShelfControl : UserControl
    {
        #region Members
        //=============

        private LogInstance Log = new LogInstance();
        private AdminShellPackageEnv thePackage = null;
        private AdminShell.Submodel theSubmodel = null;
        private AasxPluginDocumentShelf.DocumentShelfOptions theOptions = null;
        private PluginEventStack theEventStack = null;

        private string convertableFiles = ".pdf .jpeg .jpg .png .bmp .pdf .xml .txt *";

        private List<DocumentEntity> theDocEntitiesToPreview = new List<DocumentEntity>();

        #endregion

        #region View Model
        //================

        private ViewModel theViewModel = new ViewModel();

        public class ViewModel : AasxIntegrationBase.WpfViewModelBase
        {

            private int theSelectedDocClass = 0;
            public int TheSelectedDocClass
            {
                get { return theSelectedDocClass; }
                set
                {
                    theSelectedDocClass = value;
                    RaisePropertyChanged("TheSelectedDocClass");
                    RaiseViewModelChanged();
                }
            }
           
            private AasxLanguageHelper.LangEnum theSelectedLanguage = AasxLanguageHelper.LangEnum.Any;
            public AasxLanguageHelper.LangEnum TheSelectedLanguage
            {
                get { return theSelectedLanguage; }
                set
                {
                    theSelectedLanguage = value;
                    RaisePropertyChanged("TheSelectedLanguage");
                    RaiseViewModelChanged();
                }
            }

            public enum ListType { Bars, Grid };
            private ListType theSelectedListType = ListType.Bars;
            public ListType TheSelectedListType
            {
                get { return theSelectedListType; }
                set
                {
                    theSelectedListType = value;
                    RaisePropertyChanged("TheSelectedListType");
                    RaiseViewModelChanged();
                }
            }
        }

        #endregion
        #region Cache for already generated Images
        //========================================

        private static Dictionary<string, BitmapImage> referableHashToCachedBitmap =
            new Dictionary<string, BitmapImage>();

        #endregion
        #region Init of component
        //=======================

        public void ResetCountryRadioButton(RadioButton radio, CountryFlag.CountryCode code)
        {
            if (radio != null && radio.Content != null && radio.Content is WrapPanel wrap)
            {
                wrap.Children.Clear();
                var cf = new CountryFlag.CountryFlag();
                cf.Code = code;
                cf.Width = 30;
                wrap.Children.Add(cf);
            }
        }

        public ShelfControl()
        {
            InitializeComponent();

            // combo box needs init
            ComboClassId.Items.Clear();
            foreach (var dc in (DefinitionsVDI2770.Vdi2770DocClass[])Enum.GetValues(
                                                                         typeof(DefinitionsVDI2770.Vdi2770DocClass)))
                ComboClassId.Items.Add(
                    "" + DefinitionsVDI2770.GetDocClass(dc) + " - " + DefinitionsVDI2770.GetDocClassName(dc));

            ComboClassId.SelectedIndex = 0;

            // bind to view model
            this.DataContext = this.theViewModel;
            this.theViewModel.ViewModelChanged += TheViewModel_ViewModelChanged;

            var entities = new List<DocumentEntity>();
            entities.Add(new DocumentEntity("Titel", "Orga", "cdskcnsdkjcnkjsckjsdjn", new[] { "de", "GB" }));
            ScrollMainContent.ItemsSource = entities;

            // a bit hacky: explicetly load CountryFlag.dll
#if __not_working__in_Release__
            var x = CountryFlag.CountryCode.DE;
            if (x != CountryFlag.CountryCode.DE)
            {
                return null;
            }
#else
            // CountryFlag does not work in XAML (at least not in Release binary)
            ResetCountryRadioButton(RadioLangEN, CountryFlag.CountryCode.GB);
            ResetCountryRadioButton(RadioLangDE, CountryFlag.CountryCode.DE);
            ResetCountryRadioButton(RadioLangCN, CountryFlag.CountryCode.CN);
            ResetCountryRadioButton(RadioLangJP, CountryFlag.CountryCode.JP);
            ResetCountryRadioButton(RadioLangKR, CountryFlag.CountryCode.KR);
            ResetCountryRadioButton(RadioLangFR, CountryFlag.CountryCode.FR);
            ResetCountryRadioButton(RadioLangES, CountryFlag.CountryCode.ES);
#endif

            // Timer for loading
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            dispatcherTimer.Start();
        }

        private object mutexDocEntitiesInPreview = new object();
        private int numDocEntitiesInPreview = 0;
        private const int maxDocEntitiesInPreview = 3;

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            // each tick check for one image, if a preview shall be done
            if (theDocEntitiesToPreview != null && theDocEntitiesToPreview.Count > 0 &&
                numDocEntitiesInPreview < maxDocEntitiesInPreview)
            {
                // pop
                DocumentEntity ent = null;
                lock (theDocEntitiesToPreview)
                {
                    ent = theDocEntitiesToPreview[0];
                    theDocEntitiesToPreview.RemoveAt(0);
                }

                // ReSharper disable EmptyGeneralCatchClause
                try
                {
                    // temp input
                    var inputFn = ent?.DigitalFile;
                    if (inputFn != null)
                    {

                        // from package?
                        if (CheckIfPackageFile(inputFn))
                            inputFn = thePackage.MakePackageFileAvailableAsTempFile(ent.DigitalFile);

                        // temp output
                        string outputFn = System.IO.Path.GetTempFileName().Replace(".tmp", ".png");

                        // remember these for later deletion
                        ent.DeleteFilesAfterLoading = new[] { inputFn, outputFn };

                        // start process
                        string arguments = string.Format("-flatten -density 75 \"{0}\"[0] \"{1}\"", inputFn, outputFn);
                        string exeFn = System.IO.Path.Combine(
                            System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "convert.exe");
                        var startInfo = new ProcessStartInfo(exeFn, arguments);
                        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        var process = new Process();
                        process.StartInfo = startInfo;
                        process.EnableRaisingEvents = true;
                        DocumentEntity lambdaEntity = ent;
                        string outputFnBuffer = outputFn;
                        process.Exited += (sender2, args) =>
                        {
                            // release number of parallel processes
                            lock (mutexDocEntitiesInPreview)
                            {
                                numDocEntitiesInPreview--;
                            }

                            // take over data?
                            if (lambdaEntity.ImgContainer != null)
                            {
                                // trigger display image
                                lambdaEntity.ImageReadyToBeLoaded = outputFnBuffer;
                            }
                        };
                        process.Start();

                        // limit the number of parallel executions
                        lock (mutexDocEntitiesInPreview)
                        {
                            numDocEntitiesInPreview++;
                        }
                    }
                }
                catch { }
                // ReSharper enable EmptyGeneralCatchClause
            }

            // over all items in order to check, if a prepared image shall be displayed
            foreach (var x in this.ScrollMainContent.Items)
            {
                var de = x as DocumentEntity;
                if (de == null)
                    continue;

                if (de.ImageReadyToBeLoaded != null)
                {
                    // never again
                    var tempFn = de.ImageReadyToBeLoaded;
                    de.ImageReadyToBeLoaded = null;

                    // try load
                    // ReSharper disable EmptyGeneralCatchClause
                    try
                    {
                        // convert here, as the tick-Thread in STA / UI thread
                        var bi = new BitmapImage(new Uri(tempFn, UriKind.RelativeOrAbsolute));
                        var img = new Image();
                        img.Source = bi;
                        de.ImgContainer.Child = img;

                        // now delete the associated files file!
                        if (de.DeleteFilesAfterLoading != null)
                            foreach (var fn in de.DeleteFilesAfterLoading)
                                // it is quite likely (e.g. http:// files) that the delete fails!
                                try
                                {
                                    File.Delete(fn);
                                }
                                catch { }

                        // remember in the cache
                        if (referableHashToCachedBitmap != null &&
                            !referableHashToCachedBitmap.ContainsKey(de.ReferableHash))
                            referableHashToCachedBitmap[de.ReferableHash] = bi;
                    }
                    catch { }
                    // ReSharper enable EmptyGeneralCatchClause
                }
            }
        }

        public void Start(
            LogInstance log,
            AdminShellPackageEnv thePackage,
            AdminShell.Submodel theSubmodel,
            AasxPluginDocumentShelf.DocumentShelfOptions theOptions,
            PluginEventStack eventStack)
        {
            this.Log = log;
            this.thePackage = thePackage;
            this.theSubmodel = theSubmodel;
            this.theOptions = theOptions;
            this.theEventStack = eventStack;
        }

        public static ShelfControl FillWithWpfControls(
            LogInstance log,
            object opackage, object osm,
            AasxPluginDocumentShelf.DocumentShelfOptions options,
            PluginEventStack eventStack,
            object masterDockPanel)
        {
            // access
            var package = opackage as AdminShellPackageEnv;
            var sm = osm as AdminShell.Submodel;
            var master = masterDockPanel as DockPanel;
            if (package == null || sm == null || master == null)
            {
                return null;
            }

            // the Submodel elements need to have parents
            sm.SetAllParents();

            // create TOP control
            var shelfCntl = new ShelfControl();
            shelfCntl.Start(log, package, sm, options, eventStack);
            master.Children.Add(shelfCntl);

            // return shelf
            return shelfCntl;
        }

        public void HandleEventReturn(AasxPluginEventReturnBase evtReturn)
        {
            if (this.currentFormInst?.subscribeForNextEventReturn != null)
            {
                // delete first
                var tempLambda = this.currentFormInst.subscribeForNextEventReturn;
                this.currentFormInst.subscribeForNextEventReturn = null;

                // execute
                tempLambda(evtReturn);
            }
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            // user control was loaded, all options shall be set and outer grid is loaded fully ..
            ParseSubmodelToListItems(
                this.theSubmodel, this.theOptions, theViewModel.TheSelectedDocClass,
                theViewModel.TheSelectedLanguage, theViewModel.TheSelectedListType);
        }


        #endregion

        #region REDRAW of contents

        private void TheViewModel_ViewModelChanged(AasxIntegrationBase.WpfViewModelBase obj)
        {
            // re-display
            ParseSubmodelToListItems(
                this.theSubmodel, this.theOptions, theViewModel.TheSelectedDocClass,
                theViewModel.TheSelectedLanguage, theViewModel.TheSelectedListType);
        }

        private bool CheckIfPackageFile(string fn)
        {
            return fn.StartsWith(@"/");
        }

        private bool CheckIfConvertableFile(string fn)
        {
            var ext = System.IO.Path.GetExtension(fn.ToLower());
            if (ext == "")
                ext = "*";

            // check
            return (convertableFiles.Contains(ext));
        }

        private void ParseSubmodelToListItems(
            AdminShell.Submodel subModel, AasxPluginDocumentShelf.DocumentShelfOptions options,
            int selectedDocClass, AasxLanguageHelper.LangEnum selectedLanguage, ViewModel.ListType selectedListType)
        {
            try
            {
                // influence list view rendering, as well
                if (selectedListType == ViewModel.ListType.Bars)
                {
                    ScrollMainContent.ItemTemplate = (DataTemplate)ScrollMainContent.Resources["ItemTemplateForBar"];
                    ScrollMainContent.ItemsPanel = (ItemsPanelTemplate)ScrollMainContent.Resources["ItemsPanelForBar"];
                }

                if (selectedListType == ViewModel.ListType.Grid)
                {
                    ScrollMainContent.ItemTemplate = (DataTemplate)ScrollMainContent.Resources["ItemTemplateForGrid"];
                    ScrollMainContent.ItemsPanel =
                        (ItemsPanelTemplate)ScrollMainContent.Resources["ItemsPanelForGrid"];
                }

                // clean table
                ScrollMainContent.ItemsSource = null;

                // access
                if (subModel == null || options == null)
                    return;

                // make sure for the right Submodel
                var found = false;
                if (options.AllowSubmodelSemanticIds != null)
                    foreach (var x in options.AllowSubmodelSemanticIds)
                        if (subModel.semanticId != null && subModel.semanticId.Matches(x))
                        {
                            found = true;
                            break;
                        }
                if (!found)
                    return;

                // right now: hardcoded check for mdoel version
                var modelVersion = DocumentEntity.SubmodelVersion.Default;
                var defs11 = AasxPredefinedConcepts.VDI2770v11.Static;
                if (subModel.semanticId.Matches(defs11?.SM_ManufacturerDocumentation?.GetSemanticKey()))
                    modelVersion = DocumentEntity.SubmodelVersion.V11;

                // what defaultLanguage
                string defaultLang = null;
                if (theViewModel != null && theViewModel.TheSelectedLanguage > AasxLanguageHelper.LangEnum.Any)
                    defaultLang = AasxLanguageHelper.LangEnumToISO639String[(int)theViewModel.TheSelectedLanguage];

                // make new list box items
                var its = new List<DocumentEntity>();
                if (modelVersion != DocumentEntity.SubmodelVersion.V11)
                    its = ListOfDocumentEntity.ParseSubmodelForV10(
                        thePackage, subModel, options, defaultLang, selectedDocClass, selectedLanguage);
                else
                    its = ListOfDocumentEntity.ParseSubmodelForV11(
                        thePackage, subModel, defs11, defaultLang, selectedDocClass, selectedLanguage);

                // post process
                foreach (var ent in its)
                {
                    // make viewbox to host __later__ created image!
                    var vb = new Viewbox();
                    vb.Stretch = Stretch.Uniform;
                    ent.ImgContainer = vb;

                    // can already put a generated image into the viewbox?
                    if (referableHashToCachedBitmap != null &&
                        referableHashToCachedBitmap.ContainsKey(ent.ReferableHash))
                    {
                        var img = new Image();
                        img.Source = referableHashToCachedBitmap[ent.ReferableHash];
                        ent.ImgContainer.Child = img;
                    }
                    else
                    {
                        // trigger generation of image

                        // check if already in list
                        DocumentEntity foundDe = null;
                        foreach (var de in theDocEntitiesToPreview)
                            if (ent.ReferableHash == de.ReferableHash)
                                foundDe = de;

                        lock (theDocEntitiesToPreview)
                        {
                            if (foundDe != null)
                                theDocEntitiesToPreview.Remove(foundDe);
                            theDocEntitiesToPreview.Add(ent);
                        }
                    }

                    // attach events and add
                    ent.DoubleClick += DocumentEntity_DoubleClick;
                    ent.MenuClick += DocumentEntity_MenuClick;
                }
                
                // finally set
                ScrollMainContent.ItemsSource = its;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception when parse/ display Submodel");
            }
        }

        private void DocumentEntity_MenuClick(DocumentEntity e, string menuItemHeader, object tag)
        {
            // first check
            if (e == null || menuItemHeader == null)
                return;

            // what to do?
            if (tag == null && menuItemHeader == "Edit" && e.SourceElementsDocument != null &&
                e.SourceElementsDocumentVersion != null)
            {
                // show the edit panel
                OuterTabControl.SelectedItem = TabPanelEdit;
                ButtonAddUpdateDoc.Content = "Update";

                // make a template description for the content (remeber it)
                formInUpdateMode = true;
                updateSourceElements = e.SourceElementsDocument;

                var desc = theOptions.FormVdi2770;
                if (desc == null)
                    desc = DocumentShelfOptions.CreateVdi2770TemplateDesc(theOptions);

                // latest version (from resources)
                if (e.SmVersion == DocumentEntity.SubmodelVersion.V11)
                {
                    desc = DocumentShelfOptions.CreateVdi2770v11TemplateDesc();
                }

                this.currentFormDescription = desc;

                // take over existing data
                this.currentFormInst = new FormInstanceSubmodelElementCollection(null, currentFormDescription);
                this.currentFormInst.PresetInstancesBasedOnSource(updateSourceElements);
                this.currentFormInst.outerEventStack = theEventStack;

                // bring it to the panel
                var elementsCntl = new FormListOfDifferentControl();
                elementsCntl.ShowHeader = false;
                elementsCntl.DataContext = this.currentFormInst;
                ScrollViewerForm.Content = elementsCntl;

#if not_yet
                this.currentTemplateDescription = new AasTemplateDescListOfElement(desc);
                this.currentTemplateDescription.ClearDynamicData();
                this.currentTemplateDescription.PresetInstancesBasedOnSource(updateSourceElements);

                // bring it to the panel
                var elementsCntl = new AasTemplateListOfElementControl();
                elementsCntl.DataContext = this.currentTemplateDescription;
                ScrollViewerForm.Content = elementsCntl;
#endif
                // OK
                return;
            }

            if (tag == null && menuItemHeader == "Delete" && e.SourceElementsDocument != null &&
                e.SourceElementsDocumentVersion != null && theSubmodel?.submodelElements != null && theOptions != null)
            {
                // the source elements need to match a Document
                foreach (var smcDoc in
                    theSubmodel.submodelElements.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                        theOptions?.SemIdDocument))
                    if (smcDoc?.value == e.SourceElementsDocument)
                    {
                        // identify as well the DocumentVersion
                        // (convert to List() because of Count() below)
                        var allVers =
                            e.SourceElementsDocument.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                                theOptions?.SemIdDocumentVersion).ToList();
                        foreach (var smcVer in allVers)
                            if (smcVer?.value == e.SourceElementsDocumentVersion)
                            {
                                // access
                                if (smcVer == null || smcVer.value == null || smcDoc == null || smcDoc.value == null)
                                    continue;

                                // ask back .. the old-fashioned way!
                                if (MessageBoxResult.Yes != MessageBox.Show(
                                    "Delete Document?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Warning))
                                    return;

                                // confirmed! -> delete
                                if (allVers.Count < 2)
                                    // remove the whole document!
                                    theSubmodel.submodelElements.Remove(smcDoc);
                                else
                                    // remove only the document version
                                    e.SourceElementsDocument.Remove(smcVer);

                                // switch back to docs
                                // change back
                                OuterTabControl.SelectedItem = TabPanelList;

                                // re-display
                                ParseSubmodelToListItems(
                                    this.theSubmodel, this.theOptions, theViewModel.TheSelectedDocClass,
                                    theViewModel.TheSelectedLanguage, theViewModel.TheSelectedListType);

                                // re-display also in Explorer
                                var evt = new AasxPluginResultEventRedrawAllElements();
                                if (theEventStack != null)
                                    theEventStack.PushEvent(evt);

                                // OK
                                return;
                            }

                        // ReSharper enable PossibleMultipleEnumeration
                    }
            }

            // check for a document reference
            if (tag != null && tag is Tuple<DocumentEntity.DocRelationType, AdminShell.Reference> reltup
                && reltup.Item2 != null && reltup.Item2.Count > 0)
            {
                var evt = new AasxPluginResultEventNavigateToReference();
                evt.targetReference = new AdminShell.Reference(reltup.Item2);
                this.theEventStack.PushEvent(evt);
            }
        }

        private void DocumentEntity_DoubleClick(DocumentEntity e)
        {
            // first check
            if (e == null || e.DigitalFile == null || e.DigitalFile.Trim().Length < 1 || this.theEventStack == null)
                return;

            try
            {
                // temp input
                var inputFn = e.DigitalFile;
                try
                {
                    if (!inputFn.ToLower().Trim().StartsWith("http://")
                            && !inputFn.ToLower().Trim().StartsWith("https://"))
                        inputFn = thePackage.MakePackageFileAvailableAsTempFile(inputFn);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Making local file available");
                }

                // give over to event stack
                var evt = new AasxPluginResultEventDisplayContentFile();
                evt.fn = inputFn;
                this.theEventStack.PushEvent(evt);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "when double-click");
            }
        }

        #endregion

        protected FormDescSubmodelElementCollection currentFormDescription = null;
        protected FormInstanceSubmodelElementCollection currentFormInst = null;

        protected bool formInUpdateMode = false;
        protected AdminShell.SubmodelElementWrapperCollection updateSourceElements = null;


        private void ButtonTabPanels_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ButtonCreateDoc)
            {
                // show the edit panel
                OuterTabControl.SelectedItem = TabPanelEdit;
                ButtonAddUpdateDoc.Content = "Add";

                /* TODO (MIHO, 2020-09-29): if the V1.1 template works and is adopted, the old
                // V1.0 shall be removed completely (over complicated) */
                // make a template description for the content (remeber it)
                var desc = theOptions.FormVdi2770;
                if (desc == null)
                    desc = DocumentShelfOptions.CreateVdi2770TemplateDesc(theOptions);

                // latest version (from resources)
                if (this.CheckBoxLatestVersion.IsChecked == true)
                {
                    desc = DocumentShelfOptions.CreateVdi2770v11TemplateDesc();
                }

                this.currentFormDescription = desc;
                formInUpdateMode = false;
                updateSourceElements = null;

                // take over existing data
                this.currentFormInst = new FormInstanceSubmodelElementCollection(null, currentFormDescription);
                this.currentFormInst.PresetInstancesBasedOnSource(updateSourceElements);
                this.currentFormInst.outerEventStack = theEventStack;

                // bring it to the panel
                var elementsCntl = new FormListOfDifferentControl();
                elementsCntl.ShowHeader = false;
                elementsCntl.DataContext = this.currentFormInst;
                ScrollViewerForm.Content = elementsCntl;
            }

            if (sender == ButtonAddUpdateDoc)
            {
                // add
                if (this.currentFormInst != null && this.currentFormDescription != null
                    && thePackage != null
                    && theOptions != null && theOptions.SemIdDocument != null
                    && theSubmodel != null)
                {
                    // on this level of the hierarchy, shall a new SMEC be created or shall
                    // the existing source of elements be used?
                    AdminShell.SubmodelElementWrapperCollection currentElements = null;
                    if (formInUpdateMode && updateSourceElements != null)
                    {
                        currentElements = updateSourceElements;
                    }
                    else
                    {
                        currentElements = new AdminShell.SubmodelElementWrapperCollection();
                    }

                    // create a sequence of SMEs
                    try
                    {
                        this.currentFormInst.AddOrUpdateDifferentElementsToCollection(
                            currentElements, thePackage, addFilesToPackage: true);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "when adding Document");
                    }

                    // the InstSubmodel, which started the process, should have a "fresh" SMEC available
                    // make it unique in the Documentens Submodel
                    var newSmc = this.currentFormInst?.sme as AdminShell.SubmodelElementCollection;

                    // if not update, put them into the Document's Submodel
                    if (!formInUpdateMode && currentElements != null && newSmc != null)
                    {
                        // make newSmc unique in the cotext of the Submodel
                        FormInstanceHelper.MakeIdShortUnique(theSubmodel.submodelElements, newSmc);

                        // add the elements
                        newSmc.value = currentElements;

                        // add the whole SMC
                        theSubmodel.Add(newSmc);
                    }

#if __may_be_not__
                    // save directly to ensure consistency
                    try
                    {
                        if (thePackage.Filename != null)
                            thePackage.SaveAs(thePackage.Filename);
                    }
                    catch (Exception ex)
                    {
                        if (theLogger != null)
                            theLogger.Log(
                                $"Saving package {thePackage.Filename} failed for adding Document " +
                                $"and gave: {ex.Message}");
                    }
#endif
                }
                else
                {
                    Log.Error("Preconditions for adding Document not met.");
                }

                // change back
                OuterTabControl.SelectedItem = TabPanelList;

                // re-display
                ParseSubmodelToListItems(
                    this.theSubmodel, this.theOptions, theViewModel.TheSelectedDocClass,
                    theViewModel.TheSelectedLanguage, theViewModel.TheSelectedListType);

                // re-display also in Explorer
                var evt = new AasxPluginResultEventRedrawAllElements();
                if (theEventStack != null)
                    theEventStack.PushEvent(evt);
            }

            if (sender == ButtonCancel)
            {
                OuterTabControl.SelectedItem = TabPanelList;
            }

            if(sender == ButtonFixCDs)
            {
                // check if CDs are present
                var theDefs = new AasxPredefinedConcepts.DefinitionsVDI2770.SetOfDefsVDI2770(
                    new AasxPredefinedConcepts.DefinitionsVDI2770());
                var theCds = theDefs?.GetAllReferables().Where(
                    (rf) => { return rf is AdminShell.ConceptDescription; }).ToList();

                if (theCds == null || theCds.Count < 1)
                {
                    Log.Error(
                        "Not able to find appropriate ConceptDescriptions in pre-definitions. " +
                        "Aborting.");
                    return;
                }

                // check for Environment
                var env = this.thePackage?.AasEnv;
                if (env == null)
                {
                    Log.Error(
                        "Not able to access AAS environment for set of Submodel's ConceptDescriptions. Aborting.");
                    return;
                }

                // be safe?
                if (MessageBoxResult.Yes != MessageBox.Show(
                    "Add missing ConceptDescriptions to the AAS?", "Question",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning))
                    return;

                // ok, check
                int nr = 0;
                foreach (var x in theCds)
                {
                    var cd = x as AdminShell.ConceptDescription;
                    if (cd == null || cd.identification == null)
                        continue;
                    var cdFound = env.FindConceptDescription(cd.identification);
                    if (cdFound != null)
                        continue;
                    // ok, add
                    var newCd = new AdminShell.ConceptDescription(cd);
                    env.ConceptDescriptions.Add(newCd);
                    nr++;
                }

                // ok
                Log.Info("In total, {0} ConceptDescriptions were added to the AAS environment.", nr);
            }

        }
    }
}
