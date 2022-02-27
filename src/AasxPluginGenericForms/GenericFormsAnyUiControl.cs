using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AasxIntegrationBase;
using AasxIntegrationBase.AasForms;
using AdminShellNS;
using AnyUi;
using Newtonsoft.Json;

namespace AasxPluginGenericForms
{
    public class GenericFormsAnyUiControl
    {
        #region Members
        //=============

        private LogInstance _log = new LogInstance();
        private AdminShellPackageEnv _package = null;
        private AdminShell.Submodel _submodel = null;
        private AasxPluginGenericForms.GenericFormOptions _options = null;
        private PluginEventStack _eventStack = null;
        private AnyUiStackPanel _panel = null;

        protected AnyUiSmallWidgetToolkit _uitk = new AnyUiSmallWidgetToolkit();

        protected bool _formInUpdateMode = false;
        protected AdminShell.SubmodelElementWrapperCollection _updateSourceElements = null;

        protected GenericFormsOptionsRecord _currentFormRecord = null;
        protected FormInstanceSubmodel _currentFormInst = null;

        #endregion

        #region Constructors, as for WPF control
        //=============

        public void Start(
            LogInstance log,
            AdminShellPackageEnv thePackage,
            AdminShell.Submodel theSubmodel,
            AasxPluginGenericForms.GenericFormOptions theOptions,
            PluginEventStack eventStack,
            AnyUiStackPanel panel)
        {
            _log = log;
            _package = thePackage;
            _submodel = theSubmodel;
            _options = theOptions;
            _eventStack = eventStack;
            _panel = panel;

            // fill given panel
            DisplaySubmodel(_panel, _uitk);
        }

        public static GenericFormsAnyUiControl FillWithAnyUiControls(
            LogInstance log,
            object opackage, object osm,
            AasxPluginGenericForms.GenericFormOptions options,
            PluginEventStack eventStack,
            object opanel)
        {
            // access
            var package = opackage as AdminShellPackageEnv;
            var sm = osm as AdminShell.Submodel;
            var panel = opanel as AnyUiStackPanel;
            if (package == null || sm == null || panel == null)
                return null;

            // the Submodel elements need to have parents
            sm.SetAllParents();

            // do NOT create WPF controls
            FormInstanceBase.createSubControls = false;

            // factory this object
            var shelfCntl = new GenericFormsAnyUiControl();
            shelfCntl.Start(log, package, sm, options, eventStack, panel);

            // return shelf
            return shelfCntl;
        }

        #endregion

        #region Display Submodel
        //=============

        private void DisplaySubmodel(AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk)
        {
            // test trivial access
            if (_options == null || _submodel == null)
                return;

            // identify the record
            // check for a record in options, that matches Submodel
            _currentFormRecord = _options.MatchRecordsForSemanticId(_submodel.semanticId);
            if (_currentFormRecord == null)
                return;

            // check form
            if (_currentFormRecord.FormSubmodel == null || _currentFormRecord.FormSubmodel.SubmodelElements == null)
                return;

            // initialize form
            _formInUpdateMode = true;
            _updateSourceElements = _submodel.submodelElements;

            // take over existing data
            _currentFormInst = new FormInstanceSubmodel(_currentFormRecord.FormSubmodel);
            _currentFormInst.InitReferable(_currentFormRecord.FormSubmodel, _submodel);
            _currentFormInst.PresetInstancesBasedOnSource(_updateSourceElements);
            _currentFormInst.outerEventStack = _eventStack;

            // bring it to the panel            
            RenderFormInst(view, uitk, _currentFormInst);
        }

        protected double _lastScrollPosition = 0.0;

        protected void RenderFormInst (
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk, 
            FormInstanceSubmodel sm,
            double ?initialScrollPos = null)
        {
            // make an outer grid, very simple grid of two rows: header & body
            var outer = view.Add(uitk.AddSmallGrid(rows: 3, cols: 1, colWidths: new[] { "*" }));
            outer.RowDefinitions[2].Height = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);

            // at top, make buttons for the general form
            var header = uitk.AddSmallGridTo(outer, 0, 0, 1, cols: 5, colWidths: new[] { "*", "#", "#", "#", "#" });

            header.Margin = new AnyUiThickness(0);
            header.Background = AnyUiBrushes.LightBlue;

            uitk.AddSmallBasicLabelTo(header, 0, 0, margin: new AnyUiThickness(8, 6, 0, 6),
                foreground: AnyUiBrushes.DarkBlue,
                fontSize: 1.5f,
                setBold: true,
                content: $"Edit");

            AnyUiUIElement.RegisterControl(
                uitk.AddSmallButtonTo(header, 0, 1,
                    margin: new AnyUiThickness(2), setHeight: 21,
                    padding: new AnyUiThickness(2, 0, 2, 0),
                    content: "Fix missing CDs .."),
                (o) =>
                {
                    return ButtonTabPanels_Click("ButtonFixCDs");
                });

            uitk.AddSmallBasicLabelTo(header, 0, 2,
                foreground: AnyUiBrushes.DarkBlue,
                margin: new AnyUiThickness(4,0,4,0),
                verticalAlignment: AnyUiVerticalAlignment.Center,
                verticalContentAlignment: AnyUiVerticalAlignment.Center,
                content: "|");

