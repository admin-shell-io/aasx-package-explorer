using AdminShellNS;
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
using Newtonsoft.Json;
using AasxPredefinedConcepts;

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

        public class ViewModel : AasxUtilities.WpfViewModelBase
        {

            private int theSelectedDocClass = 0;
            public int TheSelectedDocClass { get { return theSelectedDocClass; } set { theSelectedDocClass = value; RaisePropertyChanged("TheSelectedDocClass"); RaiseViewModelChanged(); } }

            public enum LanguageSelection { All = 0, EN, DE, CN, JP, KR, FR, ES };

            public static string[] LanguageSelectionToISO3166String = { "All", "GB", "DE", "CN", "JP", "KR", "FR", "ES" }; // ISO 3166 -> List of contries
            public static string[] LanguageSelectionToISO639String = { "All", "en", "de", "cn", "jp", "kr", "fr", "es" }; // ISO 639 -> List of languages

            private LanguageSelection theSelectedLanguage = LanguageSelection.All;
            public LanguageSelection TheSelectedLanguage { get { return theSelectedLanguage; } set { theSelectedLanguage = value; RaisePropertyChanged("TheSelectedLanguage"); RaiseViewModelChanged(); } }

            public enum ListType { Bars, Grid };
            private ListType theSelectedListType = ListType.Bars;
            public ListType TheSelectedListType { get { return theSelectedListType; } set { theSelectedListType = value; RaisePropertyChanged("TheSelectedListType"); RaiseViewModelChanged(); } }

            //private string tagName = "Foo bar";
            //public string TagName { get { return tagName; } set { tagName = value; RaisePropertyChanged("TagName"); } }

        }

        #endregion
        #region Cache for already generated Images
        //========================================

        private static Dictionary<string, BitmapImage> referableHashToCachedBitmap = new Dictionary<string, BitmapImage>();

        #endregion
        #region Init of component
        //=======================

        public void ResetCountryRadioButton (RadioButton radio, CountryFlag.CountryCode code)
        {
            if (radio != null && radio.Content != null && radio.Content is WrapPanel)
            {
                var wrap = radio.Content as WrapPanel;
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
            //for (int i = 0; i < DocumentShelfOptions.Vdi2770ClassIdMapping.Length / 2; i++)
            //    ComboClassId.Items.Add("" + DocumentShelfOptions.Vdi2770ClassIdMapping[2 * i + 0] + " - " + DocumentShelfOptions.Vdi2770ClassIdMapping[2 * i + 1]);
            foreach (var dc in (DefinitionsVDI2770.Vdi2770DocClass[])Enum.GetValues(typeof(DefinitionsVDI2770.Vdi2770DocClass)))
                ComboClassId.Items.Add("" + DefinitionsVDI2770.GetDocClass(dc) + " - " + DefinitionsVDI2770.GetDocClassName(dc));

            ComboClassId.SelectedIndex = 0;

            // bind to view model
            this.DataContext = this.theViewModel;
            this.theViewModel.ViewModelChanged += TheViewModel_ViewModelChanged;

            var entities = new List<DocumentEntity>();
            entities.Add(new DocumentEntity("Titel", "Orga", "cdskcnsdkjcnkjsckjsdjn", new string[] { "de", "GB" }));
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
            //if (theDocEntitiesToPreview != null && theDocEntitiesToPreview.Count > 0)
            //    Log.Info("***" + numDocEntitiesInPreview);

            // each tick check for one image, if a preview shall be done
            if (theDocEntitiesToPreview != null && theDocEntitiesToPreview.Count > 0 && numDocEntitiesInPreview < maxDocEntitiesInPreview)
            {
                // pop
                DocumentEntity ent = null;
                lock (theDocEntitiesToPreview)
                {
                    ent = theDocEntitiesToPreview[0];
                    theDocEntitiesToPreview.RemoveAt(0);
                }

                try
                {
                    // temp input
                    var inputFn = ent.DigitalFile;

                    // from package?
                    if (CheckIfPackageFile(inputFn))
                        inputFn = thePackage.MakePackageFileAvailableAsTempFile(ent.DigitalFile);

                    // temp output
                    string outputFn = System.IO.Path.GetTempFileName().Replace(".tmp", ".png");

                    // remember these for later deletion
                    ent.DeleteFilesAfterLoading = new string[] { inputFn, outputFn };

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
                        if (lambdaEntity != null && lambdaEntity.ImgContainer != null)
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
                catch { }
            }

            // over all items in order to check, if a prepared image shall be display
            if (this.ScrollMainContent != null && this.ScrollMainContent.Items != null)
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
                            if (referableHashToCachedBitmap != null && !referableHashToCachedBitmap.ContainsKey(de.ReferableHash))
                                referableHashToCachedBitmap[de.ReferableHash] = bi;
                        }
                        catch { }
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

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            // user control was loaded, all options shall be set and outer grid is loaded fully ..
            ParseSubmodelToListItems(this.theSubmodel, this.theOptions, theViewModel.TheSelectedDocClass, theViewModel.TheSelectedLanguage, theViewModel.TheSelectedListType);
        }


#endregion

#region REDRAW of contents

        private void TheViewModel_ViewModelChanged(AasxUtilities.WpfViewModelBase obj)
        {
            // re-display
            ParseSubmodelToListItems(this.theSubmodel, this.theOptions, theViewModel.TheSelectedDocClass, theViewModel.TheSelectedLanguage, theViewModel.TheSelectedListType);
        }

        private bool CheckIfPackageFile (string fn)
        {
            return fn.StartsWith(@"/");
        }

        private bool CheckIfConvertableFile (string fn)
        {
            var ext = System.IO.Path.GetExtension(fn.ToLower());
            if (ext == "")
                ext = "*";

            // check
            return (convertableFiles.Contains(ext));
        }

        private void ParseSubmodelToListItems(AdminShell.Submodel subModel, AasxPluginDocumentShelf.DocumentShelfOptions options,
            int selectedDocClass, ViewModel.LanguageSelection selectedLanguage, ViewModel.ListType selectedListType)
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
                    ScrollMainContent.ItemsPanel = (ItemsPanelTemplate)ScrollMainContent.Resources["ItemsPanelForGrid"];
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

                // what defaultLanguage
                string defaultLang = null;
                if (theViewModel != null && theViewModel.TheSelectedLanguage > ViewModel.LanguageSelection.All)
                    defaultLang = ViewModel.LanguageSelectionToISO639String[(int) theViewModel.TheSelectedLanguage];

                // set a new list
                var its = new List<DocumentEntity>();

                // look for Documents
                if (subModel?.submodelElements != null)
                    foreach (var smcDoc in subModel.submodelElements.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(options?.SemIdDocument))
                    {
                        // access
                        if (smcDoc == null || smcDoc.value == null)
                            continue;

                        // look immediately for DocumentVersion, as only with this there is a valid List item
                        foreach (var smcVer in smcDoc.value.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(options?.SemIdDocumentVersion))
                        {
                            // access
                            if (smcVer == null || smcVer.value == null)
                                continue;

                            //
                            // try to lookup info in smcDoc and smcVer
                            //

                            // take the 1st title
                            var title = "" + smcVer.value.FindFirstSemanticIdAs<AdminShell.Property>(options?.SemIdTitle)?.value;

                            // could be also a multi-language title
                            foreach (var mlp in smcVer.value.FindAllSemanticIdAs<AdminShell.MultiLanguageProperty>(options?.SemIdTitle))
                                if (mlp.value != null)
                                    title = mlp.value.GetDefaultStr(defaultLang);

                            // have multiple opportunities for orga
                            var orga = "" + smcVer.value.FindFirstSemanticIdAs<AdminShell.Property>(options?.SemIdOrganizationOfficialName)?.value;
                            if (orga.Trim().Length < 1)
                                orga = "" + smcVer.value.FindFirstSemanticIdAs<AdminShell.Property>(options?.SemIdOrganizationName)?.value;

                            // class infos
                            var classId = "" + smcDoc.value.FindFirstSemanticIdAs<AdminShell.Property>(options?.SemIdDocumentClassId)?.value;
                            var className = "" + smcDoc.value.FindFirstSemanticIdAs<AdminShell.Property>(options?.SemIdDocumentClassName)?.value;

                            // collect country codes
                            var countryCodesStr = new List<string>();
                            var countryCodesEnum = new List<ViewModel.LanguageSelection>();
                            foreach (var cclp in smcVer.value.FindAllSemanticIdAs<AdminShell.Property>(options?.SemIdLanguage))
                            {
                                // language code
                                var candidate = ("" + cclp.value).Trim().ToUpper();
                                if (candidate.Length < 1)
                                    continue;

                                // convert to country codes
                                foreach (var ev in (ViewModel.LanguageSelection[])Enum.GetValues(typeof(ViewModel.LanguageSelection)))
                                    if (candidate == ViewModel.LanguageSelectionToISO639String[(int)ev]?.ToUpper())
                                    {
                                        candidate = ViewModel.LanguageSelectionToISO3166String[(int)ev]?.ToUpper();
                                        countryCodesEnum.Add(ev);
                                    }

                                // add
                                countryCodesStr.Add(candidate);
                            }

                            // evaluate, if in selection
                            var okDocClass = (selectedDocClass < 1 || classId == null || classId.Trim().Length < 1
                                || classId.Trim().StartsWith(DefinitionsVDI2770.GetDocClass((DefinitionsVDI2770.Vdi2770DocClass)selectedDocClass)));

                            var okLanguage = (selectedLanguage == ViewModel.LanguageSelection.All || countryCodesEnum == null
                                || countryCodesStr.Count < 1 /* make only exception, if no language not all (not only the preferred of LanguageSelectionToISO639String) are in the property */
                                || countryCodesEnum.Contains(selectedLanguage));

                            if (!okDocClass || !okLanguage)
                                continue;

                            // further info
                            var further = "";
                            foreach (var fi in smcVer.value.FindAllSemanticIdAs<AdminShell.Property>(options?.SemIdDocumentVersionIdValue))
                                further += "\u00b7 version: " + fi.value;
                            foreach (var fi in smcVer.value.FindAllSemanticIdAs<AdminShell.Property>(options?.SemIdDate))
                                further += "\u00b7 date: " + fi.value;
                            if (further.Length > 0)
                                further = further.Substring(2);

                            // construct entity
                            var ent = new DocumentEntity(title, orga, further, countryCodesStr.ToArray());
                            ent.ReferableHash = String.Format("{0:X14} {1:X14}", thePackage.GetHashCode(), smcDoc.GetHashCode());

                            // for updating data, set the source elements of this document entity
                            ent.SourceElementsDocument = smcDoc.value;
                            ent.SourceElementsDocumentVersion = smcVer.value;

                            // filename
                            var fn = smcVer.value.FindFirstSemanticIdAs<AdminShell.File>(options?.SemIdDigitalFile)?.value;
                            ent.DigitalFile = fn;

                            // make viewbox to host __later__ created image!
                            var vb = new Viewbox();
                            vb.Stretch = Stretch.Uniform;
                            ent.ImgContainer = vb;

                            // can already put a generated image into the viewbox?
                            if (referableHashToCachedBitmap != null && referableHashToCachedBitmap.ContainsKey(ent.ReferableHash))
                            {
                                var img = new Image();
                                img.Source = referableHashToCachedBitmap[ent.ReferableHash] as BitmapImage;
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
                            its.Add(ent);

                        }

                    }

                // make new list box items
                ScrollMainContent.ItemsSource = its;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception when parse/ display Submodel");
            }
        }

        private void DocumentEntity_MenuClick(DocumentEntity e, string menuItemHeader)
        {
            // first check
            if (e == null || menuItemHeader == null)
                return;

            // what to do?
            if (menuItemHeader == "Edit" && e.SourceElementsDocument != null && e.SourceElementsDocumentVersion != null)
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
                this.currentFormDescription = desc;

                // take over existing data
                this.currentFormInst = new FormInstanceSubmodelElementCollection(null, currentFormDescription);
                this.currentFormInst.PresetInstancesBasedOnSource(updateSourceElements);

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

            if (menuItemHeader == "Delete" && e.SourceElementsDocument != null && e.SourceElementsDocumentVersion != null && theSubmodel?.submodelElements != null && theOptions != null)
            {
                // the source elements need to match a Document
                foreach (var smcDoc in theSubmodel.submodelElements.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(theOptions?.SemIdDocument))
                    if (smcDoc?.value == e.SourceElementsDocument)
                    {
                        // identify as well the DocumentVersion
                        var allVers = e.SourceElementsDocument.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(theOptions?.SemIdDocumentVersion);
                        if (allVers != null)
                            foreach (var smcVer in allVers)
                                if (smcVer?.value == e.SourceElementsDocumentVersion)
                                {
                                    // access
                                    if (smcVer == null || smcVer.value == null || smcDoc == null || smcDoc.value == null)
                                        continue;

                                    // ask back .. the old-fashioned way!
                                    if (MessageBoxResult.Yes != MessageBox.Show("Delete Document?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Warning))
                                        return;

                                    // confirmed! -> delete
                                    if (allVers == null || allVers.Count() < 2)
                                        // remove the whole document!
                                        theSubmodel.submodelElements.Remove(smcDoc);
                                    else
                                        // remove only the document version
                                        e.SourceElementsDocument.Remove(smcVer);

                                    // switch back to docs
                                    // change back
                                    OuterTabControl.SelectedItem = TabPanelList;

                                    // re-display
                                    ParseSubmodelToListItems(this.theSubmodel, this.theOptions, theViewModel.TheSelectedDocClass, theViewModel.TheSelectedLanguage, theViewModel.TheSelectedListType);

                                    // re-display also in Explorer
                                    var evt = new AasxPluginResultEventRedrawAllElements();
                                    if (theEventStack != null)
                                        theEventStack.PushEvent(evt);

                                    // OK
                                    return;
                                }
                    }
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
                } catch (Exception ex)
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

                // make a template description for the content (remeber it)
                var desc = theOptions.FormVdi2770;
                if (desc == null)
                    desc = DocumentShelfOptions.CreateVdi2770TemplateDesc(theOptions);

                this.currentFormDescription = desc;
                formInUpdateMode = false;
                updateSourceElements = null;

                // take over existing data
                this.currentFormInst = new FormInstanceSubmodelElementCollection(null, currentFormDescription);
                this.currentFormInst.PresetInstancesBasedOnSource(updateSourceElements);

                // bring it to the panel
                var elementsCntl = new FormListOfDifferentControl();
                elementsCntl.ShowHeader = false;
                elementsCntl.DataContext = this.currentFormInst;
                ScrollViewerForm.Content = elementsCntl;
            }

            if (sender == ButtonAddUpdateDoc)
            {
                // add
                if (this.currentFormInst != null  && this.currentFormDescription != null
                    && thePackage != null
                    && theOptions != null && theOptions.SemIdDocument != null
                    && theSubmodel != null)
                {
                    // on this level of the hierarchy, shall a new SMEC be created or shall the existing source of elements
                    // be used?
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
                    AdminShell.SubmodelElementWrapperCollection smwc = null;
                    try
                    {
                        smwc = this.currentFormInst.AddOrUpdateDifferentElementsToCollection(currentElements, thePackage, addFilesToPackage: true);

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
                            theLogger.Log($"Saving package {thePackage.Filename} failed for adding Document and gave: {ex.Message}");
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
                ParseSubmodelToListItems(this.theSubmodel, this.theOptions, theViewModel.TheSelectedDocClass, theViewModel.TheSelectedLanguage, theViewModel.TheSelectedListType);

                // re-display also in Explorer
                var evt = new AasxPluginResultEventRedrawAllElements();
                if (theEventStack != null)
                    theEventStack.PushEvent(evt);
            }

            if (sender == ButtonCancel)
            {
                OuterTabControl.SelectedItem = TabPanelList;
            }

        }
    }
}
