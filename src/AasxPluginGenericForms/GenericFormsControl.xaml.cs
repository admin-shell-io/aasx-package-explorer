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
using AdminShellNS;
using Newtonsoft.Json;

namespace AasxPluginGenericForms
{
    /// <summary>
    /// Interaktionslogik für ShelfControl.xaml
    /// </summary>
    public partial class GenericFormsControl : UserControl
    {
        #region Members
        //=============

        private LogInstance Log = new LogInstance();
        private AdminShellPackageEnv thePackage = null;
        private AdminShell.Submodel theSubmodel = null;
        private AasxPluginGenericForms.GenericFormOptions theOptions = null;
        private PluginEventStack theEventStack = null;

        #endregion

        #region View Model
        //================

        private ViewModel theViewModel = new ViewModel();

        public class ViewModel : AasxIntegrationBase.WpfViewModelBase
        {
        }

        #endregion
        #region Init of component
        //=======================

        public GenericFormsControl()
        {
            InitializeComponent();

            // bind to view model
            this.DataContext = this.theViewModel;
            this.theViewModel.ViewModelChanged += TheViewModel_ViewModelChanged;

            // Timer for loading
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            dispatcherTimer.Start();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
        }

        public void Start(
            LogInstance log,
            AdminShellPackageEnv thePackage,
            AdminShell.Submodel theSubmodel,
            AasxPluginGenericForms.GenericFormOptions theOptions,
            PluginEventStack eventStack)
        {
            this.Log = log;
            this.thePackage = thePackage;
            this.theSubmodel = theSubmodel;
            this.theOptions = theOptions;
            this.theEventStack = eventStack;
        }

        public static GenericFormsControl FillWithWpfControls(
            LogInstance log,
            object opackage, object osm,
            AasxPluginGenericForms.GenericFormOptions options,
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
            var shelfCntl = new GenericFormsControl();
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
            DisplaySubmodel();
        }


        #endregion
        #region REDRAW of contents
        //========================

        private void TheViewModel_ViewModelChanged(AasxIntegrationBase.WpfViewModelBase obj)
        {
        }

        #endregion

        protected bool formInUpdateMode = false;
        protected AdminShell.SubmodelElementWrapperCollection updateSourceElements = null;

        protected GenericFormsOptionsRecord currentFormRecord = null;
        protected FormInstanceSubmodel currentFormInst = null;

        private void DisplaySubmodel()
        {
            // show the edit panel
            OuterTabControl.SelectedItem = TabPanelEdit;

            // clear first
            ScrollViewerForm.Content = null;

            // test trivial access
            if (theOptions == null || theSubmodel == null)
                return;

            // identify the record
            // check for a record in options, that matches Submodel
            currentFormRecord = theOptions.MatchRecordsForSemanticId(theSubmodel.semanticId);
            if (currentFormRecord == null)
                return;

            // check form
            if (currentFormRecord.FormSubmodel == null || currentFormRecord.FormSubmodel.SubmodelElements == null)
                return;

            // initialize form
            formInUpdateMode = true;
            updateSourceElements = theSubmodel.submodelElements;

            // take over existing data
            this.currentFormInst = new FormInstanceSubmodel(currentFormRecord.FormSubmodel);
            this.currentFormInst.InitReferable(currentFormRecord.FormSubmodel, theSubmodel);
            this.currentFormInst.PresetInstancesBasedOnSource(updateSourceElements);
            this.currentFormInst.outerEventStack = theEventStack;

            // bring it to the panel
            var elementsCntl = new FormListOfDifferentControl();
            elementsCntl.DataContext = this.currentFormInst;
            ScrollViewerForm.Content = elementsCntl;
        }

        private void ButtonTabPanels_Click(object sender, RoutedEventArgs e)
        {
            if (sender == ButtonUpdate)
            {
                // add
                if (this.currentFormInst != null
                    && thePackage != null
                    && theOptions != null
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
                    }

                    // create a sequence of SMEs
                    try
                    {
                        this.currentFormInst.AddOrUpdateDifferentElementsToCollection(
                            currentElements, thePackage, addFilesToPackage: true, editSource: true);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "when adding Document");
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
                    Log.Error("Preconditions for adding entities from GenericForm not met.");
                }

                // re-display
                DisplaySubmodel();

                // re-display also in Explorer
                var evt = new AasxPluginResultEventRedrawAllElements();
                if (theEventStack != null)
                    theEventStack.PushEvent(evt);
            }

            if (sender == ButtonFixCDs)
            {
                // check if CDs are present
                if (currentFormRecord == null || currentFormRecord.ConceptDescriptions == null ||
                    currentFormRecord.ConceptDescriptions.Count < 1)
                {
                    Log.Error(
                        "Not able to find appropriate ConceptDescriptions in the GeneralForm option records. " +
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
                foreach (var cd in currentFormRecord.ConceptDescriptions)
                {
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