            AnyUiUIElement.RegisterControl(
                uitk.AddSmallButtonTo(header, 0, 3,
                    margin: new AnyUiThickness(2), setHeight: 21,
                    padding: new AnyUiThickness(2, 0, 2, 0),
                    content: "Cancel"),
                (o) =>
                {
                    return ButtonTabPanels_Click("ButtonCancel");
                });

            AnyUiUIElement.RegisterControl(
                uitk.AddSmallButtonTo(header, 0, 4,
                    margin: new AnyUiThickness(2,2,4,2), setHeight: 21,
                    padding: new AnyUiThickness(2, 0, 2, 0),
                    content: "Update to AAS"),
                (o) =>
                {
                    return ButtonTabPanels_Click("ButtonUpdate");
                });

            // small spacer
            var space = uitk.AddSmallBasicLabelTo(outer, 1, 0, 
                fontSize: 0.3f,
                content: "", background: AnyUiBrushes.White);

            // add the body, a scroll viewer
            var scroll = AnyUiUIElement.RegisterControl(
                uitk.AddSmallScrollViewerTo(outer, 2, 0,
                    horizontalScrollBarVisibility: AnyUiScrollBarVisibility.Disabled,
                    verticalScrollBarVisibility: AnyUiScrollBarVisibility.Visible,
                    skipForTarget: AnyUiTargetPlatform.Browser, initialScrollPosition: initialScrollPos),
                (o) =>
                {
                    if (o is Tuple<double, double> positions)
                    {
                        _lastScrollPosition = positions.Item2;
                    }
                    return new AnyUiLambdaActionNone();
                }) as AnyUiScrollViewer;

            // need a stack panel to add inside
            var inner = new AnyUiStackPanel() { Orientation = AnyUiOrientation.Vertical };
            scroll.Content = inner;

            // render the innerts of the scroll viewer
            inner.Background = AnyUiBrushes.LightGray;
            _currentFormInst.RenderAnyUi(inner, uitk);
        }

        #endregion

        #region Event handling
        //=============

        public void HandleEventReturn(AasxPluginEventReturnBase evtReturn)
        {
            if (_currentFormInst?.subscribeForNextEventReturn != null)
            {
                // delete first
                var tempLambda = _currentFormInst.subscribeForNextEventReturn;
                _currentFormInst.subscribeForNextEventReturn = null;

                // execute
                tempLambda(evtReturn);
            }
        }

        #endregion

        #region Update
        //=============

        public void Update(params object[] args)
        {
            // check args
            if (args == null || args.Length < 1
                || !(args[0] is AnyUiStackPanel newPanel)) 
                return;

            // ok, re-assign panel and re-display
            _panel = newPanel;
            _panel.Children.Clear();            
            RenderFormInst(_panel, _uitk, _currentFormInst, initialScrollPos: _lastScrollPosition);
        }

        #endregion

        #region Button clicks
        //=============

        private AnyUiLambdaActionBase ButtonTabPanels_Click(string cmd)
        {
            if (cmd == "ButtonCancel")
            {
                // re-display (tree & panel)
                return new AnyUiLambdaActionRedrawAllElementsBase() { NextFocus = _submodel };
            }

            if (cmd == "ButtonUpdate")
            {
                // add
                if (this._currentFormInst != null
                    && _package != null
                    && _options != null
                    && _submodel != null)
                {
                    // on this level of the hierarchy, shall a new SMEC be created or shall
                    // the existing source of elements be used?
                    AdminShell.SubmodelElementWrapperCollection currentElements = null;
                    if (_formInUpdateMode && _updateSourceElements != null)
                    {
                        currentElements = _updateSourceElements;
                    }
                    else
                    {
                    }

                    // create a sequence of SMEs
                    try
                    {
                        _currentFormInst.AddOrUpdateDifferentElementsToCollection(
                            currentElements, _package, addFilesToPackage: true, editSource: true);
                    }
                    catch (Exception ex)
                    {
                        _log?.Error(ex, "when adding Document");
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
                    _log?.Error("Preconditions for adding entities from GenericForm not met.");
                }

                // re-display (tree & panel)
                return new AnyUiLambdaActionRedrawAllElementsBase() { NextFocus = _submodel };
            }

            if (cmd == "ButtonFixCDs")
            {
                // check if CDs are present
                if (_currentFormRecord == null || _currentFormRecord.ConceptDescriptions == null ||
                    _currentFormRecord.ConceptDescriptions.Count < 1)
                {
                    _log?.Error(
                        "Not able to find appropriate ConceptDescriptions in the GeneralForm option records. " +
                        "Aborting.");
                    return new AnyUiLambdaActionNone();
                }

                // check for Environment
                var env = _package?.AasEnv;
                if (env == null)
                {
                    _log?.Error(
                        "Not able to access AAS environment for set of Submodel's ConceptDescriptions. Aborting.");
                    return new AnyUiLambdaActionNone();
                }

#if __not_possible
                // be safe?
                if (MessageBoxResult.Yes != MessageBox.Show(
                    "Add missing ConceptDescriptions to the AAS?", "Question",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning))
                    return;
#endif

                // ok, check
                int nr = 0;
                foreach (var cd in _currentFormRecord.ConceptDescriptions)
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
                _log?.Info("In total, {0} ConceptDescriptions were added to the AAS environment.", nr);

                // re-display (tree & panel)
                return new AnyUiLambdaActionRedrawAllElementsBase() { NextFocus = _submodel } ;
            }

            // no?
            return new AnyUiLambdaActionNone();
        }

#endregion
    }
}
